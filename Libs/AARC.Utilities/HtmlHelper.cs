using System;
using System.Collections.Generic;
using System.Linq;

using HtmlAgilityPack;

namespace AARC.Utilities
{
    // HtmlAgilityPack provides useful functionality...
    public static class HtmlHelper
    {
        public static string FixHtmlString(string raw)
        {
            string s = raw.Trim();
            s = s.Replace("\r\n", " ");
            s = s.Replace('\n', ' ');
            s = s.Replace('\r', ' ');
            s = s.Replace("&iacute", "í");
            s = s.Replace("&oacute", "ó");
            s = s.Replace("&eacute", "é");
            s = s.Replace("&amp;", "&");
            s = s.Replace("&nbsp;", " ");

            return s;
        }

        public static List<List<string[]>> GetTables(string html)
        {

            var sets = new List<List<string[]>> ();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var tables = doc.DocumentNode.Descendants("table");

            foreach (var table in tables)
            {
                var dt = new List<string[]>();
                var rows = table.Descendants("tr");

                foreach (var tableRow in rows)
                {
                    var rowCells = tableRow.Descendants("td");

                    var cells = rowCells.Select(r => r.InnerText.Trim()).ToArray();

                    dt.Add(cells);
                }
                sets.Add(dt);
            }
            return sets;
        }
        public static System.Xml.XPath.XPathNavigator GetNavigator(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            System.Xml.XPath.XPathNavigator navigator = doc.CreateNavigator();
            return navigator;
        }

        public static void TestHAP(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            foreach (HtmlParseError err in doc.ParseErrors)
            {
                Console.WriteLine("Error on Line {0}:{1}. Code={2} Reason={3}. SourceText={4}", err.Line, err.LinePosition, err.Code, err.Reason, err.SourceText);
            }

            var p_xPathNav = doc.CreateNavigator();

            // move to the root and the first element
            p_xPathNav.MoveToRoot();
            p_xPathNav.MoveToFirstChild();

            Console.WriteLine("Printing contents of xml:");

            //begin looping through the nodes
            do
            {
                // list attribute;
                if (p_xPathNav.MoveToFirstAttribute())
                {
                    Console.WriteLine(p_xPathNav.Name + "=" + p_xPathNav.Value);
                    // go back from the attributes to the parent element
                    p_xPathNav.MoveToParent();
                }

                //display the child nodes
                if (p_xPathNav.MoveToFirstChild())
                {
                    Console.WriteLine(p_xPathNav.Name + "=" + p_xPathNav.Value);
                    while (p_xPathNav.MoveToNext())
                    {
                        Console.WriteLine(p_xPathNav.Name + "=" + p_xPathNav.Value);
                    }
                    p_xPathNav.MoveToParent();
                }
            } while (p_xPathNav.MoveToNext());
        }

        // some simple parsing functionality...
    }
}
