using System.Text.Json;

namespace DBcompare.Common;

public class DumpInfo
{
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8618
#pragma warning disable CS8600
#pragma warning disable CS8604
#pragma warning disable CS8625
    
    public static DumpInfo Instance { get; private set; }
    public string DumpFileSavePath { get; set; }
    public string LogSaveServerAddress { get; set; }
    
    [Refreshable]
    public static async Task RefreshAsync()
    {
        var configurationPath = "DumpInfo.config";
        string jsonString = await File.ReadAllTextAsync(configurationPath);
        var dumpInfo = JsonSerializer.Deserialize<DumpInfo>(jsonString);

        Console.WriteLine($"DumpInfo Refreshed");

        Instance = dumpInfo;
    }
}