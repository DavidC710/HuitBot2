
namespace BotH.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ETHController : ControllerBase
    {
        public Root configuration;
        public DateTime now;

        public ETHController()
        {
            string path = @"D:\Documents\Repos\BotH\BotH\Configuration\coinsData.json";
            this.configuration = JsonConvert.DeserializeObject<Root>(System.IO.File.ReadAllText(path))!;
            now = DateTime.Today;
        }


        [HttpPost]
        public async Task<ResponseMessage> ETH(LoopInput order)
        {
            try
            {
                var ftxClient = new FTXClient(new FTXClientOptions()
                {
                    ApiCredentials = new ApiCredentials(configuration.Exchange_ApiData.FirstOrDefault()!.ApiKeySub!, configuration.Exchange_ApiData.FirstOrDefault()!.SecretSub!),
                    LogLevel = LogLevel.Trace,
                    SubaccountName = "BTC"
                });

                NewOrder firstOrder = new NewOrder();
                NewOrder secondOrder = new NewOrder();
                bool firstOrderSent = false;

                ResponseMessage response = new ResponseMessage();
                DateTime date = new DateTime(now.Year, now.Month, now.Day, Convert.ToInt16(configuration.ETHLoopStart_Hour), Convert.ToInt16(configuration.ETHLoopStart_Minute), 0);
                DateTime refDate = date.AddMinutes(Convert.ToInt16(configuration.ETHLoop_Duration));
                var activeLoop = (DateTime.Now >= date && DateTime.Now <= refDate);

                if (!activeLoop)
                {
                    response.Message = "Outside ETH-Loop Time Range.";
                    return response;
                }

                switch (order.type)
                {
                    case "BUY":
                        firstOrder = new NewOrder(OrderSide.Buy)
                        {
                            symbol = "ETH/USDT",
                            quantity = order.quantity,
                            price = order.floor,
                        };
                        secondOrder = new NewOrder(OrderSide.Sell)
                        {
                            symbol = "ETH/USDT",
                            quantity = order.quantity,
                            price = order.top,
                        };
                        break;
                    case "SELL":
                        firstOrder = new NewOrder(OrderSide.Sell)
                        {
                            symbol = "ETH/USDT",
                            quantity = order.quantity,
                            price = order.top,
                        };
                        secondOrder = new NewOrder(OrderSide.Buy)
                        {
                            symbol = "ETH/USDT",
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
                    else {
                        var firstOrderOpenInfo = await ftxClient.TradeApi.CommonSpotClient.GetOpenOrdersAsync("ETH/USDT");
                        var ordersUSDTInfo = await ftxClient.TradeApi.CommonSpotClient.GetOpenOrdersAsync("ETH/USDT");
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
                                var secondOrderOpenInfo = await ftxClient.TradeApi.CommonSpotClient.GetOpenOrdersAsync("ETH/USDT");
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