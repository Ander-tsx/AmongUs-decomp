using System;
using System.Net;

namespace AmongUsLan
{
    /// <summary>Short shareable room code = the host's LAN IPv4 address packed in base 32 (7 characters).</summary>
    public static class RoomCode
    {
        const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public static string Encode(string ip)
        {
            if (!IPAddress.TryParse(ip, out var a)) return "";
            var b = a.GetAddressBytes();
            uint v = ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
            var s = new char[7];
            for (int i = 6; i >= 0; i--) { s[i] = Alphabet[(int)(v & 31)]; v >>= 5; }
            return new string(s);
        }

        /// <summary>Accepts a room code or a plain IP; returns the IP to connect to.</summary>
        public static string ToIp(string input)
        {
            input = (input ?? "").Trim().ToUpperInvariant();
            if (IPAddress.TryParse(input, out _)) return input;
            if (input.Length != 7) return input;
            uint v = 0;
            foreach (var ch in input)
            {
                int k = Alphabet.IndexOf(ch);
                if (k < 0) return input;
                v = (v << 5) | (uint)k;
            }
            return new IPAddress(new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v }).ToString();
        }
    }
}
