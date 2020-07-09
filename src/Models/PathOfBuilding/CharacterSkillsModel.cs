﻿using System;
using System.Collections.Generic;
using System.Text;

// Code from https://github.com/Kyle-Undefined/PoE-Bot

namespace Astramentis.Models.PathOfBuilding
{
    public class CharacterSkills
    {
        public CharacterSkills(IReadOnlyList<SkillGroup> skillGroups, int mainSkillIndex)
        {
            MainSkillIndex = mainSkillIndex;
            SkillGroups = skillGroups;
        }

        public SkillGroup MainSkillGroup => SkillGroups[MainSkillIndex];
        public int MainSkillIndex { get; }
        public IReadOnlyList<SkillGroup> SkillGroups { get; }
    }
}