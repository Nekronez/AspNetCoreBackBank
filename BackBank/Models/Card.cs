using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackBank.Models
{
    public class Card
    {
        public int Id { get; set; }
        public string NumberAccount { get; set; }
        public string PAN { get; set; }
        public string CVV { get; set; }
        public string Name { get; set; }
        public double Balance { get; set; }
        public DateTime ActivationDate { get; set; }
        public DateTime ExpireDate { get; set; }
        public int UserId { get; set; }
    }
}
