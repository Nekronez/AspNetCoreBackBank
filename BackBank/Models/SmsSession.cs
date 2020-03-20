using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackBank.Models
{
    public class SmsSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public long CodeHotpCounter { get; set; }
        public int Attempts { get; set; }
        public bool Checked { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual User User { get; set; }
    }
}
