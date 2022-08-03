using Binance.Net.Enums;

namespace BotH.Models
{
    public class NewOrder
    {
        public NewOrder(OrderSide orderSide) {
            symbol = string.Empty;
            this.orderSide = orderSide;
            timeInForce = TimeInForce.GoodTillCanceled;
            spotOrderType = SpotOrderType.Limit;
        }

        public NewOrder() {
            symbol = string.Empty;
            timeInForce = TimeInForce.GoodTillCanceled;
        }

        public string symbol { get; set; }
        public decimal quantity { get; set; }
        public decimal price { get; set; }
        public OrderSide orderSide { get; set; }
        public SpotOrderType spotOrderType { get; set; }
        public TimeInForce timeInForce { get; set; }
    }
}
