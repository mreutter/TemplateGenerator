using BackendTemplateCreator.Generator;
using System.Text.RegularExpressions;
using System.Text;

namespace BackendTemplateCreator;

public class SQLParser
{
    private static string CleanSql(string sql)
    {
        // Remove line comments
        sql = Regex.Replace(sql, @"--.*?$", "", RegexOptions.Multiline);

        // Remove block comments
        sql = Regex.Replace(sql, @"/\*.*?\*/", "", RegexOptions.Singleline);

        // Trim extra whitespace
        sql = Regex.Replace(sql, @"\s+", " ");

        return sql.Replace("`", "").Trim();
    }
    private static IEnumerable<string> ExtractTableDefinitions(string sql)
    {
        var tableMatches = Regex.Matches(sql, @"CREATE\s+TABLE\s+(\w+)\s*\((.*?)\;", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        foreach (Match m in tableMatches)
        {
            yield return m.Value;
        }
    }

    private static List<Property> ParseColumns(string tableSql, string tableName)
    {
        var columns = new List<Property>();
        //var inner = Regex.Match(tableSql, @"\((.*?)\;", RegexOptions.Singleline).Groups[1].Value;

        StringBuilder tableDefinition = new StringBuilder();

        //Replacement for RegEx (isnt required)
        int level = 0;
        for (int i = 0; i < tableSql.Length; i++)
        {
            char curChar = tableSql[i];
            if (level > 0 && !(level == 1 && curChar == ')')) tableDefinition.Append(curChar);
            level += curChar == '(' ? 1 : (curChar == ')' ? -1 : 0);
        }

        var inner = tableDefinition.ToString().Split(',').Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l));

        foreach (var line in inner)
        {
            if (line.StartsWith("CONSTRAINT", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("PRIMARY", StringComparison.OrdinalIgnoreCase))
                continue;

            if (line.StartsWith("CHECK", StringComparison.OrdinalIgnoreCase))
            {
                GeneratorHelper.Admonition("Table \"{tableName}\" uses a CHECK. This must be implemented manually.", "SQL ");
            }

            var match = Regex.Match(line, @"^(\w+)\s+(\w+)(?:\((\d+)\))?.*$", RegexOptions.IgnoreCase);
            if (!match.Success) continue;

            if (line.StartsWith("FOREIGN", StringComparison.OrdinalIgnoreCase))
            {
                string[] words = line.Split(' ');
                string foreignKey = Regex.Replace(words[2], @"[\(\)]+", "");
                string[] referencedTableAndId = words[4].Split('(');
                string referencedTable = referencedTableAndId[0];
                string referencedId = referencedTableAndId[1].Replace(")", "");
                GeneratorHelper.Warn("\"{foreignKey}\" in table \"{tableName}\" is a foreign key which references \"{referencedId}\" from table \"{referencedTable}\"", "SQL ");
                continue;
            }


            bool isNullable = !line.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase) && !line.Contains("AUTO_INCREMENT", StringComparison.OrdinalIgnoreCase);
            var column = new Property
            {
                DatabaseName = match.Groups[1].Value,
                CSharpType = MapSqlTypeToCSharp(match.Groups[2].Value, isNullable),
                Length = int.TryParse(match.Groups[3].Value, out int len) ? len : (int?)null,
                IsRequired = line.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase),
                IsPrimaryKey = line.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase)
            };

            columns.Add(column);
        }

        return columns;
    }

    public static string MapSqlTypeToCSharp(string sqlType, bool nullable)
    {
        string type = sqlType.ToLower() switch
        {
            "int" => "int",
            "bigint" => "long",
            "smallint" => "short",
            "tinyint" => "byte", //Might map to bool if tinyint(1)
            "bit" => "bool",
            "decimal" or "numeric" => "decimal",
            "float" or "real" => "double",
            "date" => "DateOnly",
            "datetime" or "timestamp" => "DateTime",
            "nvarchar" or "varchar" or "text" or "char" => "string",
            _ => "string"
        };

        if (type != "string" && nullable)
            type += "?";

        return type;
    }

    public static List<Table> GenerateTablesFromSQL(string sql)
    {
        string cleanSql = CleanSql(sql);
        IEnumerable<string> definitions = ExtractTableDefinitions(cleanSql);

        List<Table> tables = new();

        foreach (string definition in definitions)
        {
            Table table = new Table();
            table.DatabaseName = definition.Split(' ')[2];
            table.Properties = ParseColumns(definition, table.DatabaseName);
            tables.Add(table);
        }

        return tables;
    }

}
public class Table
{
    //Name
    public string DatabaseName { get; set; } = "";
    public string ModelName { get; set; } = "";

    //CRUD
    public bool UseGetById { get; set; }
    public bool UseGetAll { get; set; }
    public bool UseAdd { get; set; }
    public bool UseUpdate { get; set; }
    public bool UseDelete { get; set; }

    //Properties
    public List<Property> Properties { get; set; }
}

public class Property
{
    //Name
    public string DatabaseName { get; set; } = "";
    public string ModelName { get; set; } = "";

    //Type
    public string CSharpType { get; set; } = "";
    public int? Length { get; set; }

    //Additional Info
    public bool IsNullable { get; set; }
    public bool IsRequired { get; set; }
    public bool IsPrimaryKey { get; set; }

    //Misc.
    public bool IsDtoProperty { get; set; } = true;
}
