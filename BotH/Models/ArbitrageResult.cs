namespace BotH.Models
{
    public class ArbitrageResult
    {
        public string Exchange { get; set; }
        public decimal BTC { get; set; }
        public decimal ETH { get; set; }
        public decimal Difference { get; set; }
        public decimal LastPriceDifference { get; set; }
        public double DirectionalRatio { get; set; }
        public bool CandleCanOperate { get; set; }
        public string TimeToFinish { get; set; }
        public List<CoinsList> Coins { get; set; }
    }
}
