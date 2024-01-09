using MySql.Data.MySqlClient;
using System.Text;
using Dapper;
using DBcompare.Common;

namespace DBcompare.Manager;

public class DumpManager
{
    public static async Task DumpAsync(string databaseName, string server, List<string> tableNames)
    {
        string conn = $"{server};Database={databaseName}";
        Console.WriteLine($"Conn : {conn}");
        using (MySqlConnection myCon = new MySqlConnection(conn))
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                using (MySqlBackup mb = new MySqlBackup(cmd))
                {
                    try
                    {
                        StringBuilder exportedCombine = new StringBuilder();
                        await myCon.OpenAsync();
                        cmd.Connection = myCon;
                        foreach (var tableName in tableNames)
                        {
                            mb.ExportInfo.EnableComment = true;
                            mb.ExportInfo.ExportEvents = true;
                            mb.ExportInfo.ExportFunctions = true;
                            mb.ExportInfo.ExportProcedures = true;
                            mb.ExportInfo.ExportRows = true;
                            mb.ExportInfo.ExportTriggers = true;
                            mb.ExportInfo.ExportViews = true;
                            mb.ExportInfo.AddDropTable = true;
                            mb.ExportInfo.ExportTableStructure = true;
                            mb.ExportInfo.TablesToBeExportedList = new List<string>() {tableName}; // <= 여기다가 뽑을 테이블명 정리하기
                            
                            string exported = mb.ExportToString();

                            exportedCombine.AppendLine("SET NAMES utf8mb4;");
                            exportedCombine.AppendLine("SET FOREIGN_KEY_CHECKS = 0;");
                            exportedCombine.AppendLine(exported);
                            exportedCombine.AppendLine("COMMIT;");
                            exportedCombine.AppendLine("SET FOREIGN_KEY_CHECKS = 1;");
                            exportedCombine.AppendLine();
                        }
                        
                        string savePath = $@"{DumpInfo.Instance.DumpFileSavePath}";
                        File.WriteAllText(savePath, exportedCombine.ToString(), Encoding.Default);

                        string[] connArray = conn.Split(new char[] {';'});
                        string testConn = DumpInfo.Instance.DumpLogSaveServerAddress;       
                        DateTime time = DateTime.Now;           
                        using (MySqlConnection connection = new MySqlConnection(testConn))
                        {
                            await connection.OpenAsync();
                            foreach (var tableName in tableNames)
                            {
                                Console.WriteLine(connArray[0]);
                                await connection.ExecuteAsync($"INSERT INTO dumpLog (`connectionString`, `tableName`, `time`) VALUES ('{connArray[0]}', '{tableName}', '{time.ToString("yyyy-MM-dd HH:mm:ss")}')");
                                Console.WriteLine($"{tableName} dumped");
                            }
                        }
                        await myCon.CloseAsync();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    
                }
            }
        }
    }
}