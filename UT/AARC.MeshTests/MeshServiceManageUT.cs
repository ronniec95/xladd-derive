using System;
using System.Threading;
using System.Threading.Tasks;
using AARC.Mesh.Interface;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AARC.MeshTests
{
    class Test<T> : IMeshTransport<T> where T : IMeshMessage
    {
        public string TransportId => throw new NotImplementedException();

        public Task Cancel()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(T value)
        {
            throw new NotImplementedException();
        }

        public void ServiceConnect(string serverDetails, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task StartListeningServices(int port, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            throw new NotImplementedException();
        }
    }
    [TestClass]
    public class MeshServiceManageUT
    {

        public MeshServiceManageUT()
        {
        }

        [TestMethod]
        public void TestCreate()
        {
            //var msm = null;//new MeshServiceManager();
            throw new NotImplementedException();
        }
    }
}
