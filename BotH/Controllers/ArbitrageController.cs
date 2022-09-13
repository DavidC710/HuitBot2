
namespace BotH.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ArbitrageController : ControllerBase
    {
        public Root configuration;
        public DateTime now;
        public int counter = 0;

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
                    var orderResponse = await ftxClient.TradeApi.CommonSpotClient.PlaceOrderAsync(
                        ord.symbol,
                        (CommonOrderSide)ord.orderSide,
                        (CommonOrderType)ord.spotOrderType,
                        ord.quantity,
                        (decimal)ord.price);

                    if (!orderResponse.Success) response.Message += orderResponse.Error!.ToString() + ". ";
                }

                response.Message += "All orders placed succesfully.";

                return response;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + ". " + ex.StackTrace);
            }
        }

        public async Task<ResponseMessage> GetDailyReport()
        {
            try
            {
                //var fullPath = @"C:\Users\ThermalTake\Documents\Bot\Reportes\data_" +
                var fullPath = @"D:\Documents\DCC\data_" +
                now.Date.AddDays(-1).Year.ToString()
                        + now.Date.AddDays(-1).Month.ToString()
                        + now.Date.AddDays(-1).Day.ToString() + ".csv";
                if (System.IO.File.Exists(fullPath)) return null;

                FTXClient ftxClient = new FTXClient();
                ftxClient.SetApiCredentials(new ApiCredentials(configuration.Exchange_ApiData.FirstOrDefault()!.ApiKey, configuration.Exchange_ApiData.FirstOrDefault()!.Secret));

                var baseCoins = configuration.BaseCoins;
                var startDate = now.Date.AddDays(-1);
                var endtDate = now.Date.AddSeconds(-1);
                var ordersInfo = await ftxClient.TradeApi.Trading.GetOrdersAsync(null, startDate, endtDate);
                var orders = ordersInfo.Data;

                var reportList = new ReportCoinsList();

                foreach (var coin in configuration.TradeCoins)
                {
                    var coinsOrders = orders.Where(t => t.Symbol.Contains(coin.Name) && t.ClientOrderId == null).OrderByDescending(t => t.CreateTime).ToList();
                    var counter = 1;
                    if (coinsOrders.Any())
                    {
                        for (int i = 0; i < coinsOrders.Count ; i++)
                        {
                            var insertCoin = new ReportCoins();
                            insertCoin.CoinName = coin.Name;
                            coinsOrders[i].CreateTime.AddHours(-5);
                            if (i + 1 == coinsOrders.Count) break;
                            coinsOrders[i + 1].CreateTime.AddHours(-5);
                            insertCoin.Orders.Add(coinsOrders[i]);
                            insertCoin.Orders.Add(coinsOrders[i + 1]);
                            i += 1;
                            insertCoin.Order = counter;
                            counter += 1;
                            reportList.CoinsList.Add(insertCoin);
                        }
                    }
                }

                using (StreamWriter sw = System.IO.File.CreateText(fullPath))
                {
                    sw.WriteLine("Date;" +
                                "CoinName;" +
                                "BTCInvestment;" +
                                "BTCProfit;" +
                                "BTCProfitPercentage;" +
                                "BTCBuy;" +
                                "BTCPrice;" +
                                "Error");

                    foreach (var item in reportList.CoinsList)
                    {
                        long idC = 0;
                        idC = item.Orders.FirstOrDefault()!.Id;
                        var btcOrder = orders.Where(t => t.Symbol.Contains(baseCoins.FirstOrDefault()!.ConfigCoins.FirstOrDefault()!.Name) && t.Id > idC).OrderBy(t => t.CreateTime).FirstOrDefault();
                        var divisor = btcOrder!.QuantityFilled * btcOrder!.AverageFillPrice;
                        var firstFactor = item.Orders.FirstOrDefault()!.QuantityFilled * item.Orders.FirstOrDefault()!.AverageFillPrice;
                        if (divisor == null) continue;
                        var quantity = (firstFactor * btcOrder.QuantityFilled) / divisor;
                        var btcInvestment = (item.Orders.LastOrDefault()!.QuantityFilled * item.Orders.LastOrDefault()!.AverageFillPrice);
                        var btcProfit = (quantity - (item.Orders.LastOrDefault()!.QuantityFilled * item.Orders.LastOrDefault()!.AverageFillPrice));
                        var btcProfitPercentage = (item.Orders.FirstOrDefault()!.AverageFillPrice == null || item.Orders.LastOrDefault()!.AverageFillPrice == null || btcOrder.AverageFillPrice == 0) ? "ERROR" : ((btcProfit / btcInvestment) * 100).ToString() + "%";
                        var btcBuy = btcOrder.QuantityFilled - quantity;
                        var btcPrice = btcOrder.AverageFillPrice == 0 ? "FALSO" : (btcOrder.AverageFillPrice).ToString();
                        var error = (item.Orders.FirstOrDefault()!.AverageFillPrice == null || item.Orders.LastOrDefault()!.AverageFillPrice == null) ? (btcOrder.Symbol + "Price: $" +
                            btcOrder.Price.ToString() + ", Quantity: " + btcOrder.Quantity.ToString()
                            + ". " + item.Orders.FirstOrDefault()!.Symbol + "Price: $" +
                            item.Orders.FirstOrDefault()!.Price.ToString() + ", Quantity: " + item.Orders.FirstOrDefault()!.Quantity.ToString()
                            + ". " + item.Orders.LastOrDefault()!.Symbol + "Price: $" +
                            item.Orders.LastOrDefault()!.Price.ToString() + ", Quantity: " + item.Orders.LastOrDefault()!.Quantity.ToString() + ".").ToString() : "";
                        sw.WriteLine(item.Orders.FirstOrDefault()!.CreateTime + ";" +
                                     item.CoinName + ";" +
                                     btcInvestment.ToString() + ";" +
                                     btcProfit.ToString() + ";" +
                                     btcProfitPercentage + ";" +
                                     btcBuy.ToString() + ";" +
                                     btcPrice + ";" +
                                     error
                                     );
                    }
                }

                var result = new ResponseMessage();

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }         
        }

        [HttpGet]
        public async Task<List<ArbitrageResult>> Get()
        {
            try
            {
                await GetDailyReport();
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
                        var opnOrders = await ftxClient.TradeApi.CommonSpotClient.GetOpenOrdersAsync();
                        var ordersBTCInfo = await ftxClient.TradeApi.CommonSpotClient.GetOpenOrdersAsync(coin.BTCFTX);
                        var ordersUSDTInfo = await ftxClient.TradeApi.CommonSpotClient.GetOpenOrdersAsync(coin.USDTFTX);

                        bool openedOrders = (
                                            ordersBTCInfo.Data.Any() ||
                                            ordersUSDTInfo.Data.Any() ||
                                            opnOrders.Data.Where(t => t.Symbol == coin.BTCFTX).Any() ||
                                            opnOrders.Data.Where(t => t.Symbol == coin.USDTFTX).Any())
                                            ? true : false;
                        decimal perc = Math.Round(((valFTX / 1) * 100), 5);
                        DateTime refDate = date.AddMinutes(Convert.ToInt16(configuration.AutomaticProcess_Duration));
                        //bool haveToWait = (DateTime.Now.Minute == date.Minute) && (DateTime.Now.Hour == date.Hour);

                        if (perc > (decimal)configuration.ArbitragePercentageValue && !openedOrders && DateTime.Now >= date && DateTime.Now <= refDate)
                        {
                            //if(haveToWait) Thread.Sleep(60000);

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
                    var refDate = date.AddMinutes(Convert.ToInt16(configuration.AutomaticProcess_Duration));
                    var timeToFinish = "OFF";

                    if (DateTime.Now >= date && DateTime.Now <= refDate) {
                        TimeSpan ts = refDate - DateTime.Now;
                        timeToFinish = ts.Hours.ToString().PadLeft(2, '0') + ":" + ts.Minutes.ToString().PadLeft(2, '0') + ":" + ts.Seconds.ToString().PadLeft(2, '0');
                        
                    }

                    ftxResult.TimeToFinish = timeToFinish;

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