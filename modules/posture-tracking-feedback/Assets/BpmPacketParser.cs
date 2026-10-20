using System;
using System.Globalization;

/// <summary>Parses BPM or the ESP32 CSV packet BPM,temperature,humidity.</summary>
public static class BpmPacketParser
{
    public static bool TryParse(string payload, out float bpm)
    {
        bpm = 0f;
        if (String.IsNullOrWhiteSpace(payload)) return false;
        string[] fields = payload.Trim().Split(',');
        if (fields.Length != 1 && fields.Length != 3) return false;
        string first = fields[0].Trim();
        if (fields.Length == 1)
        {
            if (first.StartsWith("BPM:", StringComparison.OrdinalIgnoreCase))
                first = first.Substring(4).Trim();
            else if (first.EndsWith(" BPM", StringComparison.OrdinalIgnoreCase))
                first = first.Substring(0, first.Length - 4).Trim();
        }
        float parsed;
        if (!Single.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ||
            Single.IsNaN(parsed) || Single.IsInfinity(parsed) || parsed < 0f || parsed > 260f)
            return false;
        // Reject malformed CSV fields. NaN environmental values are valid sensor failures.
        for (int i = 1; i < fields.Length; i++)
        {
            float environment;
            if (!Single.TryParse(fields[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out environment))
                return false;
        }
        bpm = parsed;
        return true;
    }
}
