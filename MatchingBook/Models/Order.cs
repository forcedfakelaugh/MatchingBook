using System;
using System.Collections.Generic;
using System.Text;

namespace MatchingBook.Models
{
    /// <summary>
    /// Represent an order to execute
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Identifier of this order
        /// </summary>
        /// <value></value>
        public string Id { get; set; }

        /// <summary>
        /// Buy or Sell
        /// </summary>
        /// <value></value>
        public Side Side { get; set; }

        /// <summary>
        /// Price of a single stock
        /// </summary>
        /// <value></value>
        public int Price { get; set; }

        /// <summary>
        /// Number of stocks 
        /// </summary>
        /// <value></value>
        public int Quantity { get; set; }

        /// <summary>
        /// Type of this order
        /// </summary>
        /// <value></value>
        public OrderType OrderType { get; set; }

        /// <summary>
        /// Create date of this order
        /// </summary>
        /// <value></value>
        public DateTime CreateDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Type of order
    /// </summary>
    public enum OrderType
    {
        /// <summary>
        /// Limit order.
        /// </summary>
        LO,

        /// <summary>
        /// Market order.
        /// </summary>
        MO
    }

    /// <summary>
    /// Side of order
    /// </summary>
    public enum Side
    {
        /// <summary>
        /// An order to buy stock
        /// </summary>
        Buy,

        /// <summary>
        /// An order to sell stock
        /// </summary>
        Sell
    }
}
