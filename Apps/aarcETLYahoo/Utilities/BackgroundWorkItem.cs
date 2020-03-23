using System;
using System.Threading;
using System.Threading.Tasks;

namespace aarcYahooFinETL.Utilities
{
    public static class BackgroundWorkItem
    {
        public static void QueueBackgroundWorkItem(
            this IBackgroundTaskQueue queue,
            Func<CancellationToken, Task> method)
        {
            queue.Queue(new WorkOrder(method));
        }

        public class WorkOrder : IBackgroundWorkOrder<WorkOrder, Worker>
        {
            public WorkOrder(Func<CancellationToken, Task> method)
            {
                this.Method = method;
            }

            public Func<CancellationToken, Task> Method { get; }
        }

        public class Worker : IBackgroundWorker<WorkOrder, Worker>
        {
            public async Task DoWork(WorkOrder order, CancellationToken cancellationToken)
            {
                await order.Method.Invoke(cancellationToken);
            }
        }
    }
}
