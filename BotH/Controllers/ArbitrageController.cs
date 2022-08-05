
using FTX.Net.Objects.Models;

namespace BotH.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ArbitrageController : ControllerBase
    {
        private readonly ILogger<ArbitrageController> _logger;
        protected readonly IConfiguration _configuration;

        public ArbitrageController(ILogger<ArbitrageController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }


        [HttpPost]
        public async Task<ResponseMessage> CreateOrder(OrdersInput order)
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

                var resui = new ResponseMessage();

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
                    resui.Message = ""; 
                }


                resui.Message = "All orders placed succesfully.";

                return resui;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [HttpGet]
        public async Task<List<ArbitrageResult>> Get()
        {
            var coinsConfig = _configuration["BaseCoinsBinance"];
            var coinsConfigFTX = _configuration["BaseCoinsFTX"];
            var orderCoinsConfig = _configuration["OrderCoins"];
            var sufixesConfig = _configuration["Sufixes"];

            var mainBaseCoin = string.Empty;
            var secondBaseCoin = string.Empty;
            var mainBaseCoinFTX = string.Empty;
            var secondBaseCoinFTX = string.Empty;
            decimal btcUsdtBidBinance = 0;
            decimal ethUsdtBidBinance = 0;
            decimal btcUsdtBidFTX = 0;
            decimal ethUsdtBidFTX = 0;

            var coinsL = coinsConfig.Split(';');
            var coinsLFTX = coinsConfigFTX.Split(';');
            var orderCoinsL = orderCoinsConfig.Split(';');
            var sufixesL = sufixesConfig.Split(';');

            var baseCoins = new List<string>();
            var baseCoinsFTX = new List<string>();
            var sufixes = new List<string>();
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

            if (coinsLFTX.Any())
            {
                foreach (var coinL in coinsLFTX)
                {
                    baseCoinsFTX.Add(coinL);
                }
                mainBaseCoinFTX = baseCoinsFTX.FirstOrDefault();
                secondBaseCoinFTX = baseCoinsFTX.LastOrDefault();
            }

            if (sufixesL.Any())
            {
                foreach (var sufix in sufixesL)
                {
                    sufixes.Add(sufix);
                }
            }

            if (orderCoinsL.Any())
            {
                foreach (var orderCoinL in orderCoinsL)
                {
                    coins.Add(new Coin()
                    {
                        Symbol = orderCoinL,
                        BTC = orderCoinL + sufixes.FirstOrDefault(),
                        USDT = orderCoinL + sufixes.LastOrDefault(),
                        BTCFTX = orderCoinL + "/" + sufixes.FirstOrDefault(),
                        USDTFTX = orderCoinL + "/" + sufixes.LastOrDefault(),
                    });
                }
            }

            var ftxClient = new FTXClient();
            var symbolssDataFTX = await ftxClient.TradeApi.ExchangeData.GetSymbolsAsync();

            //if (!System.IO.File.Exists(fullPath))
            //{
            //    using (StreamWriter sw = System.IO.File.CreateText(fullPath))
            //    {
            //        sw.WriteLine("Symbol;Ask;Bid");
            //        foreach (var c in aw.Data)
            //        {
            //            sw.WriteLine(c.Name + ";" + c.BestAskPrice + ";" + c.BestBidPrice);
            //        }
            //    }
            //}

            var client = new BinanceClient(new BinanceClientOptions
            {
                ApiCredentials = new ApiCredentials(_configuration["ApiKey"], _configuration["ApiSecret"])
            });

            var result = new List<ArbitrageResult>();

            var callResult = await client.SpotApi.ExchangeData.GetBookPricesAsync();
            var basePrices = await client.SpotApi.ExchangeData.GetPricesAsync();

            IEnumerable<BinanceBookPrice> coinsDataBinance = callResult.Data;
            IEnumerable<FTXSymbol> coinsDataFTX = symbolssDataFTX.Data;

            btcUsdtBidBinance = (decimal)coinsDataBinance.FirstOrDefault(t => t.Symbol == mainBaseCoin).BestBidPrice;
            ethUsdtBidBinance = (decimal)coinsDataBinance.FirstOrDefault(t => t.Symbol == secondBaseCoin).BestBidPrice;

            btcUsdtBidFTX = (decimal)coinsDataFTX.FirstOrDefault(t => t.Name == mainBaseCoinFTX).BestBidPrice;
            ethUsdtBidFTX = (decimal)coinsDataFTX.FirstOrDefault(t => t.Name == secondBaseCoinFTX).BestBidPrice;

            var btcPagePriceBinance = (decimal)basePrices.Data.FirstOrDefault(t => t.Symbol == mainBaseCoin).Price;
            var ethPagePriceBinance = (decimal)basePrices.Data.FirstOrDefault(t => t.Symbol == secondBaseCoin).Price;

            var btcPagePriceFTX = (decimal)symbolssDataFTX.Data.FirstOrDefault(t => t.Name == mainBaseCoinFTX).LastPrice;
            var ethPagePriceFTX = (decimal)symbolssDataFTX.Data.FirstOrDefault(t => t.Name == secondBaseCoinFTX).LastPrice;

            var binanceResult = new ArbitrageResult();
            binanceResult.Exchange = "binance";
            binanceResult.Coins = new List<CoinsList>();
            binanceResult.BTC = btcPagePriceBinance;
            binanceResult.ETH = ethPagePriceBinance;

            var ftxResult = new ArbitrageResult();
            ftxResult.Exchange = "ftx";
            ftxResult.Coins = new List<CoinsList>();
            ftxResult.BTC = btcPagePriceFTX;
            ftxResult.ETH = ethPagePriceFTX;

            foreach (var coin in coins)
            {
                var binanceCoinBid = coinsDataBinance.FirstOrDefault(t => t.Symbol == coin.BTC).BestBidPrice;
                var binanceCoinAsk = coinsDataBinance.FirstOrDefault(t => t.Symbol == coin.USDT).BestAskPrice;

                var ftxCoinBid = (decimal)coinsDataFTX.FirstOrDefault(t => t.Name == coin.BTCFTX).BestBidPrice;
                var ftxCoinAsk = (decimal)coinsDataFTX.FirstOrDefault(t => t.Name == coin.USDTFTX).BestAskPrice;

                var valBinance = (((1 / binanceCoinBid) * binanceCoinAsk) / btcUsdtBidBinance) > 1 ? (((1 / binanceCoinBid) * binanceCoinAsk) / btcUsdtBidBinance) - 1 : 0;
                var valFTX = (((1 / ftxCoinBid) * ftxCoinAsk) / btcUsdtBidFTX) > 1 ? (((1 / ftxCoinBid) * ftxCoinAsk) / btcUsdtBidFTX) - 1 : 0;

                binanceResult.Coins.Add(new CoinsList()
                {
                    Symbol = coin.Symbol,
                    Percentage = Math.Round(((valBinance / 1) * 100), 5).ToString() + "%",
                    BTC = coin.BTC,
                    USDT = coin.USDT,
                    Price = binanceCoinBid,
                    Quantity = 0,
                    FirstQuantity = Math.Round(binanceCoinAsk, 3),
                    LastPrice = Math.Round(btcUsdtBidBinance, 3),
                });

                ftxResult.Coins.Add(new CoinsList()
                {
                    Symbol = coin.Symbol,
                    Percentage = Math.Round(((valFTX / 1) * 100), 5).ToString() + "%",
                    BTC = coin.BTCFTX,
                    USDT = coin.USDTFTX,
                    Price = ftxCoinBid,
                    Quantity = 0,
                    FirstQuantity = Math.Round(ftxCoinAsk, 3),
                    LastPrice = Math.Round(btcUsdtBidFTX, 3),
                });
            }

            result.Add(binanceResult);
            result.Add(ftxResult);

            
            return result;
        }
    }
}