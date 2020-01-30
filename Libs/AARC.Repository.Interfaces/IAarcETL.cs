using System;

namespace AARC.Repository.Interfaces
{
    public interface IAarcETL : IDisposable
    {
        string Symbol { get; }
        IUnitOfWork UnitOfWork { get; }
        DateTime StartDate { get; set; }
        DateTime EndDate { get; set; }

        /// <summary>
        /// When the load begins - either used by process or ?
        /// </summary>
        DateTime LoadStart { get; set; }

        /// <summary>
        /// LoadTime - typically now minus LoadStart giving a processing/loading time
        /// </summary>
        TimeSpan LoadTime { get; set; }

        void Initialise(string symbol, IUnitOfWork unitOfWork);
        bool IsLoaded(DateTime date);

        /// <summary>
        /// Prepare the data - e.g. load html or json into the instance
        /// e.g. a string property
        /// </summary>
        void Load();

        /// <summary>
        /// Transform the data from e.g. html/json into objects specific to the instance of the ETL
        /// </summary>
        void Process();

        /// <summary>
        /// Uses UnitOfWork Save/Overwrite method to save into the appropriate repository
        /// Specific ETLs know which Repositories to use for their data
        /// </summary>
        void Save();
    }
}