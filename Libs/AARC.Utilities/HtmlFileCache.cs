using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AARC.Utilities
{
    /// <summary>
    /// Provides methods for getting from file if present, otherwise goes to url and saves to file
    /// </summary>
    public static class HtmlFileCache
    {
        /// <summary>
        /// Attempts to load from path unless refresh is true
        /// If file not found, or refresh is true, loads from url, and saves to path
        /// </summary>
        /// <param name="url">the web location</param>
        /// <param name="path">the full path including filename</param>
        /// <param name="refresh">if true always goes to web</param>
        /// <returns></returns>
        public static string GetHtml(string url, string path, bool refresh = false)
        {
            string html;
            if (refresh || !File.Exists(path))
            {
                // load from url, and save to path
                html = AarcProxy.GetHtml(url);
                Console.WriteLine($"Read from {url}");

                File.WriteAllText(path, html);
                Console.WriteLine($"Saved to {path}");
            }
            else
            {
                // load from file
                html = File.ReadAllText(path);
                Console.WriteLine($"Read from {path}");
            }

            return html;
        }
    }
}
