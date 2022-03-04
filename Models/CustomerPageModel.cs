using System.ComponentModel.DataAnnotations;

namespace mvc_with_avr.Models
{
    public class CustomerPageModel
    {
        [DisplayFormat(DataFormatString = "{0:00000}", ApplyFormatInEditMode = true)]
        public System.Decimal CMCustNo { get; set; }
        public string CMName { get; set; }
    }
}