using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace mvc_with_avr.Models
{
    public class CustomerPageModel
    {
        [DisplayFormat(DataFormatString = "{0:00000}", ApplyFormatInEditMode = true)]
        public System.Decimal CMCustNo { get; set; }
        public string CMName { get; set; }
    }
}