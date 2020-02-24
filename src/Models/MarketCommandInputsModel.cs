using System;
using System.Collections.Generic;
using System.Text;

namespace Astramentis.Models
{
    public class MarketCommandInputsModel
    {
        public string ItemName { get; set; }
        public int ItemId { get; set; }
        public string ItemIconUrl { get; set; }
        public List<string> WorldsToSearch { get; set; }
    }
}
