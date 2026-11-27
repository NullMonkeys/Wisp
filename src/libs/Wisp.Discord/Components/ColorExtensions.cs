using NetCord;

namespace Wisp.Discord.Components;

public static class ColorExtensions
{
    extension(Color)
    {
        public static Color Error => new(237, 66, 69);
        public static Color Warning => new(254, 231, 92);
        public static Color Success => new(87, 242, 135);
    }
}