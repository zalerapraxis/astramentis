﻿using System;
using System.Collections.Generic;
using System.Text;

// Code from https://github.com/Kyle-Undefined/PoE-Bot

namespace Astramentis.Models.PathOfBuilding
{
    public class Gem
    {
        public Gem(string skillId, string name, int level, int quality, bool enabled)
        {
            Enabled = enabled;
            Level = level;
            Name = name;
            Quality = quality;
            SkillId = skillId;
        }

        public bool Enabled { get; }
        public int Level { get; }
        public string Name { get; }
        public int Quality { get; }
        public string SkillId { get; }
    }
}
