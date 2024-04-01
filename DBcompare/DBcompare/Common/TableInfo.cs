using System.Data;
using Enum;

namespace DBcompare.Common;

public class TableInfo
{
    public string? TableName;
    public bool IsDifferent = false;
    public DifferentType DifferentType = DifferentType.None;
    public List<string> PrimaryKeys = new List<string>();
    
    // 다를경우에 추가해주기, 같으면 기존 컬럼 전부 추가
    public List<string>[] Columns = new List<string>[2];
    
    // 인덱스가 잡혀있는 테이블 (특정 테이블에만 존재하는 row)
    public Dictionary<string, List<object>> Table1UniqueKeyDictionary = new Dictionary<string, List<object>>();
    public Dictionary<string, List<object>> Table2UniqueKeyDictionary = new Dictionary<string, List<object>>();
    
    // 인덱스가 잡혀있는 테이블 (테이블에 공통으로 존재하는 row중 Value값이 다른 row)
    public Dictionary<string, List<object>> Table1ValueDiffDictionary = new Dictionary<string, List<object>>();
    public Dictionary<string, List<object>> Table2ValueDiffDictionary = new Dictionary<string, List<object>>();
    
    // 인덱스가 안잡혀있는 테이블일경우 추가해주기
    public List<List<object?>> Table1DifferentRows = new List<List<object?>>();
    public List<List<object?>> Table2DifferentRows = new List<List<object?>>();
}