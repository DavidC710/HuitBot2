namespace BotH
{
    public class AutomaticProcessCoin
    {
        public string Exchange { get; set; }
        public List<ConfigCoin> ConfigCoins { get; set; }
    }

    public class BaseCoin
    {
        public string Exchange { get; set; }
        public List<ConfigCoin> ConfigCoins { get; set; }
    }

    public class ConfigCoin
    {
        public string Name { get; set; }
    }

    public class ExchangeApiDatum
    {
        public string Exchange { get; set; }
        public string ApiKey { get; set; }
        public string Secret { get; set; }
    }

    public class Root
    {
        public List<ExchangeApiDatum> Exchange_ApiData { get; set; }
        public List<Sufixes> Sufixes { get; set; }
        public List<BaseCoin> BaseCoins { get; set; }
        public List<TradeCoin> TradeCoins { get; set; }
        public List<AutomaticProcessCoin> AutomaticProcess_Coins { get; set; }
        public int AutomaticProcess_Duration { get; set; }
        public string StartProcess_Hour { get; set; }
        public string StartProcess_Minute { get; set; }
        public decimal DefaultQuantity { get; set; }
        public decimal ArbitragePercentageValue { get; set; }
    }

    public class Sufixes
    {
        public string Sufix { get; set; }
    }

    public class TradeCoin
    {
        public string Name { get; set; }
    }

    public class ReportCoinsList
    {
        public ReportCoinsList() {
            CoinsList = new List<ReportCoins>(); 
        }
        public List<ReportCoins> CoinsList { get; set; }
    }

    public class ReportCoins
    {
        public ReportCoins() {
            Orders = new List<FTXOrder>();
        }
        public string CoinName { get; set; }
        public int Order { get; set; }
        public List<FTXOrder> Orders { get; set; }
    }
}
