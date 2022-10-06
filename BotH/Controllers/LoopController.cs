
namespace BotH.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoopController : ControllerBase
    {
        public Root configuration;
        public DateTime now;

        public LoopController()
        {
            string path = @"D:\Documents\Repos\BotH\BotH\Configuration\coinsData.json";
            this.configuration = JsonConvert.DeserializeObject<Root>(System.IO.File.ReadAllText(path))!;
            now = DateTime.Today;
        }

        public async Task<ResponseMessage> Loop1(LoopInput order)
        {
            ResponseMessage response = new ResponseMessage();

            var ftxClient = new FTXClient(new FTXClientOptions()
            {
                ApiCredentials = new ApiCredentials(configuration.Exchange_ApiData.FirstOrDefault()!.ApiKeySub!, configuration.Exchange_ApiData.FirstOrDefault()!.SecretSub!),
                LogLevel = LogLevel.Trace,
                SubaccountName = "BTC"
            });

            var historicPrices = await ftxClient.TradeApi.ExchangeData.GetKlinesAsync("BTC/USDT",
                FTX.Net.Enums.KlineInterval.FifteenMinutes, DateTime.Today.Date, DateTime.Today.Date.AddHours(24));

            var mm20Records = historicPrices.Data.OrderByDescending(t => t.OpenTime).Take(20).ToList();
            var mm8Records = mm20Records.Take(8);

            var mm8 = mm8Records.Sum(t => t.ClosePrice) / mm8Records.Count();
            var mm20 = mm20Records.Sum(t => t.ClosePrice) / mm20Records.Count();

            var diff = Math.Abs(mm20 - mm8);

            return response;

        }

        [HttpPost]
        public async Task<ResponseMessage> Loop(LoopInput order)
        {
            try
            {
                var ftxClient = new FTXClient(new FTXClientOptions()
                {
                    ApiCredentials = new ApiCredentials(configuration.Exchange_ApiData.FirstOrDefault()!.ApiKeySub!, configuration.Exchange_ApiData.FirstOrDefault()!.SecretSub!),
                    LogLevel = LogLevel.Trace,
                    SubaccountName = "BTC"
                });
                await ftxClient.TradeApi.CommonSpotClient.GetOpenOrdersAsync("BTC/USDT");

                NewOrder firstOrder = new NewOrder();
                NewOrder secondOrder = new NewOrder();
                bool firstOrderSent = false;

                ResponseMessage response = new ResponseMessage();
                DateTime date = new DateTime(now.Year, now.Month, now.Day, Convert.ToInt16(configuration.BTCLoopStart_Hour), Convert.ToInt16(configuration.BTCLoopStart_Minute), 0);
                DateTime refDate = date.AddMinutes(Convert.ToInt16(configuration.BTCLoop_Duration));
                var activeLoop = (DateTime.Now >= date && DateTime.Now <= refDate);

                var historicPrices = await ftxClient.TradeApi.ExchangeData.GetKlinesAsync("BTC/USDT",
                FTX.Net.Enums.KlineInterval.FifteenMinutes, DateTime.Today.Date, DateTime.Today.Date.AddHours(24));

                var lastPrice = historicPrices.Data.OrderByDescending(t => t.OpenTime).FirstOrDefault();
                var mm20Records = historicPrices.Data.OrderByDescending(t => t.OpenTime).Take(20).ToList();
                var mm8Records = mm20Records.Take(8);
                var standarDeviationRecords = mm20Records.Take(9);
                var priceChangeData = mm20Records.Take(2);

                var priceChangeDiff = Math.Abs(priceChangeData.FirstOrDefault()!.ClosePrice - priceChangeData.LastOrDefault()!.ClosePrice);
                var movementSpeed = Convert.ToDouble(Math.Abs((lastPrice!.ClosePrice - mm8Records.LastOrDefault()!.ClosePrice)/ mm8Records.Count()));

                var deviationList = standarDeviationRecords.Select(t => Convert.ToDouble(t.ClosePrice)).ToList();
                var appliedDiffList = new List<double>();

                for (int i = 0; i < deviationList.Count() - 1  ; i++) {
                    appliedDiffList.Add(deviationList[i + 1] - deviationList[i]);
                }

                var organizedList = appliedDiffList.AsEnumerable<double>();

                var volatility = CalculateStandardDeviation(organizedList);

                var directionalRatio = movementSpeed / volatility;

                var mm8 = mm8Records.Sum(t => t.ClosePrice) / mm8Records.Count();
                var mm20 = mm20Records.Sum(t => t.ClosePrice) / mm20Records.Count();

                var diffLastPrice = Math.Abs(lastPrice!.ClosePrice - mm8);
                var diff = Math.Abs(mm20 - mm8);

                if (!activeLoop || diff > 53 || diffLastPrice > 53 || directionalRatio > 0.4)
                {
                    response.Message = "Can't run this process right now. Outside range: " + (!activeLoop ? "YES. " : "NO. ") + "Difference: " + diff.ToString()
                        + ". LastPriceDifference: " + diffLastPrice.ToString() + ". DirectionalRatio: " + directionalRatio.ToString();
                    return response;
                }

                switch (order.type)
                {
                    case "BUY":
                        firstOrder = new NewOrder(OrderSide.Buy)
                        {
                            symbol = "BTC/USDT",
                            quantity = order.quantity,
                            price = order.floor,
                        };
                        secondOrder = new NewOrder(OrderSide.Sell)
                        {
                            symbol = "BTC/USDT",
                            quantity = order.quantity,
                            price = order.top,
                        };
                        break;
                    case "SELL":
                        firstOrder = new NewOrder(OrderSide.Sell)
                        {
                            symbol = "BTC/USDT",
                            quantity = order.quantity,
                            price = order.top,
                        };
                        secondOrder = new NewOrder(OrderSide.Buy)
                        {
                            symbol = "BTC/USDT",
                            quantity = order.quantity,
                            price = order.floor,
                        };
                        break;
                }

                var firstOrderId = string.Empty;
                var secondOrderId = string.Empty;

                while (activeLoop)
                {
                    if (!firstOrderSent)
                    {
                        var firstOrderResponse = await ftxClient.TradeApi.CommonSpotClient.PlaceOrderAsync(
                                                                                             firstOrder.symbol,
                                                                                             (CommonOrderSide)firstOrder.orderSide,
                                                                                             (CommonOrderType)firstOrder.spotOrderType,
                                                                                             firstOrder.quantity,
                                                                                             (decimal)firstOrder.price);

                        if (!firstOrderResponse.Success)
                        {
                            response.Message += firstOrderResponse.Error!.ToString() + ". ";
                            return response;
                        }
                        firstOrderId = firstOrderResponse.Data.Id;
                        firstOrderSent = true;
                    }
                    else
                    {
                        var firstOrderOpenInfo = await ftxClient.TradeApi.CommonSpotClient.GetOpenOrdersAsync("BTC/USDT");
                        var firstOrderInfo = await ftxClient.TradeApi.CommonSpotClient.GetOrderAsync(firstOrderId);
                        var firstOrderData = firstOrderInfo.Data;

                        if (!firstOrderOpenInfo.Data.Any())
                        {
                            var secondOrderResponse = await ftxClient.TradeApi.CommonSpotClient.PlaceOrderAsync(
                            secondOrder.symbol,
                            (CommonOrderSide)secondOrder.orderSide,
                            (CommonOrderType)secondOrder.spotOrderType,
                            secondOrder.quantity,
                            (decimal)secondOrder.price);

                            if (!secondOrderResponse.Success)
                            {
                                response.Message += secondOrderResponse.Error!.ToString() + ". ";
                                return response;
                            }
                            secondOrderId = secondOrderResponse.Data.Id;

                            while (firstOrderSent)
                            {
                                var secondOrderOpenInfo = await ftxClient.TradeApi.CommonSpotClient.GetOpenOrdersAsync("BTC/USDT");
                                var secondOrderInfo = await ftxClient.TradeApi.CommonSpotClient.GetOrderAsync(secondOrderId);
                                var secondOrderData = secondOrderInfo.Data;
                                if (!secondOrderOpenInfo.Data.Any())
                                {
                                    firstOrderSent = false;
                                }
                            }
                        }
                    }

                    activeLoop = (DateTime.Now >= date && DateTime.Now <= refDate);
                }

                return response;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + ". " + ex.StackTrace);
            }
        }

        private double CalculateStandardDeviation(IEnumerable<double> values)
        {
            double standardDeviation = 0;

            if (values.Any())
            {
                // Compute the average.     
                double avg = values.Average();

                // Perform the Sum of (value-avg)_2_2.      
                double sum = values.Sum(d => Math.Pow(d - avg, 2));

                // Put it all together.      
                standardDeviation = Math.Sqrt((sum) / (values.Count() - 1));
            }

            return standardDeviation;
        }
    }
}