namespace BotH.Models
{
    public class OrdersInput
    {
        public string seller { get; set; }
        public string buyer { get; set; }
        public decimal price { get; set; }
        public decimal quantity { get; set; }
        public decimal ask { get; set; }
        public decimal lastPrice { get; set; }
    }
}
