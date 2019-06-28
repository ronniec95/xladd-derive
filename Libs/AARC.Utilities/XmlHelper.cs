using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace AARC.Utilities
{
    public static class XmlHelper
    {
        public static bool DeserialiseFromFile(string path, ref object objectToDeserialise)
        {
            try
            {
                var serializer = new XmlSerializer(objectToDeserialise.GetType());
                using (var sr = new FileStream(path, FileMode.Open))
                {
                    objectToDeserialise = serializer.Deserialize(sr);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return false;
            }

            return true;
        }

        public static bool SerialiseToFile(object o, string path)
        {
            try
            {
                var serializer = new XmlSerializer(o.GetType());
                using (var fs = new FileStream(path, FileMode.Create))
                {
                    var writer = new XmlTextWriter(fs, Encoding.Unicode);
                    // Serialize using the XmlTextWriter.
                    serializer.Serialize(writer, o);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return false;
            }

            return true;
        }

        public static string ObjectToXml(object o)
        {
            var serializer = new XmlSerializer(o.GetType());
            using (var s = new StringWriter())
            {
                serializer.Serialize(s, o);
                return s.ToString();
            }
        }

        public static void XmlToObject(string xml, ref object objectToDeserialise)
        {
            var serializer = new XmlSerializer(objectToDeserialise.GetType());
            using (var sr = new StringReader(xml))
            {
                objectToDeserialise = serializer.Deserialize(sr);
            }
        }

        static public string PrettyPrint(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            return PrettyPrint(doc);
        }

        static public string PrettyPrint(this XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }
            return sb.ToString();
        }
    }
}
