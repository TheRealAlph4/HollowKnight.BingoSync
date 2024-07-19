using GlobalEnums;

namespace BingoSync.Helpers
{
    internal static class ZoneHelper
    {
        public static MapZone GreaterZone(MapZone zone)
        {
            return zone switch
            {
                MapZone.CITY or MapZone.LURIENS_TOWER or MapZone.SOUL_SOCIETY or MapZone.KINGS_STATION => MapZone.CITY,
                MapZone.CROSSROADS or MapZone.SHAMAN_TEMPLE => MapZone.CROSSROADS,
                MapZone.BEASTS_DEN or MapZone.DEEPNEST => MapZone.DEEPNEST,
                MapZone.FOG_CANYON or MapZone.MONOMON_ARCHIVE => MapZone.FOG_CANYON,
                MapZone.WASTES or MapZone.QUEENS_STATION => MapZone.WASTES,
                MapZone.OUTSKIRTS or MapZone.HIVE or MapZone.COLOSSEUM => MapZone.OUTSKIRTS,
                MapZone.CLIFFS or MapZone.KINGS_PASS => MapZone.CLIFFS,
                MapZone.TOWN => MapZone.TOWN,
                MapZone.WATERWAYS or MapZone.GODSEEKER_WASTE => MapZone.WATERWAYS,
                MapZone.PEAK or MapZone.MINES => MapZone.PEAK,
                _ => zone,
            };
        }
    }
}
