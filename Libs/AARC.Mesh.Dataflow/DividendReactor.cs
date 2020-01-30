using System;
using System.Collections.Generic;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using AARC.Model;
using AARC.Repository.Interfaces;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.Dataflow
{
    public class DividendReactor : IMeshReactor<MeshMessage>
    {
        private readonly object _sync = new object();
        private readonly ILogger<DividendReactor> _logger;
        private readonly MeshObserver<List<Dividend>> _observer;
        private List<Dividend> _dividends = null;
        private IDividendRepository _repository;

        public DividendReactor(ILogger<DividendReactor> logger, IDividendRepository repository)
        {
            _logger = logger;
            _observer = new MeshObserver<List<Dividend>>(Name);

            _observer.OnConnect += ((transportUrl) =>
            {
                lock (_sync)
                    if (_dividends != null)
                        _observer?.OnNext(_dividends);
            });
        }

        public string Name => "dividends";

        public IList<IRouteRegister<MeshMessage>> ChannelRouters { get; private set; }

        public void Start()
        {
            Update();
        }

        private void Update()
        {
            lock(_sync)
            {
                _dividends = _repository.GetAllDividends();
            }
        }
    }
}
