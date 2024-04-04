using DBcompare.Common;
using DBcompare.Manager;
using Enum;
using Microsoft.AspNetCore.Components;

namespace DBcompare.Pages;

public partial class CompareDump
{
#pragma warning disable CS8600    
#pragma warning disable CS8601
#pragma warning disable CS8604    
#pragma warning disable CS8618
#pragma warning disable CS8620    
    
    int tabIndex = 0;
    int tabIndex1 = 0;

    Dictionary<string, bool> serverCheckedDictionary = new Dictionary<string, bool>();
    Dictionary<string, bool> dumpCheckedDictionary = new Dictionary<string, bool>();

    bool compareRow = true; 
    bool compareAllTables = false;
    
    bool showDatabaseSelect = false;
    bool showTableSelect = false;
    bool doCompare = false;
    bool doDump = false;
    bool isLoading = false;
    bool isDumpLoading = false;
    bool finishedDump = false;
    string? compareDatabaseName = "";
    string tableToAdd = "";
    
    List<TableInfo> tableList = new List<TableInfo>();
    List<TableInfo> tableListForTabDelete = new List<TableInfo>();
    
    [Parameter]
    public string ProjectName { get; set; }
    
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ResetObjects();
    }
    
    protected override Task OnInitializedAsync()
    {
        Console.WriteLine("Initialized");

        foreach (var projectName in ServerInfo.Instance.ConnectionStrings[ProjectName].Keys)
        {
            serverCheckedDictionary.Add(projectName, false);
            dumpCheckedDictionary.Add(projectName, false);
        }
        
        return base.OnInitializedAsync();
    }

    private void ResetObjects()
    {
        ResetTableList();
    }
    
    private void OnSelectChange(ChangeEventArgs e)
    {
        compareDatabaseName = e?.Value?.ToString() ?? ""; // Null-conditional operator and null-coalescing operator used here
    }
    
    private bool CheckIfTwoIsSelected()
    {
        int count = 0;
        
        foreach (var value in serverCheckedDictionary.Values)
        {
            if (value == true)
                count++;
        }

        return count == 2;
    }

    private bool CheckIfOneIsSelected()
    {
        int count = 0;
        
        foreach (var value in dumpCheckedDictionary.Values)
        {
            if (value == true)
                count++;
        }

        return count == 1;
    }
    
    private void SelectServerBtn()
    {
        showDatabaseSelect = true;
    }

    private void SelectDatabaseBtn()
    {
        showTableSelect = true;
    }

    private void AddTableList()
    {
        if (tableToAdd != String.Empty)
        {
            TableInfo tableInfo = new TableInfo();

            if (!tableToAdd.StartsWith("tbl"))
                tableToAdd = "tbl" + tableToAdd;

            if(tableList.Select(e => e.TableName).ToList().Contains(tableToAdd))
                return;
            
            tableInfo.TableName = tableToAdd;

            tableList.Add(tableInfo);
        }
        tableToAdd = String.Empty;
    }

    private void ResetTableInfo()
    {
        foreach (var tableInfo in tableList)
        {
            tableInfo.IsDifferent = false;
            tableInfo.DifferentType = DifferentType.None;
            tableInfo.PrimaryKeys = new List<string>();
            
            tableInfo.Columns = new List<string>[2];
            tableInfo.Table1UniqueKeyDictionary = new Dictionary<string, List<object>>();
            tableInfo.Table2UniqueKeyDictionary = new Dictionary<string, List<object>>();
            tableInfo.Table1ValueDiffDictionary = new Dictionary<string, List<object>>();
            tableInfo.Table2ValueDiffDictionary = new Dictionary<string, List<object>>();
            tableInfo.Table1DifferentRows = new List<List<object?>>();
            tableInfo.Table2DifferentRows = new List<List<object?>>();
        }
    }

    private void ResetTableList()
    {
        tabIndex = 0;
        tabIndex1 = 0;

        serverCheckedDictionary = new Dictionary<string, bool>();
        dumpCheckedDictionary = new Dictionary<string, bool>();
        
        foreach (var projectName in ServerInfo.Instance.ConnectionStrings[ProjectName].Keys)
        {
            serverCheckedDictionary.Add(projectName, false);
            dumpCheckedDictionary.Add(projectName, false);
        }

        showDatabaseSelect = false;
        showTableSelect = false;
        doCompare = false;
        compareRow = true;
        doDump = false;
        isLoading = false;
        isDumpLoading = false;
        finishedDump = false;

        compareDatabaseName = "";
        tableToAdd = "";
        tableList = new List<TableInfo>();
        tableListForTabDelete = new List<TableInfo>();
    }

    private async Task StartCompareBtnAsync()
    {
        ResetTableInfo();
        
        List<string> servers = new List<string>();
        string database = compareDatabaseName;

        foreach (var key in serverCheckedDictionary.Keys)
        {
            if (serverCheckedDictionary[key])
                servers.Add(ServerInfo.Instance.ConnectionStrings[ProjectName][key]);
        }

        isLoading = true;
        
        if (database == string.Empty)
            database = ServerInfo.Instance.Databases[ProjectName][0];
        
        var compareResult = await CompareManager.CompareAsync(servers, database, tableList, compareRow, compareAllTables);
        tableList = compareResult;
        tableListForTabDelete = compareResult.Select(e => e).ToList();
        
        doCompare = true;
        isLoading = false;
    }

    private void DumpBtn()
    {
        doDump = true;
    }

    private async Task DumpAsyncBtn()
    {
        isDumpLoading = true;
        
        string server = String.Empty;
        string database = compareDatabaseName;
        List<string> tables = new List<string>();
        
        foreach (var key in dumpCheckedDictionary.Keys)
        {
            if (dumpCheckedDictionary[key])
                server = ServerInfo.Instance.ConnectionStrings[ProjectName][key];
        }
        
        if (database == string.Empty)
            database = ServerInfo.Instance.Databases[ProjectName][0];
        
        await DumpManager.DumpAsync(database, server, tableList.Where(e => e.IsDifferent).Select(e => e.TableName).ToList(), database == "HomerunClashConst");
        
        isDumpLoading = false;
        finishedDump = true;
    }
    
    void CloseTab(TableInfo tableInfo)
    {
        tableListForTabDelete = tableListForTabDelete.Where(e => e.TableName != tableInfo.TableName).ToList();
    }
}