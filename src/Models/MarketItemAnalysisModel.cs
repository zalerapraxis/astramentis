﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Astramentis.Models
{
    public class MarketItemAnalysisModel
    {
        public string Name { get; set; }
        public int ID { get; set; }
        public bool IsHQ { get; set; }
        public decimal Differential { get; set; }
        public int AvgSalePrice { get; set; }
        public int AvgMarketPrice { get; set; }
        public int NumRecentSales { get; set; }
        public bool ItemHasListings { get; set; }
        public bool ItemHasHistory { get; set; }
        public int LowestPrice { get; set; }
        public string LowestPriceServer { get; set; }
    }
}
