using Newtonsoft.Json;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Renci.SshNet;
namespace DBcompare.Common

public class DumpInfo
{
    public static DumpInfo Instance { get; private set; }
    public string DumpFileSavePath { get; set; }
    public string DumpLogSaveServerAddress { get; set; }

    [Refreshable]
    public static void Refresh()
    {
        var configurationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DumpInfo.config");
        DumpInfo dumpInfo = JsonConvert.DeserializeObject<DumpInfo>(File.ReadAllText(configurationPath));

        Console.WriteLine($"DumpInfo Refreshed");

        Instance = dumpInfo;
    }
}