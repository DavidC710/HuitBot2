
using Binance.Net.Enums;

namespace BotH.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;
        protected readonly IConfiguration _configuration;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }


        [HttpPost]
        public async Task<string> CreateOrder(OrdersInput order)
        {
            var coinsConfig = _configuration["BaseCoins"];
            var coinsL = coinsConfig.Split(';');
            var baseCoins = new List<string>();
            var mainBaseCoin = string.Empty;

            var client = new BinanceClient(new BinanceClientOptions
            {
                ApiCredentials = new ApiCredentials(_configuration["ApiKey"], _configuration["ApiSecret"])
            });

            if (coinsL.Any())
            {
                foreach (var coinL in coinsL)
                {
                    baseCoins.Add(coinL);
                }
                mainBaseCoin = baseCoins.FirstOrDefault();
            }

            var ordersList = new List<NewOrder>();
            var cont = 1;
            try
            {
                ordersList.Add(new NewOrder(OrderSide.Buy)
                {
                    symbol = order.buyer,
                    quantity = Math.Round(order.quantity / order.ask, 0),
                    price = order.price,
                });

                ordersList.Add(new NewOrder(OrderSide.Sell)
                {
                    symbol = order.seller,
                    quantity = Math.Round(order.quantity / order.ask, 0),
                    price = order.ask,
                });

                ordersList.Add(new NewOrder(OrderSide.Buy)
                {
                    symbol = mainBaseCoin,
                    quantity = Math.Round(order.quantity / order.lastPrice, 5),
                    price = order.lastPrice,
                });


                foreach (var ord in ordersList)
                {
                    await client.SpotApi.Trading.PlaceOrderAsync(
                        ord.symbol,
                        ord.orderSide,
                        ord.spotOrderType,
                        ord.quantity, null, null,
                        (decimal)ord.price,
                        timeInForce: ord.timeInForce);
                    cont += 1;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
           
          
            return "";
        }

        [HttpGet]
        public async Task<ArbitrageResult> Get()
        {
            var coinsConfig = _configuration["BaseCoins"];
            var orderCoinsConfig = _configuration["OrderCoins"];

            var mainBaseCoin = string.Empty;
            var secondBaseCoin = string.Empty;
            decimal btcUsdtBid = 0;
            decimal ethUsdtBid = 0;

            var coinsL = coinsConfig.Split(';');
            var orderCoinsL = orderCoinsConfig.Split(';');

            var baseCoins = new List<string>();
            var coins = new List<Coin>();

            if (coinsL.Any())
            {
                foreach (var coinL in coinsL)
                {
                    baseCoins.Add(coinL);
                }
                mainBaseCoin = baseCoins.FirstOrDefault();
                secondBaseCoin = baseCoins.LastOrDefault();
            }

            if (orderCoinsL.Any())
            {
                foreach (var orderCoinL in orderCoinsL)
                {
                    coins.Add(new Coin()
                    {
                        Symbol = orderCoinL,
                        BTC = orderCoinL + "BTC",
                        USDT = orderCoinL + "USDT",
                    });
                }
            }


            var client = new BinanceClient(new BinanceClientOptions
            {
                ApiCredentials = new ApiCredentials(_configuration["ApiKey"], _configuration["ApiSecret"])
            });

            var result = new ArbitrageResult();


            var callResult = await client.SpotApi.ExchangeData.GetBookPricesAsync();

            var coinsData = callResult.Data;

            btcUsdtBid = coinsData.FirstOrDefault(t => t.Symbol == mainBaseCoin).BestBidPrice;
            ethUsdtBid = coinsData.FirstOrDefault(t => t.Symbol == secondBaseCoin).BestBidPrice;

            result.Coins = new List<CoinsList>();
            result.BTC = btcUsdtBid;
            result.ETH = ethUsdtBid;

            foreach (var coin in coins)
            {
                var binanceCoinBid = coinsData.FirstOrDefault(t => t.Symbol == coin.BTC).BestBidPrice;
                var binanceCoinAsk = coinsData.FirstOrDefault(t => t.Symbol == coin.USDT).BestAskPrice;

                var val = (((1 / binanceCoinBid) * binanceCoinAsk) / btcUsdtBid) > 1 ? (((1 / binanceCoinBid) * binanceCoinAsk) / btcUsdtBid) - 1 : 0;

                result.Coins.Add(new CoinsList()
                {
                    Symbol = coin.Symbol,
                    Percentage = Math.Round(((val / 1) * 100), 5).ToString() + "%",
                    BTC = coin.BTC,
                    USDT = coin.USDT,
                    Price = binanceCoinBid,
                    Quantity = 11,
                    FirstQuantity = Math.Round(binanceCoinAsk, 3),
                    LastPrice = Math.Round(btcUsdtBid, 3),
                });
            }

            return result;
        }
    }
}