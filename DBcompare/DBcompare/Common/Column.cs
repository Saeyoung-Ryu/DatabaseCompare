namespace DBcompare.Common;

public class Column
{
    public string Name { get; }
    public string DataType { get; }
    // public int Length { get; } // Add Length property or any other desired properties

    public Column(string name, string dataType) // Modify constructor accordingly
    {
        Name = name;
        DataType = dataType;
        // Length = length;
    }

    public bool IsEqualTo(Column other)
    {
        return Name == other.Name && DataType == other.DataType; // Update the comparison accordingly
    }
}

