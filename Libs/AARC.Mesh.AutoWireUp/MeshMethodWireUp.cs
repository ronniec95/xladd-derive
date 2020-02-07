using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using AARC.Mesh.Reflection;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.AutoWireUp
{

    public class MeshMethodWireUp : IMeshReactor<MeshMessage>
    {
        // Method, Reflection Properties
        private readonly ConcurrentDictionary<string, ReflectionTransform> _methodRefection = new ConcurrentDictionary<string, ReflectionTransform>();

        // Method, Set of Parameter names
        private readonly ConcurrentDictionary<string, HashSet<string>> _inParamNames = new ConcurrentDictionary<string, HashSet<string>>();

        // Method, Set of Parameters received (from observables)
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _inParameterResults = new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>();

        // Method, observer (returns)
        private readonly ConcurrentDictionary<string, List<MeshObserver<object>>> _observers = new ConcurrentDictionary<string, List<MeshObserver<object>>>();

        private readonly ILogger<MeshMethodWireUp> _logger;

        #region IMeshReactor interface

        public string Name { get; }
        public IList<IRouteRegister<MeshMessage>> ChannelRouters { get; }

        public void Start()
        {
        }

        #endregion IMeshReactor interface

        public MeshMethodWireUp(Type classType, ILogger<MeshMethodWireUp> logger = null)
        {
            _logger = logger;

            ChannelRouters = new List<IRouteRegister<MeshMessage>>();

            Name = classType.Name;

            // Initialise transforms
            var transforms = ReflectionHelper.GetTransforms(classType);
            foreach (var transform in transforms)
                WireUpTransform(transform);
        }

        // set up _inParamNames by method for inputs
        // add observables for inputs, and subscribe, add to ChannelRouters
        // add observers for outputs, add to ChannelRouters
        private void WireUpTransform(ReflectionTransform transform)
        {
            if (transform == null)
                return;

            var method = $"{Name}.{transform.Method.Name}";

            _methodRefection[method] = transform;

            _inParamNames[method] = transform.Inputs.Select(n => n.Name).ToHashSet();

            // See Examples: biggestStocks (producer) and NasdaqTradableTickers -> input and output
            /*
                observerable = new MeshObservable<List<string>>("nasdaqtestin");
                observer = new MeshObserver<IAarcPrice>("nasdaqtestout");

                ChannelRouters = new List<IRouteRegister<MeshMessage>> { observer as IRouteRegister<MeshMessage>, observerable as IRouteRegister<MeshMessage> };

                observerable.Subscribe((tickers) =>
                    {
                        _logger.LogInformation($"Received an update request {string.Join("", tickers)}");
                        // Should update by Ticker
                        _tickers.Union(tickers);
                        Update();
                    });

                observer.OnConnect += (transportUrl) =>
                {
                    _marketLoaded.WaitOne();
                    lock (_sync)
                        if (_marketUniverse != null)
                            foreach (var kvp in _marketUniverse)
                                observer?.OnNext(kvp.Value, transportUrl);
                };
            */

            // set up input observables
            foreach (var ip in transform.Inputs)
            {
                // Creates an observable of the provided type if possible, otherwise a MeshObservable<object>,
                // and calls Subscribe with an Action to set the parameter, and call ExecuteMethod - which will be called if all parameters are available
                var observable = CreateObservable(ip.Type, method, ip.Name);
                ChannelRouters.Add(observable as IRouteRegister<MeshMessage>);
            }

            // set up output observers
            _observers[method] = new List<MeshObserver<object>>();
            foreach (var op in transform.Outputs)
            {
                // Creates an observer of the provided type if possible, otherwise a MeshObserver<object>
                MeshObserver<object> observer = CreateObserver(FixType(op.Type), method, op.Name);
                ChannelRouters.Add(observer as IRouteRegister<MeshMessage>);

                _observers[method].Add(observer);
            }

            _logger?.Log(LogLevel.Information, $"Wired up: ${method}", null);
        }

        // Special handling for Ref type - get the 'underlying' type
        private Type FixType(Type t)
        {
            if (t.FullName != null && t.FullName.EndsWith("&"))
            {
                t = t.Assembly.GetType(t.FullName.Replace("&", ""));
                System.Diagnostics.Debug.Assert(t != null);
            }

            return t;
        }

        private void ExecuteMethod(string method)
        {
            if (CanExecute(method))
            {
                var transform = _methodRefection[method];
                Console.WriteLine($"Got full set of parameters {method}");

                object[] parametersArray = _inParameterResults[method].Values.ToArray();
                object[] resultObjects = transform.Invoke(parametersArray);

                // Can be multiple outputs - must call onNext for each
                // Note: the order of result objects, is the same as the order of Outputs in the transform
                for (int i = 0; i < resultObjects.Length; i++)
                {
                    MeshObserver<object> observer = _observers[method][i];
                    observer.OnNext(resultObjects[i]);
                }
            }
        }

        // Check if the set of in param keys is the set for the method name - if it is, we can invoke the method
        private bool CanExecute(string method)
        {
            if (!_methodRefection.ContainsKey(method))
                return false;

            return _inParamNames[method].SetEquals(_inParameterResults[method].Keys);
        }

        private static string GetParameterFullName(string method, string pName) => $"{method}.{pName}";

        private static MeshObserver<object> CreateObserver(Type t, string method, string pName)
        {
            string fullNameParameter = GetParameterFullName(method, pName);

            if (t.ContainsGenericParameters)
            {
                // now we have a problem? we cannot instantiate a MeshObserver of Type T when the runtime type is unknown
                // so we must instantiate a general one of type object
                MeshObserver<object> observer = new MeshObserver<object>(fullNameParameter);
                return observer;
            }

            // Note, ref type such as System.String& - may not be used here
            var constructed = typeof(MeshObserver<>).MakeGenericType(typeof(object));
            return (MeshObserver<object>)Activator.CreateInstance(constructed, fullNameParameter);
        }

        // This will be called by reflection
        // Note: Late bound operations cannot be performed on types or methods for which ContainsGenericParameters is true (eg.. T[], or List<T>)
        // Also, for example "cannot create an instance of AARC.Mesh.Model.MeshObservable`1[T[]] because Type.ContainsGenericParameters is true."
        private MeshObservable<object> CreateObservable(Type t, string method, string pName)
        {
            var fullNameParameter = GetParameterFullName(method, pName);

            if (!_inParameterResults.ContainsKey(method))
                _inParameterResults[method] = new ConcurrentDictionary<string, object>();

            var paramResults = _inParameterResults[method];

            Type moType = typeof(MeshObservable<>).MakeGenericType(typeof(object));

            MeshObservable<object> observable;
            if (t.ContainsGenericParameters)
            {
                // we cannot instantiate if we don't have the run time parameter type, so we have to use object
                observable = new MeshObservable<object>(fullNameParameter);
            }
            else
            {
                var o = Activator.CreateInstance(moType, fullNameParameter);
                observable = o as MeshObservable<object>;
            }

            observable.Subscribe(s =>
            {
                Console.WriteLine($"Received an update {fullNameParameter} {s}");
                paramResults[pName] = s;
                ExecuteMethod(method);
            });

            return observable;
        }
    }
}
