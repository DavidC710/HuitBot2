
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
                //var ttt = 1;

                //if (ttt == 1) return await Loop1(order);

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

                var mm20Records = historicPrices.Data.OrderByDescending(t => t.OpenTime).Take(20).ToList();
                var mm8Records = mm20Records.Take(8);

                var mm8 = mm8Records.Sum(t => t.ClosePrice) / mm8Records.Count();
                var mm20 = mm20Records.Sum(t => t.ClosePrice) / mm20Records.Count();

                var diff = Math.Abs(mm20 - mm8);

                if (!activeLoop || diff > 53)
                {
                    response.Message = "Can't run this process right now. Outside range: " + (!activeLoop ? "YES. " : "NO. ") + "Difference: " + diff.ToString();
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
    }
}