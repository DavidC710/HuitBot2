namespace BotH.Models
{
    public class Candle
    {
        public string Order { get; set; }

        public string Type { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
    }
}
