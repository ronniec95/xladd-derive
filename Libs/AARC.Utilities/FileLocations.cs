using System;
using System.IO;

namespace AARC.Utilities
{
    public static class FileLocations
    {
        public const string DataPath = BasePath + @"Data\";
        private const string BasePath = @"E:\Dev\AARC2\";

        public enum FileLocationType { SnpAutoCorrelation, SnpTickers, Options, Profiles };

        public static string GetPath(FileLocationType type)
        {
            switch (type)
            {
                case FileLocationType.SnpAutoCorrelation:
                    return BasePath + @"Autocorrelations\SP500_AC.csv";

                case FileLocationType.SnpTickers:
                    return DataPath + @"Tickers\SP500.csv";

                case FileLocationType.Options:
                    return DataPath + @"Options\";

                case FileLocationType.Profiles:
                    return DataPath + @"Profiles\";

                default:
                    throw new NotImplementedException();
            }
        }

        public static string[] ReadLines(FileLocationType type)
        {
            return File.ReadAllLines(GetPath(type));
        }

    }
}
