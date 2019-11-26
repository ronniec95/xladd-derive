using System;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;
using System.Collections.Generic;

namespace AARC.Graph.Test
{
    public class QueueListener : MeshQueueMarshal, IObserver<IList<MeshMessage>>
    {
        private ILogger _logger;
        public QueueListener(string[] queues, ILogger logger = null)
        {
            InputQueueNames = queues;
            OutputQueueNames = new string[] { };
            _logger = logger;
        }

        public override void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public override void Subscribe(IObservable<MeshMessage> provider)
        {
            Buffer(provider);
        }

        public void Throttle(IObservable<MeshMessage> provider)
        {
            if (provider != null)
                unsubscriber = provider
                    .Throttle(TimeSpan.FromMilliseconds(600))
                    .Subscribe(this);
        }

        public void Buffer(IObservable<MeshMessage> provider)
        {
            if (provider != null)
                unsubscriber = provider
                    .Buffer(TimeSpan.FromMilliseconds(600))
                    .Where(b => b.Count > 0)
                    .Subscribe(this);
        }

        public override void OnNext(MeshMessage item)
        {
            if (item != null)
                if (_logger != null)
                {
                    _logger.LogInformation($"MeshMessage received");
                    _logger.LogInformation($"GraphId {item.GraphId}");
                    _logger.LogInformation($"Xid {item.XId}");
                    _logger.LogInformation($"QueueName {item.QueueName}");
                    _logger.LogInformation($"PayLoad {item.PayLoad}");
                    _logger.LogInformation($"Split {item.Split}");
                    _logger.LogInformation($"Monitor {item.Monitor}");
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now} MeshMessage received");
                    Console.WriteLine($"GraphId {item.GraphId}");
                    Console.WriteLine($"Xid {item.XId}");
                    Console.WriteLine($"QueueName {item.QueueName}");
                    Console.WriteLine($"PayLoad {item.PayLoad}");
                    Console.WriteLine($"Split {item.Split}");
                    Console.WriteLine($"Monitor {item.Monitor}");
                }
        }

        /// <summary>
        /// Handles Buffer subscription
        /// </summary>
        /// <param name="items"></param>
        public virtual void OnNext(IList<MeshMessage> items)
        {
            Console.WriteLine($"{DateTime.Now} Buffer {items.Count}");
            foreach (var item in items)
            {
                OnNext(item);
            }
        }
    }
}
