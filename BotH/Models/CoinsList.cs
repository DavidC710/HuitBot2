namespace BotH.Models
{
    public class CoinsList
    {
        public string Symbol { get; set; }

        public string Percentage { get; set; }

        public string BTC { get; set; }

        public string USDT { get; set; }

        public decimal Price { get; set; }

        public double Quantity { get; set; }

        public decimal FirstQuantity { get; set; }

        public decimal LastPrice { get; set; }
        public decimal BtcPrice { get; set; }

        public decimal EthPrice { get; set; }
        public string TimeToFinish { get; set; }
        public bool HasOpendOrders { get; set; } = false;
    }
}
