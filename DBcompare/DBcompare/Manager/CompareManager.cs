using System.Data;
using DBcompare.Common;
using MySqlConnector;
using Renci.SshNet;

namespace DBcompare.Manager;

public class CompareManager
{
    public class ConnectionInfo
    {
        public string ConnectionString { get; set; }
        public SshClient? SshClient { get; set; }
        public ForwardedPortLocal? ForwardedPortLocal { get; set; }
    }
    
    private static void SetDifferentTablesAfterCompare(List<TableInfo> tableInfos, List<string> differentTables)
    {
        foreach (var differentTable in differentTables)
        {
            foreach (var tableInfo in tableInfos)
            {
                if(tableInfo.tableName == differentTable)
                {
                    Console.WriteLine($"Different Table: {differentTable}");
                    tableInfo.isDifferent = true;
                    break;
                }
            }
        }
    }

    public static async Task<List<TableInfo>> CompareAsync(List<string> servers, string databaseName, List<TableInfo> tableInfos, bool compareAllTables = false)
    {
        string server1 = servers[0];
        string server2 = servers[1];
        
        var connectionInfo1 = await GetConnectionInfoAsync(server1, databaseName);
        var connectionInfo2 = await GetConnectionInfoAsync(server2, databaseName);
        
        using (MySqlConnection connection1 = new MySqlConnection(connectionInfo1.ConnectionString))
        using (MySqlConnection connection2 = new MySqlConnection(connectionInfo2.ConnectionString))
        {
            await connection1.OpenAsync();
            await connection2.OpenAsync();
            
            // check TABLE EXIST
            var tableNameWithNowExisting = await CompareTablesExistAsync(tableInfos, connection1, connection2);
            SetDifferentTablesAfterCompare(tableInfos, tableNameWithNowExisting);
            
            // check TABLE COLUMN
            var tableNamesWithDifferentColumns = await CompareTableColumnsAsync(tableInfos, connection1, connection2);
            SetDifferentTablesAfterCompare(tableInfos, tableNamesWithDifferentColumns);

            // check TABLE DATA. Const 테이블만 비교
            if (databaseName.Contains("Const"))
            {
                var tableNamesWithDifferentData = await CompareTableDataAsync(tableInfos, connection1, connection2);
                SetDifferentTablesAfterCompare(tableInfos, tableNamesWithDifferentData);
            }
            
            // Close SshClient, ForwardedPortLocal
            if (connectionInfo1.SshClient != null && connectionInfo1.ForwardedPortLocal != null)
            {
                connectionInfo1.SshClient?.Dispose();
                connectionInfo2.ForwardedPortLocal?.Dispose();
                Console.WriteLine("connectionInfo1 Disposed!!");
            }
            
            if (connectionInfo2.SshClient != null && connectionInfo2.ForwardedPortLocal != null)
            {
                connectionInfo1.SshClient?.Dispose();
                connectionInfo2.ForwardedPortLocal?.Dispose();
                Console.WriteLine("connectionInfo2 Disposed!!");
            }

            return tableInfos;
        }
    }

    private static async Task<ConnectionInfo> GetConnectionInfoAsync(string server, string databaseName)
    {
        try
        {
            SshClient? sshClient = null;
            ForwardedPortLocal? forwardedPortLocal = null;
            
            string connectionString = String.Empty;

            if (!server.Contains("Pwd")) // ssh tunnel 사용중 (Live)
            {
                sshClient = new SshClient(ServerInfo.Instance.SshHost, 
                    int.Parse(ServerInfo.Instance.SshPort), 
                    ServerInfo.Instance.SshUserName, ServerInfo.PrivateKeyFileArray);
            
                await sshClient.ConnectAsync(CancellationToken.None); // 닫아주기

                if (sshClient.IsConnected)
                {
                    forwardedPortLocal = new ForwardedPortLocal("localhost", 0, server, (uint)int.Parse(ServerInfo.Instance.MysqlPort));
                    sshClient.AddForwardedPort(forwardedPortLocal);
                    forwardedPortLocal.Start(); // 닫아주기
                    connectionString = $"Server=localhost;Port={forwardedPortLocal.BoundPort};Database={databaseName};Uid={ServerInfo.Instance.MySqlUserName};Pwd={ServerInfo.Instance.MySqlPassword};";
                }
            }
            else
            {
                connectionString = $"{server};database={databaseName}";
            }

            ConnectionInfo connectionInfo = new ConnectionInfo()
            {
                ConnectionString = connectionString,
                SshClient = sshClient,
                ForwardedPortLocal = forwardedPortLocal
            };
            
            return connectionInfo;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }
    
    private static async Task<List<string>> CompareTablesExistAsync(List<TableInfo> tableInfos, MySqlConnection connection1, MySqlConnection connection2)
    {
        var tableNames = tableInfos.Select(e => e.tableName).ToList();

        var server1MissingTable = await TableExistsAsync(connection1, tableNames);
        var server2MissingTable = await TableExistsAsync(connection2, tableNames);

        var combinedList = server1MissingTable.Union(server2MissingTable).ToList();
                
        return combinedList;
    }

    public static async Task<List<string>> GetExistingTablesAsync(MySqlConnection connection, List<string> tableNames)
    {
        // List<string> missingTables = new List<string>();

        foreach (string tableName in tableNames)
        {
            string query = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{connection.Database}' AND table_name = '{tableName}'";

            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                object result = await command.ExecuteScalarAsync();
                if (result == null)
                    break;

                long count = (long)result;
                if (count == 0)
                {
                    tableNames.Remove(tableName);
                }
            }
        }

        return tableNames;
    }
    private static async Task<List<string>> TableExistsAsync(MySqlConnection connection, List<string> tableNames)
    {
        List<string> missingTables = new List<string>();

        foreach (string tableName in tableNames)
        {
            string query = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{connection.Database}' AND table_name = '{tableName}'";

            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                object result = await command.ExecuteScalarAsync();
                if (result == null)
                    break;

                long count = (long)result;
                if (count == 0)
                {
                    missingTables.Add(tableName);
                }
            }
        }

