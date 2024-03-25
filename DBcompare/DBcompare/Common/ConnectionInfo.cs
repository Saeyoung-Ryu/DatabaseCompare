using MySqlConnector;
using Renci.SshNet;

namespace DBcompare.Common;

public class DBConnectionInfo
{
    public string ConnectionString { get; set; }
    public SshClient? SshClient { get; set; }
    public ForwardedPortLocal? ForwardedPortLocal { get; set; }
    public MySqlConnection? MySqlConnection { get; set; }
    
    public static async Task<DBConnectionInfo?> GetConnectionInfoAsync(string server, string databaseName)
    {
        string connectionString = String.Empty;
        DBConnectionInfo connectionInfo = null;
        SshClient? sshClient = null;
        ForwardedPortLocal? forwardedPortLocal = null;
        
        try
        {
            connectionString = $"server={server};Port={ServerInfo.Instance.MySqlPort};Uid={ServerInfo.Instance.MySqlUserName};Pwd={ServerInfo.Instance.MySqlPassword};database={databaseName}";

            MySqlConnection connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
                
            connectionInfo = new DBConnectionInfo()
            {
                ConnectionString = connectionString,
                SshClient = sshClient,
                ForwardedPortLocal = forwardedPortLocal,
                MySqlConnection = connection
            };
            
            return connectionInfo;
        }
        catch (MySqlException)
        {
            try
            {
                sshClient = new SshClient(ServerInfo.Instance.SshHost, 
                    int.Parse(ServerInfo.Instance.SshPort), 
                    ServerInfo.Instance.SshUserName, ServerInfo.PrivateKeyFileArray);
            
                await sshClient.ConnectAsync(CancellationToken.None);

                if (sshClient.IsConnected)
                {
                    forwardedPortLocal = new ForwardedPortLocal("localhost", 0, server, (uint)int.Parse(ServerInfo.Instance.MySqlPort));
                    sshClient.AddForwardedPort(forwardedPortLocal);
                    forwardedPortLocal.Start(); // 닫아주기
                    connectionString = $"Server=localhost;Port={forwardedPortLocal.BoundPort};Database={databaseName};Uid={ServerInfo.Instance.MySqlUserName};Pwd={ServerInfo.Instance.MySqlPassword};";
                }
            
                MySqlConnection connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                connectionInfo = new DBConnectionInfo()
                {
                    ConnectionString = connectionString,
                    SshClient = sshClient,
                    ForwardedPortLocal = forwardedPortLocal,
                    MySqlConnection = connection
                };
            
                return connectionInfo;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                
                if (connectionInfo != null)
                {
                    connectionInfo.MySqlConnection?.DisposeAsync();
                    connectionInfo.ForwardedPortLocal?.Dispose();
                    connectionInfo.SshClient?.Dispose();
                }

                return null;
            }
        }
    }
}