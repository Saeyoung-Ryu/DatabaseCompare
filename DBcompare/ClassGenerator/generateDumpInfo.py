import json
import os

def generate_csharp_class(json_data, class_name, additional_usings=None):
    csharp_class = ""

    # Include additional using statements
    if additional_usings:
        for using_statement in additional_usings:
            csharp_class += f"using {using_statement};\n"

    csharp_class += f"namespace DBcompare.Common\n\n"

    csharp_class += f"public class {class_name}\n{{\n"
    
    # Add the Instance property
    csharp_class += f"    public static {class_name} Instance {{ get; private set; }}\n"

    for key, value in json_data.items():
        csharp_class += f"    public string {key} {{ get; set; }}\n"

    # Add the Refresh method
    csharp_class += "\n"
    csharp_class += "    [Refreshable]\n"
    csharp_class += f"    public static void Refresh()\n"
    csharp_class += "    {\n"
    csharp_class += f"        var configurationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, \"{class_name}.config\");\n"
    csharp_class += f"        {class_name} dumpInfo = JsonConvert.DeserializeObject<{class_name}>(File.ReadAllText(configurationPath));\n"
    csharp_class += "\n"
    csharp_class += f"        Console.WriteLine($\"{class_name} Refreshed\");\n"
    csharp_class += "\n"
    csharp_class += f"        Instance = dumpInfo;\n"
    csharp_class += "    }\n"

    csharp_class += "}"

    return csharp_class

json_file_path = os.path.expanduser("~/Project/DBcompare/DBcompare/DBcompare/DumpInfo.config")
class_name = "DumpInfo"

# Additional using statements
additional_usings = ["Newtonsoft.Json", "System", "System.IO", "Microsoft.Extensions.Configuration", "Renci.SshNet"]

with open(json_file_path, "r") as file:
    json_data = json.load(file)

csharp_class_code = generate_csharp_class(json_data, class_name, additional_usings)

# Save the generated C# class code to a file
output_file_path = os.path.expanduser("~/Project/DBcompare/DBcompare/ClassGenerator/Generated/DumpInfo.cs")
with open(output_file_path, "w") as output_file:
    output_file.write(csharp_class_code)

print(f"C# class generated and saved to {output_file_path}")
