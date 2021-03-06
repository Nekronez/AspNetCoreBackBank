﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BackBank.Models.Incoming
{
    public class AuthModel
    {
        [MinLength(11, ErrorMessage = "Phone length should be 11.")]
        [MaxLength(11, ErrorMessage = "Phone length should be 11.")]
        public string Phone { get; set; }

        [Required]
        [MinLength(4, ErrorMessage = "Passcode length should be 4.")]
        [MaxLength(4, ErrorMessage = "Passcode length should be 4.")]
        [RegularExpression(@"^[0-9-]*$", ErrorMessage = "The passcode should consist only of numbers.")]
        public string Passcode { get; set; }
    }
}
