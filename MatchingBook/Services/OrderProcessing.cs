using MatchingBook.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchingBook.Services
{
    public class ProcessOrderResponse 
    {
        public int? TotalCost { get; set; }
        public bool StopProcessing { get; set; }
    }

    public class OrderProcessing 
    {
        private readonly OrderService _orderService;

        public OrderProcessing(OrderService orderService)
        {
            _orderService = orderService;
        }

        public ProcessOrderResponse ProcessOrder(string input, OrderBook book)
        {
            var parsedResult = _orderService.ProcessInputOrder(input);

            if (parsedResult.End) {
                _orderService.ExecuteEnd(book);
                return new ProcessOrderResponse { StopProcessing = true };
            }

            if (parsedResult.Cancel)
            {
                _orderService.ExecuteCancelOrder(parsedResult.Order, book);
                return new ProcessOrderResponse();
            }

            int? totalCost = null;
            switch (parsedResult.Order.OrderType)
            {
                case OrderType.LO: totalCost = _orderService.ExecuteLimitOrder(parsedResult.Order, book).TotalPrice; break;
                case OrderType.MO: totalCost = _orderService.ExecuteMarketOrder(parsedResult.Order, book).TotalPrice; break;
                default: break;
            }

            return new ProcessOrderResponse { TotalCost = totalCost };
        }
    }
}
