using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace AARC.Mesh.SubService
{
    using System.Threading;
    using AARC.Mesh.Model;
    using static AARC.Mesh.Model.DiscoveryMessage;

    public class DiscoveryServiceStateMachine<T>
    {
        public readonly ManualResetEvent RegistrationComplete;

        /// <summary>
        /// Queue interested MS will connect via this port supplied by DS.
        /// </summary>
        public int Port;
        /// <summary>
        /// Service named input Qs
        /// </summary>
        public MeshDictionary<T> inputChannels { get; private set; }
        /// <summary>
        /// Service named output Qs
        /// </summary>
        public MeshDictionary<T> outputChannels { get; private set; }

        public ConcurrentDictionary<string, HashSet<string>> InputChannelRoutes { get; private set; }
        public ConcurrentDictionary<string, HashSet<string>> OutputChannelRoutes { get; private set; }

        protected DiscoveryStates _state = DiscoveryStates.Register;

        public DiscoveryServiceStateMachine()
        {
            RegistrationComplete = new ManualResetEvent(false);
            inputChannels = new MeshDictionary<T>();
            outputChannels = new MeshDictionary<T>();
            InputChannelRoutes = new ConcurrentDictionary<string, HashSet<string>>();
            OutputChannelRoutes = new ConcurrentDictionary<string, HashSet<string>>();
        }

        /// <summary>
        /// Returns the list of enpoints availble for 
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public IEnumerable<string> FindInputChannelRoutes(string channel)
            => InputChannelRoutes.Where(kv => string.Equals(kv.Key, channel, StringComparison.OrdinalIgnoreCase)).Select(kv => kv.Value).FirstOrDefault();

        public IEnumerable<string> FindOutputChannelRoutes(string channel)
            => OutputChannelRoutes.Where(kv => string.Equals(kv.Key, channel, StringComparison.OrdinalIgnoreCase)).Select(kv => kv.Value).FirstOrDefault();


        public bool RegisteredInputSource(string action, string endpoint)
            => InputChannelRoutes.Where(iq => string.Equals(iq.Key, action, StringComparison.OrdinalIgnoreCase)).Where(kvp => kvp.Value.Contains(endpoint)).Any();

        public IEnumerable<string> RoutableInputChannels() => OutputChannelRoutes.Keys.Intersect(inputChannels.Keys);
        public IEnumerable<string> RoutableOutputChannels() => InputChannelRoutes.Keys.Intersect(outputChannels.Keys);

        public IEnumerable<string> RoutableInputChannelEndpoints() => OutputChannelRoutes.Keys.Intersect(inputChannels.Keys).Select(key => OutputChannelRoutes[key].ToList()).SelectMany(t => t).Distinct();

        public IEnumerable<string> RoutableOutputChannelEndpoints() => outputChannels.Keys.Intersect(InputChannelRoutes.Keys).Select(key => InputChannelRoutes[key].ToList()).SelectMany(t => t).Distinct();

        public IEnumerable<Tuple<string, IEnumerable<string>>> OutputQRoutes =>
            RoutableOutputChannels()
                .Select(r => new { r, l = FindInputChannelRoutes(r) })
                .Where(r => r.l != null && r.l.Any())
                .Select(r => new Tuple<string, IEnumerable<string>> (r.r,  r.l ));

        public IEnumerable<Tuple<string, IEnumerable<string>>> InputQRoutes =>
            RoutableInputChannels()
                .Select(r => new { r, l = FindOutputChannelRoutes(r) })
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
                        MeshUtilities.Merge(InputChannelRoutes, iq);
                        _state = DiscoveryStates.GetOutputQs;
                        break;
                    case DiscoveryStates.GetOutputQs:
                        var oq = JsonConvert.DeserializeObject<ConcurrentDictionary<string, HashSet<string>>>(message.Payload);
                        MeshUtilities.Merge(OutputChannelRoutes, oq);
                        _state = DiscoveryStates.GetInputQs;
                        RegistrationComplete.Set();
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
                    message.Payload = inputChannels.Keys.Any() ? string.Join(",", inputChannels.Keys) : null;
#endif
#if NETSTANDARD2_1 // Targets .netcore 3.0
                    message.PayLoad = inputQs.Keys.Any() ? string.Join(',',inputQs.Keys) : string.Empty;
#endif
                    break;
                case DiscoveryStates.GetOutputQs:
#if NETSTANDARD2_0
                    message.Payload = outputChannels.Keys.Any() ? string.Join(",", outputChannels.Keys) : null;
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