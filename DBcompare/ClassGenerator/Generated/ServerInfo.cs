using Newtonsoft.Json;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Renci.SshNet;
namespace DBcompare.Common

public class ServerInfo
{
    public static ServerInfo Instance { get; private set; }
    public string SshHost { get; set; }
    public string SshUserName { get; set; }
    public string MySqlUserName { get; set; }
    public string MySqlPassword { get; set; }
    public string SshPort { get; set; }
    public string MySqlPort { get; set; }
    public string Team3s { get; set; }
    public string Team3c { get; set; }
    public string Team3q { get; set; }
    public string Team32s { get; set; }
    public string Team32c { get; set; }
    public string Team32q { get; set; }
    public string Hc2s { get; set; }
    public string Hc2c { get; set; }
    public string Hc2q { get; set; }
    public string HomerunClashInspection { get; set; }
    public string SblInspection { get; set; }
    public string Hc2Inspection { get; set; }
    public string HomerunClashLiveCommon { get; set; }
    public string HomerunClashLiveGame1 { get; set; }
    public string HomerunClashLiveGame2 { get; set; }
    public string HomerunClashLiveGame3 { get; set; }
    public string HomerunClashLiveClan { get; set; }
    public string HomerunClashLiveRank { get; set; }
    public string HomerunClashLiveLog { get; set; }
    public string HomerunClashLivePush { get; set; }
    public string SblLiveCommon { get; set; }
    public string SblLiveGame1 { get; set; }
    public string SblLiveGame2 { get; set; }
    public string SblLiveClub { get; set; }
    public string SblLiveRank { get; set; }
    public string SblLiveLog { get; set; }
    public string SblLivePush { get; set; }
    public string Hc2LiveCommon { get; set; }
    public string Hc2LiveGame1 { get; set; }
    public string Hc2LiveClub { get; set; }
    public string Hc2LiveRank { get; set; }
    public string Hc2LiveLog { get; set; }
    public string Hc2LivePush { get; set; }
    public string HomerunClashDatabases { get; set; }
    public string SblDatabases { get; set; }
    public string HC2Databases { get; set; }

    [Refreshable]
    public static void Refresh()
    {
        var configurationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServerInfo.config");
        ServerInfo ServerInfo = JsonConvert.DeserializeObject<ServerInfo>(File.ReadAllText(configurationPath));

        Console.WriteLine($"ServerInfo Refreshed");

        Instance = ServerInfo;
    }
}