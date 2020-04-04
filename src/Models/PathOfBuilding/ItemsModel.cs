using System;
using System.Collections.Generic;
using System.Text;

namespace Astramentis.Models.PathOfBuilding
{
    public class Items
    {
        public Items(int iD, string content)
        {
            Content = content;
            ID = iD;
        }

        public string Content { get; }
        public int ID { get; }
    }
}
