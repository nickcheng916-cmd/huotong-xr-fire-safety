using System;
using System.Globalization;

public static class BpmPacketParserTests
{
    private static int assertions;
    private static void Accept(string packet, float expected)
    {
        float actual;
        if (!BpmPacketParser.TryParse(packet, out actual) || Math.Abs(actual - expected) > 0.001f)
            throw new Exception("Incorrect BPM for: " + packet);
        assertions++;
    }
    private static void Reject(string packet)
    {
        float actual;
        if (BpmPacketParser.TryParse(packet, out actual))
            throw new Exception("Accepted invalid packet: " + packet);
        assertions++;
    }
    public static void Main()
    {
        Accept("78,26.50,48.00", 78);
        Accept(" 92,31.8,62.4\r\n", 92);
        Accept("72", 72);
        Accept("72.5", 72.5f);
        Accept("BPM: 89", 89);
        Accept("88 BPM", 88);
        Accept("0,0,0", 0);
        Accept("260,20,40", 260);
        Accept("80,NaN,NaN", 80);
        Reject(null); Reject(""); Reject("  ");
        Reject("NaN"); Reject("Infinity"); Reject("-Infinity");
        Reject("-1,23,50"); Reject("261,23,50");
        Reject("bad78temp26"); Reject("{\"bpm\":78,\"temp\":26}");
        Reject("78,26"); Reject("78,26,50,1"); Reject("78,,50");
        Reject("78,broken,50"); Reject("NaN,20,40");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Accept("78.5,26.50,48.00", 78.5f);
        Reject("78,5");
        Console.WriteLine("PASS: " + assertions + " BPM parser assertions");
    }
}
