using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campaign
{
    public class Order
    {
        public string productCode { get; set; }
        public int quantity { get; set; }
        public double price { get; set; }
        public DateTime createDate { get; set; }
        public string CampaignName { get; set; }
    }
}
