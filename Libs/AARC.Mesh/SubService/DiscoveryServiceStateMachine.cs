using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace AARC.Mesh.SubService
{
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
        /// Service name of exposed Input Channels
        /// </summary>
        public MeshDictionary<T> LocalInputChannels { get; private set; }
        /// <summary>
        /// Service named of exposed Output Channels
        /// </summary>
        public MeshDictionary<T> LocalOutputChannels { get; private set; }

        private ConcurrentDictionary<string, HashSet<Uri>> _exInputChannels { get;  set; }
        private ConcurrentDictionary<string, HashSet<Uri>> _exOutputChannels { get;  set; }

        protected DiscoveryStates _state = DiscoveryStates.Connect;

        public DiscoveryServiceStateMachine()
        {
            RegistrationComplete = new ManualResetEvent(false);
            LocalInputChannels = new MeshDictionary<T>();
            LocalOutputChannels = new MeshDictionary<T>();
            _exInputChannels = new ConcurrentDictionary<string, HashSet<Uri>>();
            _exOutputChannels = new ConcurrentDictionary<string, HashSet<Uri>>();
        }

        /// <summary>
        /// Returns the list of enpoints availble for 
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public IEnumerable<Uri> FindInputChannelRoutes(string channel)
            => _exInputChannels.Where(kv => string.Equals(kv.Key, channel, StringComparison.OrdinalIgnoreCase)).Select(kv => kv.Value).FirstOrDefault();

        public IEnumerable<Uri> FindOutputChannelRoutes(string channel)
            => _exOutputChannels.Where(kv => string.Equals(kv.Key, channel, StringComparison.OrdinalIgnoreCase)).Select(kv => kv.Value).FirstOrDefault();


        public bool RegisteredInputSource(string action, Uri endpoint)
            => _exInputChannels.Where(iq => string.Equals(iq.Key, action, StringComparison.OrdinalIgnoreCase)).Where(kvp => kvp.Value.Contains(endpoint)).Any();

        public IEnumerable<string> RoutableInputChannels() => _exOutputChannels.Keys.Intersect(LocalInputChannels.Keys);
        public IEnumerable<string> RoutableOutputChannels() => _exInputChannels.Keys.Intersect(LocalOutputChannels.Keys);

        public IEnumerable<Uri> RoutableInputChannelEndpoints() => _exOutputChannels.Keys.Intersect(LocalInputChannels.Keys).Select(key => _exOutputChannels[key].ToList()).SelectMany(t => t).Distinct();

        public IEnumerable<Uri> RoutableOutputChannelEndpoints() => LocalOutputChannels.Keys.Intersect(_exInputChannels.Keys).Select(key => _exInputChannels[key].ToList()).SelectMany(t => t).Distinct();

        public IEnumerable<Tuple<string, IEnumerable<Uri>>> OutputChannelMap =>
            RoutableOutputChannels()
                .Select(r => new { r, l = FindInputChannelRoutes(r) })
                .Where(r => r.l != null && r.l.Any())
                .Select(r => new Tuple<string, IEnumerable<Uri>>(r.r, r.l));

        public IEnumerable<Tuple<string, IEnumerable<Uri>>> IputChannelMap =>
            RoutableInputChannels()
                .Select(r => new { r, l = FindOutputChannelRoutes(r) })
                .Where(r => r.l != null && r.l.Any())
                .Select(r => new Tuple<string, IEnumerable<Uri>>(r.r, r.l));


        public void ResetState() => _state = DiscoveryStates.Connect;

        /// <summary>
        /// State Machine
        ///  Connect -> register -> send inputQs -> send outputQs -> register
        ///  If disconnected then back to connect
        /// </summary>
        public void CreateReceiveMessage(DiscoveryMessage message)
        {
            if (message != null)
            {
                try
                {
                    switch (message.State)
                    {
                        case DiscoveryStates.Connect:
                            _state = DiscoveryStates.ConnectResponse;
                            break;
                        case DiscoveryStates.ConnectResponse:
                            if (Port == 0 && message.Service.Port > 0)
                            {
                                var uri = message.Service;
                                Port = uri.Port;
                            }
                            if (Port == 0)
                                // Todo: Report to Monitor error
                                throw new NotImplementedException();
                            _state = DiscoveryStates.ChannelData;
                            break;
                        case DiscoveryStates.ChannelData:
                            if (message.Channels != null)
                                foreach (var channel in message.Channels)
                                {
                                    // In this senario Service is the address of a single MS
                                    // Channel Name/Service = "tcp://serverhost:xxx"
                                    var channelName = channel.Name;
                                    var service = message.Service;

                                    // Is this safe to take from the DS message?
                                    if (channel.ChannelType == MeshChannel.ChannelTypes.Input)
                                        _exInputChannels.AddOrUpdate(channelName, channel.Addresses, (k, v) => channel.Addresses);
                                    else
                                        _exOutputChannels.AddOrUpdate(channelName, channel.Addresses, (k, v) => channel.Addresses);
                                }
                            _state = DiscoveryStates.ChannelData;
                            RegistrationComplete.Set();
                            break;
                        default:
                            _state = DiscoveryStates.Connect;
                            break;
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// The main purpose is to send the input and output q names to the discovery service.
        /// </summary>
        /// <returns></returns>
        public void CreateSendMessage(DiscoveryMessage message, string hostName)
        {
            var transportId = new Uri($"tcp://{hostName}:{Port}");
            CreateSendMessage(message, transportId);
        }

        public void CreateSendMessage(DiscoveryMessage message, Uri transportId)
        {
            message.State = _state;
            message.Service = transportId;
            switch (_state)
            {
                case DiscoveryStates.Connect:
                    break;
                case DiscoveryStates.ConnectResponse:
                    break;
                case DiscoveryStates.ChannelData:
                    message.Channels = new List<MeshChannel>();
                    foreach (var name in LocalInputChannels.Keys)
                        message.Channels.Add(new MeshChannel { ChannelType = MeshChannel.ChannelTypes.Input, Name = name, Addresses = new HashSet<Uri> { transportId } }); ;

                    foreach (var name in LocalOutputChannels.Keys)
                        message.Channels.Add(new MeshChannel { ChannelType = MeshChannel.ChannelTypes.Output, Name = name, Addresses = new HashSet<Uri> { transportId } });

                    break;
                default:
                    message.State = DiscoveryMessage.DiscoveryStates.Error;
                    break;
            }
        }

        public void CreateErrorMessage(DiscoveryMessage message, string url, string errorMessage)
        {
            // Todo: send to smart monitor
            throw new NotImplementedException();
//            message.State = DiscoveryStates.Error;
//            message.Payload = errorMessage;
//            message.HostServer = url;
        }
    }
}