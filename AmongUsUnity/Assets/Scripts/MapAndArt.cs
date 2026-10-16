using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Crewmate colours: body and shade pairs as in the original game.</summary>
    public static class Palette
    {
        public static readonly string[] Names =
            { "Red", "Blue", "Green", "Pink", "Orange", "Yellow", "Black", "White", "Purple", "Brown", "Cyan", "Lime" };

        static Color C(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

        public static readonly Color[] Colors =
        {
            C(0xC51111), C(0x132ED2), C(0x117F2D), C(0xED54BA), C(0xEF7D0D), C(0xF6F657),
            C(0x3F474E), C(0xD6E0F0), C(0x6B31BC), C(0x71491E), C(0x38FEDC), C(0x50EF39),
        };

        public static readonly Color[] Shades =
        {
            C(0x7A0838), C(0x09158E), C(0x0A4D2E), C(0xAB2BAD), C(0xB33E15), C(0xC38822),
            C(0x1E1F29), C(0x8394BF), C(0x3B177C), C(0x5E2615), C(0x24A8BE), C(0x15A742),
        };

        public static readonly Color Visor = C(0x95CADC);

        public static Color Body(int i) => Colors[Mathf.Abs(i) % Colors.Length];
        public static Color Shade(int i) => Shades[Mathf.Abs(i) % Shades.Length];
    }
}
