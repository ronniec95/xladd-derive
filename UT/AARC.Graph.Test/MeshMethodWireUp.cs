using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using Microsoft.Extensions.Logging;

namespace AARC.Graph.Test
{
    public class Dumb
    {
        public static string StringMethod(string input1, string input2)
        {
            return input1 + input2;
        }
/*
        public static int IntMethod(int input1, int input2)
        {
            return input1 + input2;
        }

        public static long LongMethod(long input1, long input2)
        {
            return input1 + input2;
        }

        public static double DoubleMethod(double input1, double input2)
        {
            return input1 + input2;
        }

        public static double HybridMethod(int input1, long input2, double input3, string input4)
        {
            if (string.IsNullOrEmpty(input4))
                return 0;
            return input1 + input2 + input3;
        }*/
    }

    public class MeshMethodWireUp : IMeshReactor<MeshMessage>
    {
        // Method, Reflection Properties
        private ConcurrentDictionary<string, ReflectionTransform> _methodRefection;
        // Method, Set of Parameter names
        private ConcurrentDictionary<string, HashSet<string>> _inParamNames;
        // Method, Set of Parameters received (from observables)
        private ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _inParameterResults;
        // Method, observer (returns)
        private ConcurrentDictionary<string, object> observers;

        private ILogger<MeshMethodWireUp> _logger;
        public string Name { get; set; }
        public IList<IRouteRegister<MeshMessage>> ChannelRouters { get; set; }

        public Type GetConstructorType(Type t, Type generic) => generic.MakeGenericType(new [] { t });

        private void ExecuteMethod(string method)
        {
            if (_inParamNames[method].SetEquals(_inParameterResults.Keys))
                if (_methodRefection.ContainsKey(method))
                {
                    var m = _methodRefection[method];
                    Console.WriteLine($"Got full set of parameters {method}");
                    var result = m.Method.Invoke(null, _inParameterResults.Values.ToArray());
                    var observer = observers[method];
                    var observerType = observer.GetType();
                    var onNext = observerType.GetMethod("OnNext");
                    var tmp = onNext.Invoke(observer, new[] { result });
                }
        }
        public object CreateObservable(Type t, string method, string pName)
        {
            var fullNameParameter = $"{method}.{pName}";
            if (!_inParameterResults.ContainsKey(pName))
                _inParameterResults[pName] = new ConcurrentDictionary<string, object>();
            var paramResults = _inParameterResults[pName];

            if (t == typeof(string))
            {
                var observerable = new MeshObservable<string>(fullNameParameter);
                observerable.Subscribe((s) =>
                    {
                        Console.WriteLine($"Received an update {fullNameParameter} {s}");
                        paramResults[pName] = s;
                        ExecuteMethod(method);

                    });
                return observerable;
            }
            else if (t == typeof(int))
            {
                var observerable = new MeshObservable<int>(fullNameParameter);
                observerable.Subscribe((s) =>
                {
                    Console.WriteLine($"Received an update {fullNameParameter} {s}");
                    paramResults[pName] = s;
                    ExecuteMethod(method);
                });
                return observerable;
            }
            else if (t == typeof(long))
            {
                var observerable = new MeshObservable<long>(fullNameParameter);
                observerable.Subscribe((s) =>
                {
                    Console.WriteLine($"Received an update {fullNameParameter} {s}");
                    paramResults[pName] = s;
                    ExecuteMethod(method);
                });
                return observerable;
            }
            else if (t == typeof(double))
            {
                var observerable = new MeshObservable<double>(fullNameParameter);
                observerable.Subscribe((s) =>
                {
                    Console.WriteLine($"Received an update {fullNameParameter} {s}");
                    paramResults[pName] = s;
                    ExecuteMethod(method);
                });
                return observerable;
            }
            else
            {
                var constructed = GetConstructorType(t, typeof(MeshObservable<>));
                var observerable = Activator.CreateInstance(constructed, fullNameParameter);
                return observerable;
            }
        }

        public object CreateObserver(Type t, string method, string pName)
        {
            if (t == typeof(string))
                return new MeshObserver<string>(method);
            else if (t == typeof(int))
                return new MeshObserver<int>(method);
            else if (t == typeof(long))
                return new MeshObserver<long>(method);
            else if (t == typeof(double))
                return new MeshObserver<double>(method);
            else
            {
                var constructed = GetConstructorType(t, typeof(MeshObserver<>));
                return Activator.CreateInstance(constructed, method);
            }
        }

        public MeshMethodWireUp(ILogger<MeshMethodWireUp> logger = null)
        {
            _logger = logger;
            _inParameterResults = new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>();
            _inParamNames = new ConcurrentDictionary<string, HashSet<string>>();
            observers = new ConcurrentDictionary<string, object>();
            ChannelRouters = new List<IRouteRegister<MeshMessage>>();

            var transforms = DataFlowReflectionHelper.GetTransforms(typeof(Dumb));
            Name = typeof(Dumb).Name;
            foreach(var m in transforms)
            if (m != null)
            {
                var method = $"{Name}.{m.Method.Name}";
                _inParamNames[method] = m.Inputs.Select(n => n.Name).ToHashSet();
                foreach(var ip in m.Inputs)
                {
                    Type t = ip.Type;
                    var observerable = CreateObservable(ip.Type, method, ip.Name);
                    ChannelRouters.Add(observerable as IRouteRegister<MeshMessage>);
                }
                //var outParms = object;
                var op = m.Outputs?.FirstOrDefault();
                if (op != null)
                {
                    observers[Name] = CreateObserver(op.Type, method, op.Name);
                    ChannelRouters.Add(observers[Name] as IRouteRegister<MeshMessage>);
                }
            }
        }

        public void Start()
        {
        }
    }
}
