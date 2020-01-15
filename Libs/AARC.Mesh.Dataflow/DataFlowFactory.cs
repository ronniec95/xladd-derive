﻿using System;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using AARC.Repository.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.Dataflow
{
    public class DataFlowFactory
    {
        private IServiceCollection _collection;
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
                default:
                    //return new QueueListener(new string[] { name });
                    throw new NotImplementedException();
            }
        }
    }
}