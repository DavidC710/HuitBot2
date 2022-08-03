namespace BotH.Models
{
    public class ArbitrageResult
    {
        public decimal BTC { get; set; }
        public decimal ETH { get; set; }
        public List<CoinsList> Coins { get; set; }
    }
}
