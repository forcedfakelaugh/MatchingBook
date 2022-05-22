﻿using MatchingBook.Models;
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
        /// /// <value></value>
        public bool Cancel { get; set; } = false;

        /// <summary>
        /// If true, this order processing should be stop
        /// </summary>
        /// <value></value>
        public bool End { get; set; } = false;

        /// <summary>
        /// If true, this order is being updated
        /// </summary>
        /// <value></value>
        public bool Update { get; set; } = false;
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
                return new ProcessInputOrderResponse { Cancel = true, Order = new Order { Id = splitted[1] } };
            }

            if (splitted[0].Equals("CRP"))
            {
                return new ProcessInputOrderResponse
                {
                    Update = true,
                    Order = new Order
                    {
                        Id = splitted[1],
                        Quantity = int.Parse(splitted[2]),
                        Price = int.Parse(splitted[3])
                    }
                };
            }

            
            var type = (OrderType)Enum.Parse(typeof(OrderType), splitted[1]);
            var side = splitted[2] == "B" ? Side.Buy : Side.Sell;

            int.TryParse(splitted[4], out int quantity);

            int price = 0;
            if (splitted.Length > 5)
            {
                int.TryParse(splitted[5], out price);
            }

            int displaySize = 0;
            if (splitted.Length > 6)
            {
                int.TryParse(splitted[6], out displaySize);
            }

            var order = new Order()
            {
                OrderType = type,
                Side = side,
                Id = splitted[3],
                Quantity = quantity,
                Price = price,
                DisplaySize = displaySize,
                RemaingingSize = Math.Min(displaySize, quantity),
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
            Console.WriteLine($"B: {displayString}");

            // for sell
            orderByPriority = book.SellOrders.OrderBy(it => it.Price).ThenBy(it => it.CreateDate);
            displayString = string.Join(" ", orderByPriority.Select(it => TransformOrderToprint(it)));
            Console.WriteLine($"S: {displayString}");
        }

        private string TransformOrderToprint(Order order)
        {
            string quantity = order.OrderType == OrderType.ICE ?
                $"{order.RemaingingSize}({order.Quantity})" : order.Quantity.ToString();
            return $"{quantity}@{order.Price}#{order.Id}";
        }

        public ExecuteOrderResponse ExecuteUpdateOrder(Order order, OrderBook book)
        {
            var orderToUpdate = book.SellOrders.FirstOrDefault(it => order.Id == it.Id);
            orderToUpdate = orderToUpdate ?? book.BuyOrders.FirstOrDefault(it => order.Id == it.Id);

            if (orderToUpdate == null || orderToUpdate.OrderType == OrderType.ICE) return new ExecuteOrderResponse();

            var needToChangePriority = order.Quantity >= orderToUpdate.Quantity ||
                                       order.Price != orderToUpdate.Price;
            orderToUpdate.Quantity = order.Quantity;
            orderToUpdate.Price = order.Price;
            if (needToChangePriority)
            {
                orderToUpdate.CreateDate = DateTime.Now;
            }
            return new ExecuteOrderResponse();
        }

        public ExecuteOrderResponse ExecuteGenericOrders(Order order, OrderBook book)
        {
            var totalPrice = 0;
            var isBuy = order.Side == Side.Buy;
            var pendingOrders = isBuy ? book.SellOrders : book.BuyOrders;

            var considerPrice = order.OrderType != OrderType.MO;
            var pushToBookAfterProcess = order.OrderType == OrderType.LO ||
                                         order.OrderType == OrderType.ICE;
            var commitOnlyWhenFinish = order.OrderType == OrderType.FOK;

            if (commitOnlyWhenFinish) 
            {
                // deep clone
                pendingOrders = pendingOrders.Select(it => it.Clone()).ToList();
            }

            while (order.Quantity > 0)
            {
                var limitedOrders = pendingOrders.AsEnumerable();
                if (considerPrice) limitedOrders = isBuy ? FilterByBuy(pendingOrders, order.Price) : FilterBySell(pendingOrders, order.Price);

                var priorityLimitedOrders = isBuy ? OrderBySellPriority(limitedOrders) : OrderByBuyPriority(limitedOrders);
                var matched = priorityLimitedOrders.FirstOrDefault();

                if (matched == null) break; // No matched found

                var matchedQuantity = GetExecutingQuantity(matched);

                var quantity = Math.Min(order.Quantity, matchedQuantity);
                order.Quantity -= quantity;
                matched.Quantity -= quantity;
                totalPrice += quantity * matched.Price;

                if (matched.OrderType == OrderType.ICE)
                {
                    matched.RemaingingSize -= quantity;
                    if (matched.RemaingingSize == 0)
                    {
                        matched.RemaingingSize = Math.Min(matched.DisplaySize, matched.Quantity);
                        matched.CreateDate = DateTime.Now;
                    }
                }

                if (matched.Quantity == 0)
                {
                    pendingOrders.Remove(matched);
                }
            }

            if (commitOnlyWhenFinish)
            {
                if (order.Quantity == 0)
                {
                    if (isBuy) book.SellOrders = pendingOrders;
                    else book.BuyOrders = pendingOrders;
                }
                else
                {
                    totalPrice = 0;
                }
            }
            else
            {
                if (pushToBookAfterProcess && order.Quantity > 0)
                {
                    if (isBuy) book.BuyOrders.Add(order);
                    else book.SellOrders.Add(order);
                }
            }

            return new ExecuteOrderResponse { TotalPrice = totalPrice };
        }

        private IEnumerable<Order> FilterBySell(IEnumerable<Order> orders, int price)
            => orders.Where(it => it.Price >= price);

        private IEnumerable<Order> FilterByBuy(IEnumerable<Order> orders, int price)
            => orders.Where(it => it.Price <= price);

        private IOrderedEnumerable<Order> OrderBySellPriority(IEnumerable<Order> orders)
            => orders.OrderBy(it => it.Price).ThenBy(it => it.CreateDate);

        private IOrderedEnumerable<Order> OrderByBuyPriority(IEnumerable<Order> orders)
            => orders.OrderByDescending(it => it.Price).ThenBy(it => it.CreateDate);

        private int GetExecutingQuantity(Order order)
        {
            if (order.OrderType == OrderType.ICE) return order.RemaingingSize;
            return order.Quantity;
        }

        private void UpdateCreateDateForIceBerge(Order order)
        {
            if (order.OrderType != OrderType.ICE) return;

            order.CreateDate = DateTime.Now;
        }
    }
}
