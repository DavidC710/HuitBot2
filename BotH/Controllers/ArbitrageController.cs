
namespace BotH.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ArbitrageController : ControllerBase
    {
        public Root configuration;
        public DateTime now;

        public ArbitrageController()
        {
            string path = @"D:\Documents\Repos\BotH\BotH\Configuration\coinsData.json";
            this.configuration = JsonConvert.DeserializeObject<Root>(System.IO.File.ReadAllText(path))!;
            now = DateTime.Today;
        }


        [HttpPost]
        public async Task<ResponseMessage> CreateOrder(OrdersInput order)
        {
            try
            {
                var ordersList = new List<NewOrder>();
                var mainBaseFTXCoin = configuration.BaseCoins.FirstOrDefault()!.ConfigCoins.FirstOrDefault()!.Name;
                var quantity = order.quantity / order.price;
                ResponseMessage response = new ResponseMessage();
                var ftxClient = new FTXClient();
                ftxClient.SetApiCredentials(new ApiCredentials(configuration.Exchange_ApiData.FirstOrDefault()!.ApiKey!, configuration.Exchange_ApiData.FirstOrDefault()!.Secret!));

                ordersList.Add(new NewOrder(OrderSide.Buy)
                {
                    symbol = order.buyer,
                    quantity = quantity,
                    price = order.price,
                });

                ordersList.Add(new NewOrder(OrderSide.Sell)
                {
                    symbol = order.seller,
                    quantity = quantity,
                    price = order.ask,
                });

                ordersList.Add(new NewOrder(OrderSide.Buy)
                {
                    symbol = mainBaseFTXCoin,
                    quantity = order.quantity,
                    price = order.lastPrice,
                });

                foreach (var ord in ordersList)
                {
                    var trer = await ftxClient.TradeApi.CommonSpotClient.GetOpenOrdersAsync();

                    var orderResponse = await ftxClient.TradeApi.CommonSpotClient.PlaceOrderAsync(
                        ord.symbol,
                        (CommonOrderSide)ord.orderSide,
                        (CommonOrderType)ord.spotOrderType,
                        ord.quantity,
                        (decimal)ord.price);

                    if(!orderResponse.Success) response.Message += orderResponse.Error!.ToString() + ". ";
                }

                response.Message += "All orders placed succesfully.";

                return response;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + ". " + ex.StackTrace);
            }
        }

        [HttpGet]
        public async Task<List<ArbitrageResult>> Get()
        {
            try
            {
                List<ArbitrageResult> result = new List<ArbitrageResult>();
                DateTime date = new DateTime(now.Year, now.Month, now.Day, Convert.ToInt16(configuration.StartProcess_Hour), Convert.ToInt16(configuration.StartProcess_Minute), 0);
                string mainBaseCoinFTX = configuration.BaseCoins.FirstOrDefault()!.ConfigCoins.FirstOrDefault()!.Name!;
                string secondBaseCoinFTX = configuration.BaseCoins.FirstOrDefault()!.ConfigCoins.LastOrDefault()!.Name!;
                List<Coin> coins = new List<Coin>();
                FTXClient ftxClient = new FTXClient();
                ftxClient.SetApiCredentials(new ApiCredentials(configuration.Exchange_ApiData.FirstOrDefault()!.ApiKey, configuration.Exchange_ApiData.FirstOrDefault()!.Secret));
                var ftxInfo = await ftxClient.TradeApi.ExchangeData.GetSymbolsAsync();
                IEnumerable<FTXSymbol> coinsDataFTX = ftxInfo.Data;
                decimal bid_BTDUSDT_FTX = (decimal)coinsDataFTX.FirstOrDefault(t => t.Name == mainBaseCoinFTX)!.BestBidPrice!;
                decimal bid_ETHUSDT_FTX = (decimal)coinsDataFTX.FirstOrDefault(t => t.Name == secondBaseCoinFTX)!.BestBidPrice!;
                var page_BTCPrice_FTX = (decimal)coinsDataFTX.FirstOrDefault(t => t.Name == mainBaseCoinFTX)!.LastPrice!;
                var page_ETHPrice_FTX = (decimal)coinsDataFTX.FirstOrDefault(t => t.Name == secondBaseCoinFTX)!.LastPrice!;

                var ftxResult = new ArbitrageResult();
                ftxResult.Exchange = "ftx";
                ftxResult.Coins = new List<CoinsList>();
                ftxResult.BTC = page_BTCPrice_FTX;
                ftxResult.ETH = page_ETHPrice_FTX;

                foreach (var orderCoin in configuration.TradeCoins)
                {
                    coins.Add(new Coin()
                    {
                        Symbol = orderCoin.Name,
                        BTC = orderCoin.Name + configuration.Sufixes.FirstOrDefault()!.Sufix,
                        USDT = orderCoin.Name + configuration.Sufixes.LastOrDefault()!.Sufix,
                        BTCFTX = orderCoin.Name + "/" + configuration.Sufixes.FirstOrDefault()!.Sufix,
                        USDTFTX = orderCoin.Name + "/" + configuration.Sufixes.LastOrDefault()!.Sufix,
                        IsAutomatic = configuration.AutomaticProcess_Coins.FirstOrDefault()!.ConfigCoins.Where(t => t.Name == orderCoin.Name).Any(),
                    });
                }

                foreach (var coin in coins)
                {
                    var ftxCoinBid = (decimal)coinsDataFTX.FirstOrDefault(t => t.Name == coin.BTCFTX)!.BestBidPrice!;
                    var ftxCoinAsk = (decimal)coinsDataFTX.FirstOrDefault(t => t.Name == coin.USDTFTX)!.BestAskPrice!;
                    var valFTX = (((1 / ftxCoinBid) * ftxCoinAsk) / bid_BTDUSDT_FTX) > 1 ? (((1 / ftxCoinBid) * ftxCoinAsk) / bid_BTDUSDT_FTX) - 1 : 0;

                    if (coin.IsAutomatic)
                    {
                        var ordersBTCInfo = await ftxClient.TradeApi.CommonSpotClient.GetOpenOrdersAsync(coin.BTCFTX);
                        var ordersUSDTInfo = await ftxClient.TradeApi.CommonSpotClient.GetOpenOrdersAsync(coin.BTCFTX);

                        var openedOrders = (ordersBTCInfo.Data.Any() || ordersUSDTInfo.Data.Any()) ? true : false;
                        var perc = Math.Round(((valFTX / 1) * 100), 5);

                        if (perc > (decimal)configuration.ArbitragePercentageValue && !openedOrders && DateTime.Now >= date && DateTime.Now <= date.AddMinutes(Convert.ToDouble(configuration.StartProcess_Minute)))
                        {
                            await CreateOrder(new OrdersInput()
                            {
                                seller = coin.USDTFTX,
                                buyer = coin.BTCFTX,
                                price = ftxCoinBid,
                                quantity = configuration.DefaultQuantity,
                                ask = Math.Round(ftxCoinAsk, 3),
                                lastPrice = Math.Round(bid_BTDUSDT_FTX, 3),
                                exchange = "ftx",
                                percentage = Math.Round(((valFTX / 1) * 100), 5).ToString() + "%",
                            });
                        }
                    }


                }

                var myOrdersInfo = await ftxClient.TradeApi.CommonSpotClient.GetOpenOrdersAsync();
                var myOrders = myOrdersInfo.Data;

                foreach (var coin in coins)
                {
                    var ftxCoinBid = (decimal)coinsDataFTX.FirstOrDefault(t => t.Name == coin.BTCFTX)!.BestBidPrice!;
                    var ftxCoinAsk = (decimal)coinsDataFTX.FirstOrDefault(t => t.Name == coin.USDTFTX)!.BestAskPrice!;
                    var valFTX = (((1 / ftxCoinBid) * ftxCoinAsk) / bid_BTDUSDT_FTX) > 1 ? (((1 / ftxCoinBid) * ftxCoinAsk) / bid_BTDUSDT_FTX) - 1 : 0;

                    ftxResult.Coins.Add(new CoinsList()
                    {
                        Symbol = coin.Symbol,
                        Percentage = Math.Round(((valFTX / 1) * 100), 5).ToString() + "%",
                        BTC = coin.BTCFTX,
                        USDT = coin.USDTFTX,
                        Price = ftxCoinBid,
                        Quantity = 0.0005,
                        FirstQuantity = Math.Round(ftxCoinAsk, 3),
                        LastPrice = Math.Round(bid_BTDUSDT_FTX, 3),
                        HasOpendOrders = (myOrders.Where(t => t.Symbol == coin.BTCFTX).Any() || myOrders.Where(t => t.Symbol == coin.USDTFTX).Any()) ? true : false,
                    });
                }

                //var fullPath = @"C:\Users\ThermalTake\Documents\Bot\FTX Coins\data.csv";

                //if (!System.IO.File.Exists(fullPath))
                //{
                //    using (StreamWriter sw = System.IO.File.CreateText(fullPath))
                //    {
                //        sw.WriteLine("Symbol;Ask;Bid");
                //        foreach (var c in coinsDataFTX.Data)
                //        {
                //            sw.WriteLine(c.Name + ";" + c.BestAskPrice + ";" + c.BestBidPrice);
                //        }
                //    }
                //}

                result.Add(ftxResult);

                return result;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + ". " + ex.StackTrace);
            }
        }
    }
}