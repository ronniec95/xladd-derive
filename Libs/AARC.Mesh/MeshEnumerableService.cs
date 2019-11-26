using System.Collections.Generic;
using AARC.Mesh.Interface;

namespace AARC.Mesh
{
    public class MeshEnumerableService : IMeshService, IByteEnumerator
    {
        IMeshService _dataSource;
        public MeshEnumerableService(IMeshService dataSource)
        {
            _dataSource = dataSource;
            EndPoint = _dataSource.EndPoint;
            Actions = new List<string>();
        }

        public void GetMeshDetails()
        {
            var bytes = _dataSource.GetData();
            //var data = System.Text.Encoding.ASCII.GetString(bytes, 0, bytes.Length);

            //if (data.ValidateJSON())
            {
                var message = Model.MeshMessage.Deserialise(bytes);
                if (message.IsValid())
                {
                    Actions.Add(message.QueueName);
                        //Message.Split(',');
                }
            }
        }

        public IList<string> Actions { get; private set; }

        public IEnumerable<byte[]> GetEnumerable()
        {
            byte[] data;
            do
            {
                data = _dataSource.GetData();

                yield return data;
            } while (data != null);
            if (data == null)
                yield break;
        }

        public string EndPoint { get; set; }

        public byte[] GetData() => _dataSource.GetData();

        public void PutData(byte[] data) => _dataSource.PutData(data);
    }
}