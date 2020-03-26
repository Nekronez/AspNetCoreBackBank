using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackBank.Models
{
    public class HistoryOperations
    {
        public int Id { get; set; }
        public int CardId { get; set; }
        public string OperationName { get; set; }
        public string OperationStatus { get; set; }
        public double Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
