
using FTX.Net.Objects;

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


        [HttpPost]
        public async Task<ResponseMessage> Loop(LoopInput order)
        {
            try
            {               
                ResponseMessage response = new ResponseMessage();
                DateTime date = new DateTime(now.Year, now.Month, now.Day, Convert.ToInt16(configuration.LoopStart_Hour), Convert.ToInt16(configuration.LoopStart_Minute), 0);
                DateTime refDate = date.AddMinutes(Convert.ToInt16(configuration.Loop_Duration));
                var activeLoop = (DateTime.Now >= date && DateTime.Now <= refDate);

                if (activeLoop == false) {
                    response.Message = "Outside Loop Time Range.";

                    return response;
                }

                var ftxClient = new FTXClient(new FTXClientOptions()
                {
                    ApiCredentials = new ApiCredentials(configuration.Exchange_ApiData.FirstOrDefault()!.ApiKeySub!, configuration.Exchange_ApiData.FirstOrDefault()!.SecretSub!),
                    LogLevel = LogLevel.Trace,
                    SubaccountName = "BTC"
                });
                var accountInfo = await ftxClient.TradeApi.Account.GetAccountInfoAsync("BTC");

                NewOrder firstOrder = new NewOrder();
                NewOrder secondOrder = new NewOrder();
                
                switch (order.type)
                {
                    case "BUY":
                        firstOrder = new NewOrder(OrderSide.Buy)
                        {
                            symbol = "BTC/USDT",
                            quantity = order.quantity,
                            price = order.floor,
                        };
                        break;
                    case "SELL":
                        firstOrder = new NewOrder(OrderSide.Sell)
                        {
                            symbol = "BTC/USDT",
                            quantity = order.quantity,
                            price = order.top,
                        };
                        break;
                }

                var orderResponse = await ftxClient.TradeApi.CommonSpotClient.PlaceOrderAsync(
                       firstOrder.symbol,
                       (CommonOrderSide)firstOrder.orderSide,
                       (CommonOrderType)firstOrder.spotOrderType,
                       firstOrder.quantity,
                       (decimal)firstOrder.price);

                if (!orderResponse.Success) response.Message += orderResponse.Error!.ToString() + ". ";

                var orderId = orderResponse.Data.Id;

                var pending = true;

                while (activeLoop)
                {
                    var ftxOrderInfo = await ftxClient.TradeApi.CommonSpotClient.GetOrderAsync(orderId);
                    var ftxOrder = ftxOrderInfo.Data;

                    if (ftxOrder.Status == CommonOrderStatus.Filled)
                    {
                        switch (order.type)
                        {
                            case "BUY":
                                secondOrder = new NewOrder(OrderSide.Sell)
                                {
                                    symbol = "BTC/USDT",
                                    quantity = order.quantity,
                                    price = order.top,
                                };
                                break;
                            case "SELL":
                                secondOrder = new NewOrder(OrderSide.Buy)
                                {
                                    symbol = "BTC/USDT",
                                    quantity = order.quantity,
                                    price = order.top,
                                };
                                break;
                        }

                        var secondOrderResponse = await ftxClient.TradeApi.CommonSpotClient.PlaceOrderAsync(
                        secondOrder.symbol,
                        (CommonOrderSide)secondOrder.orderSide,
                        (CommonOrderType)secondOrder.spotOrderType,
                        secondOrder.quantity,
                        (decimal)secondOrder.price);

                        if (!secondOrderResponse.Success) response.Message += secondOrderResponse.Error!.ToString() + ". ";

                        while (pending) {
                            var secondOrderId = secondOrderResponse.Data.Id;
                            var ftxSecondOrderInfo = await ftxClient.TradeApi.CommonSpotClient.GetOrderAsync(secondOrderId);
                            var ftxSecondOrder = ftxSecondOrderInfo.Data;
                            if (ftxSecondOrder.Status == CommonOrderStatus.Filled) {
                                pending = false;
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