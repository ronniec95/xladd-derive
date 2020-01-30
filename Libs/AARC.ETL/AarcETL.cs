using System;
using AARC.Repository.Interfaces;

namespace AARC.ETL
{
    public abstract class AarcETL : IAarcETL
    {
        public string Symbol { get; private set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime LoadStart { get; set; }
        public TimeSpan LoadTime { get; set; }
        public IUnitOfWork UnitOfWork { get; private set; }

        public abstract void Dispose();

        /// <summary>
        /// This must be called prior to use of ETL to set the symbol (common to derived ETLs)
        /// and the UnitOfWork
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="unitOfWork"></param>
        public void Initialise(string symbol, IUnitOfWork unitOfWork)
        {
            Symbol = symbol;
            UnitOfWork = unitOfWork;
        }

        public abstract bool IsLoaded(DateTime date);

        public abstract void Load();

        public abstract void Process();

        public abstract void Save();
    }
}
