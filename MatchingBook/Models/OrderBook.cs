using System;
using System.Collections.Generic;
using System.Text;

namespace MatchingBook.Models
{
    /// <summary>
    /// Represent an order book that stored the pending selling and buying orders
    /// </summary>
    public class OrderBook
    {
        /// <summary>
        /// Pending buying orders
        /// </summary>
        /// <value></value>
        public List<Order> BuyOrders { get; set; } = new List<Order>();

        /// <summary>
        /// Pending selling orders
        /// </summary>
        /// <value></value>
        public List<Order> SellOrders { get; set; } = new List<Order>();
    }
}
