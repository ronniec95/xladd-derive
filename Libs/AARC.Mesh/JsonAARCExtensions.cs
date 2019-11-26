using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class JsonAARCExtensions
    {

        public static bool ValidateJSON(this string s)
        {
            try
            {
                JToken.Parse(s);
                return true;
            }
            catch (JsonReaderException ex)
            {
                return false;
            }
        }
    }