using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BackBank.Models.Incoming
{
    public class RegistrModel
    {
        [Required]
        [MinLength(11, ErrorMessage = "Phone length should be 11.")]
        [MaxLength(11, ErrorMessage = "Phone length should be 11.")]
        public string Phone { get; set; }

        [Required]
        [MinLength(13, ErrorMessage = "PAN length should be 13.")]
        [MaxLength(19, ErrorMessage = "PAN length should be 19.")]
        public string PAN { get; set; }
    }
}
