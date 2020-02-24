using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Astramentis.Datasets
{
    public class MarketOrderGearsetDataset
    {
        public static List<string> fendingSet = new List<string>()
        {
            "Neo-Ishgardian Sword",
            "Neo-Ishgardian Shield",
            "Neo-Ishgardian Greatsword",
            "Neo-Ishgardian Labrys",
            "Neo-Ishgardian Manatrigger",
            "Neo-Ishgardian Cap of Fending",
            "Neo-Ishgardian Top of Fending",
            "Neo-Ishgardian Gauntlets of Fending",
            "Neo-Ishgardian Plate Belt of Fending",
            "Neo-Ishgardian Bottoms of Fending",
            "Neo-Ishgardian Sollerets of Fending",
            "Neo-Ishgardian Earrings of Fending",
            "Neo-Ishgardian Choker of Fending",
            "Neo-Ishgardian Wristbands of Fending",
            "Neo-Ishgardian Ring of Fending"
        };

        public static List<string> healingSet = new List<string>()
        {
            "Neo-Ishgardian Cane",
            "Neo-Ishgardian Codex",
            "Neo-Ishgardian Planisphere",
            "Neo-Ishgardian Cap of Healing",
            "Neo-Ishgardian Top of Healing",
            "Neo-Ishgardian Gloves of Healing",
            "Neo-Ishgardian Leather Belt of Healing",
            "Neo-Ishgardian Bottoms of Healing",
            "Neo-Ishgardian Boots of Healing",
            "Neo-Ishgardian Earrings of Healing",
            "Neo-Ishgardian Choker of Healing",
            "Neo-Ishgardian Wristbands of Healing",
            "Neo-Ishgardian Ring of Healing"
        };

        public static List<string> strikingSet = new List<string>()
        {
            "Neo-Ishgardian Claws",
            "Neo-Ishgardian Blade",
            "Neo-Ishgardian Hat of Striking",
            "Neo-Ishgardian Top of Striking",
            "Neo-Ishgardian Gloves of Striking",
            "Neo-Ishgardian Leather Belt of Striking",
            "Neo-Ishgardian Bottoms of Striking",
            "Neo-Ishgardian Boots of Striking",
            "Neo-Ishgardian Wristbands of Slaying",
            "Neo-Ishgardian Earrings of Slaying",
            "Neo-Ishgardian Choker of Slaying",
            "Neo-Ishgardian Ring of Slaying"
        };

        public static List<string> maimingSet = new List<string>()
        {
            "Neo-Ishgardian Trident",
            "Neo-Ishgardian Cap of Maiming",
            "Neo-Ishgardian Top of Maiming",
            "Neo-Ishgardian Gloves of Maiming",
            "Neo-Ishgardian Plate Belt of Maiming",
            "Neo-Ishgardian Bottoms of Maiming",
            "Neo-Ishgardian Boots of Maiming",
            "Neo-Ishgardian Earrings of Slaying",
            "Neo-Ishgardian Choker of Slaying",
            "Neo-Ishgardian Wristbands of Slaying",
            "Neo-Ishgardian Ring of Slaying",
        };

        public static List<string> scoutingSet = new List<string>()
        {
            "Neo-Ishgardian Daggers",
            "Neo-Ishgardian Hat of Scouting",
            "Neo-Ishgardian Top of Scouting",
            "Neo-Ishgardian Gloves of Scouting",
            "Neo-Ishgardian Leather Belt of Scouting",
            "Neo-Ishgardian Bottoms of Scouting",
            "Neo-Ishgardian Sollerets of Scouting",
            "Neo-Ishgardian Earrings of Aiming",
            "Neo-Ishgardian Choker of Aiming",
            "Neo-Ishgardian Wristbands of Aiming",
            "Neo-Ishgardian Ring of Aiming",
        };

        public static List<string> aimingSet = new List<string>()
        {
            "Neo-Ishgardian Chakrams",
            "Neo-Ishgardian Longbow",
            "Neo-Ishgardian Revolver",
            "Neo-Ishgardian Cap of Aiming",
            "Neo-Ishgardian Top of Aiming",
            "Neo-Ishgardian Gloves of Aiming",
            "Neo-Ishgardian Leather Belt of Aiming",
            "Neo-Ishgardian Bottoms of Aiming",
            "Neo-Ishgardian Boots of Aiming",
            "Neo-Ishgardian Earrings of Aiming",
            "Neo-Ishgardian Choker of Aiming",
            "Neo-Ishgardian Wristbands of Aiming",
            "Neo-Ishgardian Ring of Aiming",
        };

        public static List<string> castingSet = new List<string>()
        {
            "Neo-Ishgardian Rod",
            "Neo-Ishgardian Foil",
            "Neo-Ishgardian Grimoire",
            "Neo-Ishgardian Hat of Casting",
            "Neo-Ishgardian Top of Casting",
            "Neo-Ishgardian Gloves of Casting",
            "Neo-Ishgardian Leather Belt of Casting",
            "Neo-Ishgardian Bottoms of Casting",
            "Neo-Ishgardian Boots of Casting",
            "Neo-Ishgardian Earrings of Casting",
            "Neo-Ishgardian Choker of Casting",
            "Neo-Ishgardian Wristbands of Casting",
            "Neo-Ishgardian Ring of Casting",
        };


        public static List<string> craftingSet = new List<string>()
        {
            "Facet Hat Of Crafting",
            "Facet Coat Of Crafting",
            "Facet Gloves Of Crafting",
            "Facet Trousers Of Crafting",
            "Facet Boots Of Crafting",
            "Facet Saw",
            "Facet Claw Hammer",
            "Facet Cross-Pein Hammer",
            "Facet File",
            "Facet Raising Hammer",
            "Facet Pliers",
            "Facet Mallet",
            "Facet Grinding Wheel",
            "Facet Round Knife",
            "Facet Awl",
            "Facet Needle",
            "Facet Spinning Wheel",
            "Facet Alembic",
            "Facet Mortar",
            "Facet Frypan",
            "Dwarven Mythril Ear Cuffs",
            "Dwarven Mythril Choker",
            "Dwarven Mythril Bracelets",
            "Dwarven Mythril Ring"

        };


        public static List<string> gatheringSet = new List<string>()
        {
            "Facet Pickaxe",
            "Facet Sledgehammer",
            "Facet Scythe",
            "Facet Hatchet",
            "Facet Fishing Rod",
            "Facet Cap Of Gathering",
            "Facet Coat Of Gathering",
            "Facet Fingerless Gloves Of Gathering",
            "Facet Bottoms Of Gathering",
            "Facet Boots Of Gathering",
            "Swallowskin Survival Belt",
            "Lignum Vitae Earrings",
            "Lignum Vitae Necklace",
            "Lignum Vitae Wristbands",
            "Lignum Vitae Ring",
        };

        public static Dictionary<string, List<string>> Gearsets = new Dictionary<string, List<string>>()
        {
            { "fending", fendingSet },
            { "healing", healingSet },
            { "striking", strikingSet },
            { "maiming", maimingSet },
            { "scouting", scoutingSet },
            { "aiming", aimingSet },
            { "casting", castingSet },
            { "crafting", craftingSet },
            { "gathering", gatheringSet },
        };
    }
}
