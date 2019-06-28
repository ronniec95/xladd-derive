using System;
namespace AARC.Utilities
{
    public static class PathHelper
    {
        public static string GetHeadPath(string path)
        {
            if (path == null)
                return string.Empty;

            var i = path.IndexOf('/');
            if (i < 0)
                return path;

            if (i == 0)
                return GetHeadPath(path.Substring(1));

            return path.Substring(0, i);
        }
    }
}
