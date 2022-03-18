using System.Collections.Generic;

namespace mvc_with_avr.Models
{
    public class CustomerPagedModelViewModel
    {
        public List<CustomerPageModel> Customers { get; set; }
        public int NextPage { get; set; }
        public int PrevPage { get; set; }
        public bool MorePages { get; set; }
    }
}