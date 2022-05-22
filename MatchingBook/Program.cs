using MatchingBook.Models;
using MatchingBook.Services;
using System;

namespace MatchingBook
{
    internal class Program
    {
        static void Main(string[] args)
        {
            OrderBook book = new OrderBook();
            OrderService os = new OrderService();
            OrderProcessing op = new OrderProcessing(os);
            while (true)
            {
                var input = Console.ReadLine();
                if (input == "END")
                {
                    op.ProcessOrder(input, book);
                    break;
                }

                var totalCost = op.ProcessOrder(input, book).TotalCost;
                if (totalCost.HasValue)
                {
                    Console.WriteLine(totalCost);
                }
            }
        }
    }
}
