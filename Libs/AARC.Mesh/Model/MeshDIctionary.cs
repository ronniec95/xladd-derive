using System;
using System.Collections.Concurrent;
using AARC.Mesh.Interface;
using Newtonsoft.Json;

namespace AARC.Mesh.Model
{
    public class MeshDictionary<T> : ConcurrentDictionary<string, INetQueueObservable<T>>
    {
        public string Serialize() => JsonConvert.SerializeObject(this);
        public static MeshDictionary<T> Deserialise(string message) => JsonConvert.DeserializeObject<MeshDictionary<T>>(message);
    }
}
