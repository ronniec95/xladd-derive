using System;
using System.Threading;
using System.Threading.Tasks;

namespace aarcYahooFinETL.Utilities
{
    public interface IBackgroundWorkOrder { }

    public interface IBackgroundWorkOrder<TWorkOrder, TWorker> : IBackgroundWorkOrder
        where TWorker : IBackgroundWorker<TWorkOrder, TWorker>
        where TWorkOrder : IBackgroundWorkOrder<TWorkOrder, TWorker>
    {
    }

    public interface IBackgroundWorker { }

    public interface IBackgroundWorker<TWorkOrder, TWorker> : IBackgroundWorker
        where TWorker : IBackgroundWorker<TWorkOrder, TWorker>
        where TWorkOrder : IBackgroundWorkOrder<TWorkOrder, TWorker>
    {
        Task DoWork(TWorkOrder order, CancellationToken cancellationToken);
    }

    public interface IBackgroundTaskQueue
    {
        void Queue<TWorkOrder, TWorker>(IBackgroundWorkOrder<TWorkOrder, TWorker> order)
            where TWorker : IBackgroundWorker<TWorkOrder, TWorker>
            where TWorkOrder : IBackgroundWorkOrder<TWorkOrder, TWorker>;

        Task<IBackgroundWorkOrder> DequeueAsync(CancellationToken cancellationToken);
    }
}