        return missingTables;
    }

    private static async Task<List<string>> CompareTableColumnsAsync(List<TableInfo> tableInfos, MySqlConnection connection1, MySqlConnection connection2)
    {
        var tableNames = tableInfos.Select(e => e.tableName).ToList();

        List<string> differentTables = new List<string>();

        foreach (string tableName in tableNames)
        {
            List<Column> columnsServer1 = await GetTableColumnsAsync(connection1, tableName);
            List<Column> columnsServer2 = await GetTableColumnsAsync(connection2, tableName);

            if (!AreColumnsEqual(columnsServer1, columnsServer2))
            {
                Console.WriteLine($"Name1 = {columnsServer1.First().Name}");
                Console.WriteLine($"DataType1 = {columnsServer1.First().DataType}");
                    
                Console.WriteLine($"Name2 = {columnsServer2.First().Name}");
                Console.WriteLine($"DataType2 = {columnsServer2.First().DataType}");
                    
                differentTables.Add(tableName);
            }
        }
        
        return differentTables;
    }

    public static async Task<List<Column>> GetTableColumnsAsync(MySqlConnection connection, string tableName)
    {
        List<Column> columns = new List<Column>();

        string query = $"SELECT COLUMN_NAME, DATA_TYPE FROM information_schema.columns WHERE table_schema = '{connection.Database}' AND table_name = '{tableName}' order by COLUMN_NAME";

        using (MySqlCommand command = new MySqlCommand(query, connection))
        using (MySqlDataReader reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                string columnName = reader.GetString(0);
                string dataType = reader.GetString(1);
                // int length = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);

                columns.Add(new Column(columnName, dataType));
            }
        }

        return columns;
    }

    private static bool AreColumnsEqual(List<Column> columns1, List<Column> columns2)
    {
        if (columns1.Count != columns2.Count)
        {
            return false;
        }

        for (int i = 0; i < columns1.Count; i++)
        {
            if (!columns1[i].IsEqualTo(columns2[i]))
            {
                return false;
            }
        }

        return true;
    }
    
    private static async Task<List<string>> CompareTableDataAsync(List<TableInfo> tableInfos, MySqlConnection connection1, MySqlConnection connection2)
    {
        var tableNames = tableInfos.Select(e => e.tableName).ToList();

        List<string> differentTables = new List<string>();

        foreach (string tableName in tableNames)
        {
            DataTable tableServer1 = await GetTableDataAsync(connection1, tableName);
            DataTable tableServer2 = await GetTableDataAsync(connection2, tableName);

            if (!AreTablesEqual(tableServer1, tableServer2))
            {
                differentTables.Add(tableName);
            }
        }

        return differentTables;
    }

    private static async Task<DataTable> GetTableDataAsync(MySqlConnection connection, string tableName)
    {
        string query = $"SELECT * FROM {tableName}";

        using (MySqlCommand command = new MySqlCommand(query, connection))
        {
            using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
            {
                DataTable dataTable = new DataTable();
                await Task.Run(() => adapter.Fill(dataTable));
                return dataTable;
            }
        }
    }

    private static bool AreTablesEqual(DataTable table1, DataTable table2)
    {
        if (table1.Rows.Count != table2.Rows.Count || table1.Columns.Count != table2.Columns.Count)
        {
            return false;
        }

        for (int i = 0; i < table1.Rows.Count; i++)
        {
            for (int j = 0; j < table1.Columns.Count; j++)
            {
                if (!table1.Rows[i].ItemArray[j].Equals(table2.Rows[i].ItemArray[j]))
                {
                    return false;
                }
            }
        }

        return true;
    }
    
}