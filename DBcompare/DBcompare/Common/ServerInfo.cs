using Renci.SshNet;

namespace DBcompare.Common;

using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

public class ServerInfo
{
    public static ServerInfo Instance { get; private set; }
    
    public static PrivateKeyFile[] PrivateKeyFileArray { get; private set; }
    public string SshHost { get; set; }
    public string SshUserName { get; set; }
    public string MySqlUserName { get; set; }
    public string MySqlPassword { get; set; }
    public string SshPort { get; set; }
    public string MySqlPort { get; set; }
    public Dictionary<string, Dictionary<string, string>> ConnectionStrings { get; set; }
    public Dictionary<string, List<string>> Databases { get; set; }

    [Refreshable]
    public static async Task RefreshAsync()
    {
        var privateKeyFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "id_rsa");
        var privateKeyFile = new PrivateKeyFile(privateKeyFilePath);
        PrivateKeyFileArray = new PrivateKeyFile[] {privateKeyFile};
        
        // var configurationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServerInfo.config");
        var configurationPath = "ServerInfo.config";
        string jsonString = await File.ReadAllTextAsync(configurationPath);
        var serverInfo = JsonSerializer.Deserialize<ServerInfo>(jsonString);

        Console.WriteLine($"ServerInfo Refreshed");

        Instance = serverInfo;
    }

    public Dictionary<string, string> FindConnectionStrings(string projectName)
    {
        return ConnectionStrings[projectName];
    }

    public string FindConnectionString(string projectName, string server)
    {
        return ConnectionStrings[projectName][server];
    }
    
    public List<string> FindDatabases(string ProjectName)
    {
        return Databases[ProjectName];
    }
}