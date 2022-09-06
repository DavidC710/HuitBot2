namespace BotH.Models
{
    public class ArbitrageResult
    {
        public string Exchange { get; set; }
        public decimal BTC { get; set; }
        public decimal ETH { get; set; }
        public string TimeToFinish { get; set; }
        public List<CoinsList> Coins { get; set; }
    }
}
