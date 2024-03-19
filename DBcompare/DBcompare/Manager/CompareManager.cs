using System.Data;
using System.Diagnostics;
using DBcompare.Common;
using Enum;
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
    
    private static void SetDifferentTablesAfterCompare(List<TableInfo> tableInfos, List<string> differentTables, DifferentType differentType)
    {
        foreach (var differentTable in differentTables)
        {
            foreach (var tableInfo in tableInfos)
            {
                if(tableInfo.tableName == differentTable)
                {
                    Console.WriteLine($"Different Table: {differentTable}");
                    tableInfo.isDifferent = true;
                    tableInfo.DifferentType = differentType;
                    break;
                }
            }
        }
    }

    public static async Task<List<TableInfo>> CompareAsync(List<string> servers, string databaseName, List<TableInfo> tableInfos, bool compareAllTables = false)
    {
        Stopwatch stopwatch = new Stopwatch();
        
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
            stopwatch.Start();
            var tableNameWithNowExisting = await CompareTablesExistAsync(tableInfos, connection1, connection2);
            SetDifferentTablesAfterCompare(tableInfos, tableNameWithNowExisting, DifferentType.TableNotExist);
            stopwatch.Stop();
            TimeSpan elapsedTime1 = stopwatch.Elapsed;
            Console.WriteLine("Elapsed Time1: " + elapsedTime1);
            stopwatch.Reset();
            
            // check TABLE COLUMN
            stopwatch.Start();
            var tableNamesWithDifferentColumns = await CompareTableColumnsAsync(tableInfos.Where(e => e.DifferentType == DifferentType.None).ToList(), connection1, connection2);
            SetDifferentTablesAfterCompare(tableInfos, tableNamesWithDifferentColumns, DifferentType.ColumnDifferent);
            stopwatch.Stop();
            TimeSpan elapsedTime2 = stopwatch.Elapsed;
            Console.WriteLine("Elapsed Time2: " + elapsedTime2);
            stopwatch.Reset();

            // check TABLE DATA. Const 테이블만 비교
            if (databaseName.Contains("Const"))
            {
                stopwatch.Start();
                var tableNamesWithDifferentData = await CompareTableDataAsync(tableInfos.Where(e => e.DifferentType == DifferentType.None).ToList(), connection1, connection2);
                SetDifferentTablesAfterCompare(tableInfos, tableNamesWithDifferentData, DifferentType.DataDifferent);
                stopwatch.Stop();
                TimeSpan elapsedTime3 = stopwatch.Elapsed;
                Console.WriteLine("Elapsed Time3: " + elapsedTime3);
                stopwatch.Reset();
            }
            
            // Close SshClient, ForwardedPortLocal
            /*if (connectionInfo1.SshClient != null && connectionInfo1.ForwardedPortLocal != null)
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
            }*/

            return tableInfos;
        }
    }

    public static async Task<ConnectionInfo> GetConnectionInfoAsync(string server, string databaseName)
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
        // var tableNames = tableInfos.Select(e => e.tableName).ToList();

        List<string> differentTables = new List<string>();

        foreach (var tableInfo in tableInfos)
        {
            List<Column> columnsServer1 = await GetTableColumnsAsync(connection1, tableInfo.tableName);
            List<Column> columnsServer2 = await GetTableColumnsAsync(connection2, tableInfo.tableName);

            var areColumnsEqual = AreColumnsEqual(columnsServer1, columnsServer2, tableInfo);
            
            if (!areColumnsEqual)
            {
                tableInfo.DifferentType = DifferentType.ColumnDifferent;
                differentTables.Add(tableInfo.tableName);
            }
        }
        
        return differentTables;
    }

    public static async Task<List<Column>> GetTableColumnsAsync(MySqlConnection connection, string tableName)
    {
        List<Column> columns = new List<Column>();

        string query = $"SELECT COLUMN_NAME, DATA_TYPE FROM information_schema.columns WHERE table_schema = '{connection.Database}' AND table_name = '{tableName}' ORDER BY ORDINAL_POSITION;";

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

    private static bool AreColumnsEqual(List<Column> columns1, List<Column> columns2, TableInfo tableInfo)
    {
        /*
        List<Column> columns1Copy = columns1.Select(e => e).ToList();
        List<Column> columns2Copy = columns2.Select(e => e).ToList();
        */

        List<string> columns1StringList = columns1.Select(e => e.StringFormat).ToList();
        List<string> columns2StringList = columns2.Select(e => e.StringFormat).ToList();
        
        List<string> copyColumns1StringList = columns1StringList.Select(e => e).ToList();
        List<string> copyColumns2StringList = columns2StringList.Select(e => e).ToList();
        
        for (int i = 0; i < columns1.Count; i++)
        {
            for (int j = 0; j < columns2.Count; j++)
            {
                if (columns1[i].Name == columns2[j].Name & columns1[i].DataType == columns2[j].DataType)
                {
                    copyColumns1StringList[i] = string.Empty;
                    copyColumns2StringList[j] = string.Empty;
                    break;
                }
            }
        }
        
        var differentColumns1 = copyColumns1StringList.Where(e => e != string.Empty).ToList();
        var differentColumns2 = copyColumns2StringList.Where(e => e != string.Empty).ToList();

        if (differentColumns1.Count == 0 && differentColumns2.Count == 0)
        {
            // 테이블 컬럼들이 똑같으면 다음 row비교를 위해 컬럼들 넣어주기 (컬럼은 보여주기용으로 넣는다)
            tableInfo.Columns[0] = columns1StringList;
            tableInfo.Columns[1] = columns2StringList;
            
            return (true);
        }
        else
        {
            tableInfo.Columns[0] = differentColumns1;
            tableInfo.Columns[1] = differentColumns2;
            
            return (false);
        }
    }
    
    private static async Task<List<string>> CompareTableDataAsync(List<TableInfo> tableInfos, MySqlConnection connection1, MySqlConnection connection2)
    {
        // var tableNames = tableInfos.Select(e => e.tableName).ToList();

        List<string> differentTables = new List<string>();

        foreach (var tableInfo in tableInfos)
        {
            DataTable tableServer1 = await GetTableDataAsync(connection1, tableInfo.tableName);
            DataTable tableServer2 = await GetTableDataAsync(connection2, tableInfo.tableName);

            if (!AreTablesEqual(tableServer1, tableServer2, tableInfo))
            {
                tableInfo.isDifferent = true;
                tableInfo.DifferentType = DifferentType.DataDifferent;
                differentTables.Add(tableInfo.tableName);
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

    public class MyDataTable
    {
        DataRowCollection row;
        bool isRowEqual = false;
    }
    
    private static bool AreTablesEqual(DataTable table1, DataTable table2, TableInfo tableInfo)
    {
        // tableInfo 안에 데이터가 다르면 isDifferent = true로 바꿔주기, DifferentType 세팅, 다른 데이터 row들 넣어주기
        
        MyDataTable myDataTable1 = new MyDataTable() {};
        
        int itemArrayCount = tableInfo.Columns[0].Count;

        for (int i = 0; i < table1.Rows.Count; i++)
        {
            bool foundSameRow = false;
            for (int j = 0; j < table2.Rows.Count; j++)
            {
                for (int k = 0; k < itemArrayCount; k++) // row가 같은지 다른지 체크
                {
                    if (!table1.Rows[i].ItemArray[k].Equals(table2.Rows[j].ItemArray[k]))
                    {
                        break;
                    }
                    
                    if (k == itemArrayCount - 1)
                        foundSameRow = true;
                }
                
                if(foundSameRow)
                    break;

                if (j == table2.Rows.Count - 1)
                {
                    if (foundSameRow == false)
                    {
                        var list = new List<object?>();

                        foreach (var item in table1.Rows[i].ItemArray)
                        {
                            list.Add(item);
                        }
                        
                        tableInfo.Table1DifferentRows.Add(list);
                    }
                }
            }
        }
        
        for (int i = 0; i < table2.Rows.Count; i++)
        {
            bool foundSameRow = false;
            for (int j = 0; j < table1.Rows.Count; j++)
            {
                for (int k = 0; k < itemArrayCount; k++) // row가 같은지 다른지 체크
                {
                    if (!table2.Rows[i].ItemArray[k].Equals(table1.Rows[j].ItemArray[k]))
                    {
                        break;
                    }
                    
                    if (k == itemArrayCount - 1)
                        foundSameRow = true;
                }
                
                if(foundSameRow)
                    break;

                if (j == table1.Rows.Count - 1)
                {
                    if (foundSameRow == false)
                    {
                        var list = new List<object?>();

                        foreach (var item in table2.Rows[i].ItemArray)
                        {
                            list.Add(item);
                        }
                        
                        tableInfo.Table2DifferentRows.Add(list);
                    }
                }
            }
        }
        
        return tableInfo.Table1DifferentRows.Count == 0 && tableInfo.Table2DifferentRows.Count == 0;
    }
    
}