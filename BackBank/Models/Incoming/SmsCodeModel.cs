using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BackBank.Models.Incoming
{
    public class SmsCodeModel
    {
        [Required]
        [MinLength(6, ErrorMessage = "Code length should be 6.")]
        [MaxLength(6, ErrorMessage = "Code length should be 6.")]
        [RegularExpression(@"^[0-9-]*$", ErrorMessage = "The code should consist only of numbers.")]
        public string Code { get; set; }
    }
}
