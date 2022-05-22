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
                // _orderService.ExecuteEnd(book);
                return new ProcessOrderResponse();
            }

            if (parsedResult.Update)
            {
                _orderService.ExecuteUpdateOrder(parsedResult.Order, book);
                // _orderService.ExecuteEnd(book);
                return new ProcessOrderResponse();
            }

            int totalCost = _orderService.ExecuteGenericOrders(parsedResult.Order, book).TotalPrice;
            // _orderService.ExecuteEnd(book);

            return new ProcessOrderResponse { TotalCost = totalCost };
        }
    }
}
