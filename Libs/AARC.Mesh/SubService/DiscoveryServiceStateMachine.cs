using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace AARC.Mesh.SubService
{
    using AARC.Mesh.Model;
    using static AARC.Mesh.Model.DiscoveryMessage;

    public class DiscoveryServiceStateMachine<T>
    {
        /// <summary>
        /// Queue interested MS will connect via this port supplied by DS.
        /// </summary>
        public int Port;
        /// <summary>
        /// Service named input Qs
        /// </summary>
        public MeshDictionary<T> inputQs { get; private set; }
        /// <summary>
        /// Service named output Qs
        /// </summary>
        public MeshDictionary<T> outputQs { get; private set; }

        public ConcurrentDictionary<string, HashSet<string>> InputQsRoutes { get; private set; }
        public ConcurrentDictionary<string, HashSet<string>> OutputQsRoutes { get; private set; }

        protected DiscoveryStates _state = DiscoveryStates.Register;

        public DiscoveryServiceStateMachine()
        {
            inputQs = new MeshDictionary<T>();
            outputQs = new MeshDictionary<T>();
            InputQsRoutes = new ConcurrentDictionary<string, HashSet<string>>();
            OutputQsRoutes = new ConcurrentDictionary<string, HashSet<string>>();
        }

        /// <summary>
        /// Returns the list of enpoints availble for 
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        public IEnumerable<string> InputQueueRoute(string queue)
            => InputQsRoutes.Where(kv => string.Equals(kv.Key, queue, StringComparison.OrdinalIgnoreCase)).Select(kv => kv.Value).FirstOrDefault();

        public IEnumerable<string> OutputQueueRoute(string action)
            => OutputQsRoutes.Where(kv => string.Equals(kv.Key, action, StringComparison.OrdinalIgnoreCase)).Select(kv => kv.Value).FirstOrDefault();


        public bool RegisteredInputSource(string action, string endpoint)
            => InputQsRoutes.Where(iq => string.Equals(iq.Key, action, StringComparison.OrdinalIgnoreCase)).Where(kvp => kvp.Value.Contains(endpoint)).Any();

        public IEnumerable<string> RoutableInputQs() => OutputQsRoutes.Keys.Intersect(inputQs.Keys);
        public IEnumerable<string> RoutableOutputQs() => InputQsRoutes.Keys.Intersect(outputQs.Keys);

        public IEnumerable<string> RoutableInputQEndpoints() => OutputQsRoutes.Keys.Intersect(inputQs.Keys).Select(key => OutputQsRoutes[key].ToList()).SelectMany(t => t).Distinct();

        public IEnumerable<string> RoutableOutputQEndpoints() => outputQs.Keys.Intersect(InputQsRoutes.Keys).Select(key => InputQsRoutes[key].ToList()).SelectMany(t => t).Distinct();

        public IEnumerable<Tuple<string, IEnumerable<string>>> OutputQRoutes =>
            RoutableOutputQs()
                .Select(r => new { r, l = InputQueueRoute(r) })
                .Where(r => r.l != null && r.l.Any())
                .Select(r => new Tuple<string, IEnumerable<string>> (r.r,  r.l ));

        public IEnumerable<Tuple<string, IEnumerable<string>>> InputQRoutes =>
            RoutableInputQs()
                .Select(r => new { r, l = OutputQueueRoute(r) })
                .Where(r => r.l != null && r.l.Any())
                .Select(r => new Tuple<string, IEnumerable<string>>(r.r, r.l));

        /// <summary>
        /// State Machine
        ///  Connect -> register -> send inputQs -> send outputQs -> register
        ///  If disconnected then back to connect
        /// </summary>
        public void Receive(DiscoveryMessage message)
        {
            if (message != null)
            {
                switch (message.State)
                {
                    case DiscoveryStates.Connect:
                        _state = DiscoveryStates.Register;
                        break;
                    case DiscoveryStates.Register:
                        Port = message.Port;
                        _state = DiscoveryStates.GetInputQs;
                        break;
                    case DiscoveryStates.GetInputQs:
                        var iq = JsonConvert.DeserializeObject<ConcurrentDictionary<string, HashSet<string>>>(message.Payload);
                        MeshUtilities.Merge(InputQsRoutes, iq);
                        _state = DiscoveryStates.GetOutputQs;
                        break;
                    case DiscoveryStates.GetOutputQs:
                        var oq = JsonConvert.DeserializeObject<ConcurrentDictionary<string, HashSet<string>>>(message.Payload);
                        MeshUtilities.Merge(OutputQsRoutes, oq);
                        _state = DiscoveryStates.Register;
                        break;
                    default:
                        _state = DiscoveryStates.Connect;
                        break;
                }
            }
        }

        /// <summary>
        /// The main purpose is to send the input and output q names to the discovery service.
        /// </summary>
        /// <returns></returns>
        public void Send(DiscoveryMessage message, string hostName)
        {
            message.State = _state;
            message.HostServer = hostName;
            message.Port = Port;
            switch (_state)
            {
                case DiscoveryStates.Connect:
                    break;
                case DiscoveryStates.Register:
                    break;
                case DiscoveryStates.GetInputQs:
#if NETSTANDARD2_0
                    message.Payload = inputQs.Keys.Any() ? string.Join(",", inputQs.Keys) : string.Empty;
#endif
#if NETSTANDARD2_1 // Targets .netcore 3.0
                    message.PayLoad = inputQs.Keys.Any() ? string.Join(',',inputQs.Keys) : string.Empty;
#endif
                    break;
                case DiscoveryStates.GetOutputQs:
#if NETSTANDARD2_0
                    message.Payload = outputQs.Keys.Any() ? string.Join(",", outputQs.Keys) : string.Empty;
#endif
#if NETSTANDARD2_1 // Targets .netcore 3.0
                    message.PayLoad =  outputQs.Keys.Any() ? string.Join(',', outputQs.Keys) : string.Empty;
#endif
                    break;
                default:
                    message.State = DiscoveryMessage.DiscoveryStates.Error;
                    break;
            }
        }
    }
}