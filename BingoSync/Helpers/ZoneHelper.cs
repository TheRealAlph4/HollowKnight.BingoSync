using GlobalEnums;

namespace BingoSync.Helpers
{
    internal static class ZoneHelper
    {
        public static MapZone GreaterZone(MapZone zone)
        {
            switch (zone)
            {
                case MapZone.CITY:
                case MapZone.LURIENS_TOWER:
                case MapZone.SOUL_SOCIETY:
                case MapZone.KINGS_STATION:
                    return MapZone.CITY;
                case MapZone.CROSSROADS:
                case MapZone.SHAMAN_TEMPLE:
                    return MapZone.CROSSROADS;
                case MapZone.BEASTS_DEN:
                case MapZone.DEEPNEST:
                    return MapZone.DEEPNEST;
                case MapZone.FOG_CANYON:
                case MapZone.MONOMON_ARCHIVE:
                    return MapZone.FOG_CANYON;
                case MapZone.WASTES:
                case MapZone.QUEENS_STATION:
                    return MapZone.WASTES;
                case MapZone.OUTSKIRTS:
                case MapZone.HIVE:
                case MapZone.COLOSSEUM:
                    return MapZone.OUTSKIRTS;
                case MapZone.CLIFFS:
                case MapZone.KINGS_PASS:
                    return MapZone.CLIFFS;
                case MapZone.TOWN:
                    return MapZone.TOWN;
                case MapZone.WATERWAYS:
                case MapZone.GODSEEKER_WASTE:
                    return MapZone.WATERWAYS;
                case MapZone.PEAK:
                case MapZone.MINES:
                    return MapZone.PEAK;
                default:
                    return zone;
            }
        }
    }
}
