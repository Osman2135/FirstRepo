using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campaign
{
    public class Campaign
    {
        public string name { get; set; }
        public string productCode { get; set; }
        public int duration { get; set; }
        public int priceManipulationLimit { get; set; }
        public int targetSalesCount { get; set; }
        public DateTime createDate { get; set; }
    }
}
