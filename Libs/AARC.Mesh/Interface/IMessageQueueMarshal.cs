using System.Collections.Generic;
using AARC.Mesh.Model;

namespace AARC.Mesh.Interface
{
    public interface IMessageQueueMarshal<T> where T : class
    {
        /// <summary>
        /// Names of the input queues we wish to subscribe to.
        /// </summary>
        IList<string> InputQueueNames { get; set; }
        /// <summary>
        /// Names of the output queues we wish to publish to.
        /// </summary>
        IList<string> OutputQueueNames { get; set; }

        /// <summary>
        /// Add InputQueueNames and OutputQueue names to the Mesh Service queues
        /// </summary>
        /// <param name="inputQs"></param>
        /// <param name="outputQs"></param>
        void RegisterDependencies(MeshDictionary<T> inputQs, MeshDictionary<T> outputQs);

        MeshQueueResult<T> PostOutputQueue { get; set; }
    }

    public delegate void MeshQueueResult<T>(string action, T message) where T : class;

}