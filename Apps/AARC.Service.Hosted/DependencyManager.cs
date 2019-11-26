using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AARC.Service.Hosted
{
    /// <summary>
    /// A new services fires up
    /// Sends a register action to the depedency service
    /// </summary>
    public class DependencyManager
    {
        /// <summary>
        /// action => Set of address:port
        /// </summary>
        private ConcurrentDictionary<string, HashSet<string>> _inputDependencies;

        private ConcurrentDictionary<string, HashSet<string>> _outputDependencies;

        public DependencyManager()
        {
            _inputDependencies = new ConcurrentDictionary<string, HashSet<string>>();
            _outputDependencies = new ConcurrentDictionary<string, HashSet<string>>();
        }

        /// <summary>
        /// Each service will register its inputs actions
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <param name="actions"></param>
        public void RegisterInputDependencies(string ipaddress, IList<string> actions)
        {
            foreach (var action in actions)
                if (!_inputDependencies.ContainsKey(action))
                {
                    var list = new HashSet<string> { ipaddress };
                    _inputDependencies[action] = list;
                }
                else
                    _inputDependencies[action].Add(ipaddress);
        }

        public void RegisterOutputDependencies(string ipaddress, IList<string> actions)
        {
            foreach (var action in actions)
                if (!_outputDependencies.ContainsKey(action))
                {
                    var list = new HashSet<string> { ipaddress };
                    _outputDependencies[action] = list;
                }
                else
                    _outputDependencies[action].Add(ipaddress);
        }

        public IList<string> FindInputServices(string action) => FindServices(_inputDependencies, action);

        public IList<string> FindOutputServices(string action) => FindServices(_outputDependencies, action);

        public static IList<string> FindServices(ConcurrentDictionary<string, HashSet<string>> dependencies, string action)
        {
            if (dependencies.ContainsKey(action))
                return new List<string>(dependencies[action]);
            else
                return null;
        }
    }
}
