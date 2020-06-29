using System;
using System.Collections.Generic;
using System.Text;

// Code from https://github.com/Kyle-Undefined/PoE-Bot

namespace Astramentis.Models.PathOfBuilding
{
    public class ItemSlots
    {
        public ItemSlots(string name, int itemID)
        {
            ItemID = itemID;
            Name = name;
        }

        public int ItemID { get; private set; }
        public string Name { get; private set; }
    }
}
