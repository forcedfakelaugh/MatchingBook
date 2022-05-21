using MatchingBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatchingBook.Services
{
    /// <summary>
    /// Response after parsing order input
    /// </summary>
    public class ProcessInputOrderResponse
    {
        /// <summary>
        /// Parsed order
        /// </summary>
        /// <value></value>
        public Order Order { get; set; }

        /// <summary>
        /// If true, this is a cancel order
        /// </summary>
        /// <value></value>
        public bool Cancel { get; set; } = false;

        /// <summary>
        /// If true, this order processing should be stop
        /// </summary>
        /// <value></value>
        public bool End { get; set; } = false;
    }

    /// <summary>
    /// Response after executing an order
    /// </summary>
    public class ExecuteOrderResponse
    {
        /// <summary>
        /// Total cost of order 
        /// </summary>
        /// <value></value>
        public int TotalPrice { get; set; }
    }

    /// <summary>
    /// Service contains methods to handle business logic for an order.
    /// </summary>
    public class OrderService
    {
        /// <summary>
        /// Parse the order command as a string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public ProcessInputOrderResponse ProcessInputOrder(string input)
        {
            if (input == "END") return new ProcessInputOrderResponse { End = true };

            var splitted = input.Split(' ');
            if (splitted[0].Equals("CXL")) 
            {
                return new ProcessInputOrderResponse { Cancel = true, Order = new Order { Id = splitted[1] }};
            }

            var type = splitted[1] switch
            {
                "LO" => OrderType.LO,
                "MO" => OrderType.MO,
                _ => throw new NotImplementedException()
            };

            var side = splitted[2] switch
            {
                "B" => Side.Buy,
                "S" => Side.Sell,
                _ => throw new NotImplementedException()
            };

            int.TryParse(splitted[4], out int quantity);

            int price = 0;
            if (splitted.Length > 5)
            {
                int.TryParse(splitted[5], out price);
            }
            

            // SUB LO B Ffuj 200 13
            var order = new Order()
            {
                OrderType = type,
                Side = side,
                Id = splitted[3],
                Quantity = quantity,
                Price = price
            };

            return new ProcessInputOrderResponse
            {
                Order = order,
            };
        }

        /// <summary>
        /// Cancel an order based on Id
        /// </summary>
        /// <param name="order"></param>
        /// <param name="book"></param>
        /// <returns></returns>
        public ExecuteOrderResponse ExecuteCancelOrder(Order order, OrderBook book)
        {
            var orderToCancel = book.BuyOrders.SingleOrDefault(r => r.Id == order.Id);
            if (orderToCancel != null)
                book.BuyOrders.Remove(orderToCancel);

            orderToCancel = book.SellOrders.SingleOrDefault(r => r.Id == order.Id);
            if (orderToCancel != null)
                book.SellOrders.Remove(orderToCancel);

            return null;
        }

        public void ExecuteEnd(OrderBook book)
        {
            StringBuilder buyPrint = new StringBuilder();

            // for buy
            var orderByPriority = book.BuyOrders.OrderByDescending(it => it.Price).ThenBy(it => it.CreateDate);
            var displayString = string.Join(" ", orderByPriority.Select(it => TransformOrderToprint(it)));
            Console.WriteLine($"B {displayString}");

            // for sell
            orderByPriority = book.SellOrders.OrderBy(it => it.Price).ThenBy(it => it.CreateDate);
            displayString = string.Join(" ", orderByPriority.Select(it => TransformOrderToprint(it)));
            Console.WriteLine($"S {displayString}");

            Console.WriteLine("ENDDDDD");
        }

        private string TransformOrderToprint(Order order)
        {
            return $"{order.Quantity}@{order.Price}#{order.Id}";
        }

        /// <summary>
        /// Execute a limit order, and modify the data inside the input OrderBook
        /// </summary>
        /// <param name="order"></param>
        /// <param name="book"></param>
        public ExecuteOrderResponse ExecuteLimitOrder(Order order, OrderBook book)
        {
            if (order.Side == Side.Buy)
            {
                return ExecuteBuyLimitOrder(order, book);
            }

            return ExecuteSellLimitOrder(order, book);
        }

        private ExecuteOrderResponse ExecuteBuyLimitOrder(Order order, OrderBook book)
        {
            var totalPrice = 0;
            while (order.Quantity > 0)
            {
                var matchedSell = book.SellOrders.Where(it => order.Price >= it.Price)
                    .OrderBy(it => it.Price).ThenBy(it => it.CreateDate)
                    .FirstOrDefault();
                
                if (matchedSell == null) break; // No matched sell found

                var buyQuantity = Math.Min(order.Quantity, matchedSell.Quantity);
                totalPrice += buyQuantity * matchedSell.Price;
                order.Quantity -= buyQuantity;
                matchedSell.Quantity -= buyQuantity;

                if (matchedSell.Quantity == 0) 
                {
                    book.SellOrders.Remove(matchedSell);
                }
            }

            if (order.Quantity > 0)
            {
                book.BuyOrders.Add(order);
            }
            return new ExecuteOrderResponse { TotalPrice = totalPrice };
        }

        private ExecuteOrderResponse ExecuteSellLimitOrder(Order order, OrderBook book)
        {
            var totalPrice = 0;
            while (order.Quantity > 0)
            {
                var matchedBuyOrder = book.BuyOrders.Where(it => order.Price <= it.Price)
                    .OrderByDescending(it => it.Price).ThenBy(it => it.CreateDate)
                    .FirstOrDefault();
                
                if (matchedBuyOrder == null) break; // No matched buy found

                var sellQuantity = Math.Min(order.Quantity, matchedBuyOrder.Quantity);
                order.Quantity -= sellQuantity;
                matchedBuyOrder.Quantity -= sellQuantity;
                totalPrice += sellQuantity * matchedBuyOrder.Price;

                if (matchedBuyOrder.Quantity == 0)
                {
                    book.BuyOrders.Remove(matchedBuyOrder);
                }
            }

            if (order.Quantity > 0)
            {
                book.SellOrders.Add(order);
            }
            
            return new ExecuteOrderResponse { TotalPrice = totalPrice };
        }

        /// <summary>
        /// Execute a market order, and modify the data of the OrderBook.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="book"></param>
        /// <returns></returns>
        public ExecuteOrderResponse ExecuteMarketOrder(Order order, OrderBook book)
        {
            if (order.Side == Side.Buy)
            {
                return ExecuteBuyMarketOrder(order, book);
            }
            return ExecuteSellMarketOrder(order, book);
        }
        
        private ExecuteOrderResponse ExecuteBuyMarketOrder(Order order, OrderBook book)
        {
            var totalPrice = 0;
            while (order.Quantity > 0)
            {
                var matchedSell = book.SellOrders
                    .OrderBy(it => it.Price).ThenBy(it => it.CreateDate)
                    .FirstOrDefault();
                
                if (matchedSell == null) break; // No matched sell found

                var buyQuantity = Math.Min(order.Quantity, matchedSell.Quantity);
                totalPrice += buyQuantity * matchedSell.Price;
                order.Quantity -= buyQuantity;
                matchedSell.Quantity -= buyQuantity;

                if (matchedSell.Quantity == 0) 
                {
                    book.SellOrders.Remove(matchedSell);
                }
            }
            return new ExecuteOrderResponse { TotalPrice = totalPrice };
        }

        private ExecuteOrderResponse ExecuteSellMarketOrder(Order order, OrderBook book)
        {
            var totalPrice = 0;
            while (order.Quantity > 0)
            {
                var matchedBuyOrder = book.BuyOrders
                    .OrderByDescending(it => it.Price).ThenBy(it => it.CreateDate)
                    .FirstOrDefault();
                
                if (matchedBuyOrder == null) break; // No matched buy found

                var sellQuantity = Math.Min(order.Quantity, matchedBuyOrder.Quantity);
                order.Quantity -= sellQuantity;
                matchedBuyOrder.Quantity -= sellQuantity;
                totalPrice += sellQuantity * matchedBuyOrder.Price;

                if (matchedBuyOrder.Quantity == 0)
                {
                    book.BuyOrders.Remove(matchedBuyOrder);
                }
            }
            return new ExecuteOrderResponse { TotalPrice = totalPrice };
        }
    }
}
