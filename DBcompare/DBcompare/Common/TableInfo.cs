using System.Data;
using Enum;

namespace DBcompare.Common;

public class TableInfo
{
    public string tableName;
    public bool isDifferent = false;
    public DifferentType DifferentType = DifferentType.None;
    
    // 다를경우에 추가해주기
    public List<string>[] Columns = new List<string>[2];
    
    public List<List<object?>> Table1DifferentRows = new List<List<object?>>();
    public List<List<object?>> Table2DifferentRows = new List<List<object?>>();
}