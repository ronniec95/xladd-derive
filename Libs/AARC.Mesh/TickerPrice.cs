using Newtonsoft.Json;

namespace AARC.Mesh
{
    public partial class TickerPrice
    {
        public string Ticker { get; set; }
        public double Price { get; set; }
        public override string ToString() => $"{Ticker}={Price}";
    }

    /// <summary>
    /// idea here is to create extension methods to handle serialization in a generic way
    /// </summary>
    public partial class TickerPrice
    {
        public static TickerPrice Deserialise(string message) => JsonConvert.DeserializeObject<TickerPrice>(message);

        public static TickerPrice Deserialise<T>(object message)
        {
            if (typeof(T) == typeof(string))
                return TickerPrice.Deserialise((string)message);
            return null;
        }
    }

    public static class TickerPriceExt
    {
        public static string Serialize(this TickerPrice tp) => JsonConvert.SerializeObject(tp);
        public static T Serialize<T>(this TickerPrice tp) where T : class
        {
            if (typeof(T) == typeof(string))
                return Serialize(tp) as T;
            return null;
        }
    }
}
