using System;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using AARC.Repository.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.Dataflow
{
    public class DataFlowFactory : IMeshNodeFactory
    {
        private IServiceProvider _provider;

        public DataFlowFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IMeshReactor<MeshMessage> Get(string service)
        {
            switch (service.ToLower())
            {
                case @"biggeststocks":
                    {
                        var logger = _provider.GetService<ILogger<BiggestStocksReactor>>();
                        var repository = _provider.GetService<IStockRepository>();
                        return new BiggestStocksReactor(logger, repository);
                    }
                case @"nasdaqtradabletickers":
                    {
                        var logger = _provider.GetService<ILogger<NasdaqTradableTickers>>();
                        var repository = _provider.GetService<IMarketDataRepository>();
                        return new NasdaqTradableTickers(logger, repository);
                    }
                case @"test":
                    {
                        var logger = _provider.GetService<ILogger<DataFlowChannelListener>>();
                        return new DataFlowChannelListener(logger);
                    }
                default:
                    //return new QueueListener(new string[] { name });
                    throw new NotImplementedException();
            }
        }
    }
}
