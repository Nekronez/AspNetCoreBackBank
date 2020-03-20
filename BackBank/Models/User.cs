using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackBank.Models
{
    public class User 
    {
        public int Id { get; set; }
        public string Phone { get; set; }
        public string Passcode { get; set; }
    }
}
