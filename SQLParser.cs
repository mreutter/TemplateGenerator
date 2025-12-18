using BackendTemplateCreator.Generator;
using System.Text.RegularExpressions;
using System.Text;
using Spectre.Console;

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

    public enum CardinalityEnum {
        OneToOne,
        OneToMany, // {Nothing} -> ICollection<Product> (Only if ManyToOne exists)
        ManyToOne // ProductId -> Product
    };
    //Origin Table and Foreign key are known and therefore omitted
    public class ForeignKeyReference
    {
        public string ReferencedTable { get; set; }
        public string ReferencedKey { get; set; }
        public CardinalityEnum Cardinality { get; set; } //From Perspective of table -> There is no many to one since other side wouldnt keep a reference
    }

    //Graph Problem
    public static Dictionary<(string,string),Property> ForeignKeys = new();
    public static void AdjustCardinality(List<Table> tables)
    {
        Dictionary<(string, string), Property> referencedProperties = new(); 
        //Only changes references so no need for return
        foreach (var tableAndProperty in ForeignKeys.Keys)
        {
            var property = ForeignKeys[tableAndProperty];
            var fkr = property.ForeignKeyReference;
            (string, string) reference = (fkr.ReferencedTable, fkr.ReferencedKey);

            if (ForeignKeys.ContainsKey(reference))
            {
                //One to one on both
                property.ForeignKeyReference.Cardinality = CardinalityEnum.OneToOne;
                ForeignKeys[reference].ForeignKeyReference.Cardinality = CardinalityEnum.OneToOne;
            } else
            {
                //Set i.cardinality to one to many
                property.ForeignKeyReference.Cardinality = CardinalityEnum.OneToMany;
                //Set referece.cardinalty to many to one


                Property referencedProperty = tables.Find(t => t.DatabaseName == reference.Item1).Properties.Find(p => p.DatabaseName == reference.Item2); //Is this reference or pointer?
                if(referencedProperty == null)
                {
                    GeneratorHelper.Error($"Couldnt find \"{reference.Item2}\" of table \"{reference.Item1}\" referenced by \"{tableAndProperty.Item2}\" of table \"{tableAndProperty.Item1}\".", true, "SQL ");
                }
                referencedProperty.ForeignKeyReference = new ForeignKeyReference() { ReferencedTable=tableAndProperty.Item1, ReferencedKey=tableAndProperty.Item2, Cardinality=CardinalityEnum.ManyToOne };
                referencedProperty.IsForeignKey = true; //Meaning its referenced from somewhere else
            }
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
                GeneratorHelper.Admonition($"Table \"{tableName}\" uses a CHECK. This must be implemented manually.", "SQL ");
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


                //Supposes Foreign Key Declarations are at end
                Property foreignKeyProperty = columns.Find(i => i.DatabaseName == foreignKey);
                if (foreignKeyProperty == null) GeneratorHelper.Error($"Foreign Key Refence \"{foreignKey}\" in \"{tableName}\" is invalid."); //Fatal

                foreignKeyProperty.IsForeignKey = true;
                foreignKeyProperty.ForeignKeyReference = new ForeignKeyReference() { ReferencedTable = referencedTable, ReferencedKey = referencedId };

                ForeignKeys[(tableName, foreignKey)] = foreignKeyProperty;
                
                GeneratorHelper.Warn($"\"{foreignKey}\" in table \"{tableName}\" is a foreign key which references \"{referencedId}\" from table \"{referencedTable}\"", "SQL ");
                continue;
            }


            bool isNullable = !line.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase) && !line.Contains("AUTO_INCREMENT", StringComparison.OrdinalIgnoreCase);
            bool isPrimaryKey = line.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase);
            var column = new Property
            {
                DatabaseName = match.Groups[1].Value,
                CSharpType = MapSqlTypeToCSharp(match.Groups[2].Value, isNullable),
                Length = int.TryParse(match.Groups[3].Value, out int len) ? len : (int?)null,
                IsRequired = line.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase),
                IsPrimaryKey = isPrimaryKey,
                IsDtoProperty = !isPrimaryKey //Standard to not show Id in DTO
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
            "double" => "double",
            "float" or "real" => "float",
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
        AdjustCardinality(tables);

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
    public bool IsChangeableByDto { get; set; } = true; //If false will not show up in create & update dto even if it is a dto property

    public bool IsForeignKey { get; set; }
    public SQLParser.ForeignKeyReference? ForeignKeyReference { get; set; } = null;
}
