using System.Text;

namespace BackendTemplateCreator.Generator;
public class DataAccessGenerator
{
    //Local folder scoped directories
    private string _targetDirectory;
    private string _templateDirectory;

    //Refernce to Orchistrator with config & name info
    private BackendGenerator _g;
    public DataAccessGenerator(BackendGenerator backendGen)
    {
        _g = backendGen;

        _templateDirectory = _g.templateDirectory + "DataAccess\\";
        _targetDirectory = _g.targetDirectory + _g.dataAccessPartition + "\\";

        if (!Directory.Exists(_targetDirectory))
        {
            Directory.CreateDirectory(_targetDirectory);
        }
    }

    public void GenerateDataAccessBoilerplate()
    {
        //csproj
        GeneratorHelper.CopyRenameFile(_templateDirectory, _targetDirectory, "Project.csproj", _g.dataAccessPartition + ".csproj");

        //DataDependency
        StringBuilder repoInjections = new();
        foreach (string repo in _g.repositoryNames)
        {
            repoInjections.Append($"\r\n        services.AddScoped<I{repo}, {repo}>();");
        }

        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory,
            "DataDependency.txt",
            _g.names.DataDependencyInjectName + ".cs",
            [
                ("dataAccess",_g.dataAccessPartition),
                ("dbContextName",_g.names.DbContextName),
                ("dataDependencyInjectName",_g.names.DataDependencyInjectName),
                ("repoInjections",repoInjections.ToString())
            ]
        );

        //Db
        StringBuilder DbSets = new();
        for(int i = 0; i < _g.tables.Count; i++)
        {
            DbSets.Append("\r\n    //Comment\r\n    public DbSet<" + _g.tables[i].ModelName +"> "+ _g.dbSetNames[i] +" { get; set; }");
        }
        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory,
            "DbContext.txt",
            _g.names.DbContextName + ".cs",
            [
                ("dataAccess",_g.dataAccessPartition),
                ("dbContextName",_g.names.DbContextName),
                ("dbSets",DbSets.ToString())
            ]
        );

    }
    
    public void GenerateModel(Table table)
    {
        //Model
        var properties = new StringBuilder();

        foreach (var col in table.Properties)
        {
            if (col.IsRequired) properties.AppendLine("    [Required]"); //Auto add error message depending on options

            //Option to infer type
            if (_g.config.InferType)
            {
                if (col.DatabaseName.ToLower().Contains("email")) { properties.AppendLine("    [EmailAddress]"); GeneratorHelper.Warn($"In table {table.ModelName} column {col.DatabaseName} is thought to be an Email Address."); }
                else if (col.DatabaseName.ToLower().Contains("phone")) {properties.AppendLine("    [Phone]"); GeneratorHelper.Warn($"In table {table.ModelName} column {col.DatabaseName} is thought to be an Phone Number."); }
            }
             

            if (col.Length.HasValue)
                properties.AppendLine($"    [MaxLength({col.Length.Value})]");

            if (col.IsPrimaryKey)
                properties.AppendLine($"    [Key]");

            properties.AppendLine($"    public {col.CSharpType} {col.DatabaseName} {{ get; set; }}\r\n");
        }

        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory + "Model\\",
            "Model.txt",
            $"{table.ModelName}.cs",
            [
                ("dataAccess", _g.dataAccessPartition),
                ("databaseName", table.ModelName != table.DatabaseName ? $"\r\n[Table(\"{table.ModelName}\")]" : ""),
                ("modelName", table.ModelName),
                ("properties", properties.ToString())
            ]
        );
    }

    public void GenerateRepository(Table table)
    {
        //Interface
        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory + "Repository\\",
            "IModelRepository.txt",
            $"I{table.ModelName}Repository.cs",
            [
                ("dataAccess", _g.dataAccessPartition),
                ("modelName", table.ModelName),
                ("CRUD", xxx ? GenerateRepositoryInterfaceCRUD(table) : "   "),
            ]
        );

        //Class
        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory + "Repository\\",
            "ModelRepository.txt",
            $"{table.ModelName}Repository.cs", // Replace name
            [
                ("dataAccess", _g.dataAccessPartition),
                ("modelName", table.ModelName),
                ("dbContextName", _g.names.DbContextName),
                ("dbSetName", _g.dbSetNames[tableIndex]),
                ("CRUD", xxx ? GenerateRepositoryClassCRUD(table) : ""),
            ]
        );
    }

    public void GenerateAuthRepository()
    {
        //Interface
        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory + "Repository\\",
            "IAuthRepository.txt",
            $"IAuthRepository.cs",
            [
                ("dataAccess", _g.dataAccessPartition),
                ("authType", _g.config.AuthType),
                ("identifier", _g.config.AuthIdentifer),
                ("smallIdentifer", _g.config.AuthIdentifer.ToLower())
            ]
        );

        //class
        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory + "Repository\\",
            "AuthRepository.txt",
            $"AuthRepository.cs",
            [
                ("dataAccess", _g.dataAccessPartition),
                ("authType", _g.config.AuthType),
                ("dbContextName", _g.names.DbContextName),
                ("identifer", _g.config.AuthIdentifer),
                ("smallIdentifer", _g.config.AuthIdentifer.ToLower()),
                ("dbSetName", _g.config.AuthDbSetName)
            ]
        );
    }

    private string GenerateRepositoryClassCRUD(Table table)
    {
        StringBuilder crud = new StringBuilder();
        string comment = "\r\n   /// <inheritdoc />\r\n";

        crud.Append(
            comment +
            $"   public {table.ModelName}? Get{table.ModelName}ById(int id)\r\n" +
            "   {\r\n" +
            $"       return _db.{_g.dbSetNames[tableIndex]}.Find(id);\r\n" +
            "   }\r\n");
        crud.Append(
            comment +
            $"   public IEnumerable<{table.ModelName}> GetAll{table.ModelName}s()\r\n" +
            "   {\r\n" +
            $"       return _db.{_g.dbSetNames[tableIndex]}.ToList();\r\n" +
            "   }\r\n");
        crud.Append(
            comment +
            $"   public void Add{table.ModelName}({table.ModelName} new{table.ModelName})\r\n" +
            "   {\r\n" +
            $"       _db.{_g.dbSetNames[tableIndex]}.Add(new{table.ModelName});\r\n" +
            "       _db.SaveChanges();\r\n" +
            "   }\r\n");
        crud.Append(
            comment +
            $"   public void Update{table.ModelName}({table.ModelName} update{table.ModelName})\r\n" +
            "   {\r\n" +
            $"       _db.{_g.dbSetNames[tableIndex]}.Update(update{table.ModelName});\r\n" +
            "       _db.SaveChanges();\r\n" +
            "   }\r\n");
        crud.Append(
            comment +
            $"   public void Delete{table.ModelName}({table.ModelName} remove{table.ModelName})\r\n" +
            "   {\r\n" +
            $"       _db.{_g.dbSetNames[tableIndex]}.Remove(remove{table.ModelName});\r\n" +
            "       _db.SaveChanges();\r\n" +
            "   }\r\n");

        return crud.ToString();
    }

    private string GenerateRepositoryInterfaceCRUD(Table table)
    {
        StringBuilder crud = new StringBuilder();

        crud.Append(
            "   /// <summary>\r\n" +
            $"   /// Gets the {table.ModelName} with a specifed Id.\r\n" +
            "   /// </summary>\r\n" +
            $"   /// <param name=\"id\"> The Id of the {table.ModelName} to get. </param>\r\n" +
            $"   /// <returns> The {table.ModelName} model with the specified Id. </returns>\r\n" +
            $"   public {table.ModelName}? Get{table.ModelName}ById(int id);\r\n");
        crud.Append(
            "   /// <summary>\r\n" +
            $"   /// Gets all {table.ModelName}s.\r\n" +
            "   /// </summary>\r\n" +
            $"   /// <returns> A list of {table.ModelName} models. </returns>\r\n" +
            $"   public IEnumerable<{table.ModelName}> GetAll{table.ModelName}s();\r\n");
        crud.Append(
            "   /// <summary>\r\n" +
            $"   /// Adds the given model to the Database.\r\n" +
            "   /// </summary>\r\n" +
            $"   /// <param name=\"new{table.ModelName}\"> The {table.ModelName} to get added. </param>\r\n" +
            $"   public void Add{table.ModelName}({table.ModelName} new{table.ModelName});\r\n");
        crud.Append(
            "   /// <summary>\r\n" +
            $"   /// Updates the given {table.ModelName} with its new values.\r\n" +
            "   /// </summary>\r\n" +
            $"   /// <param name=\"update{table.ModelName}\"> The {table.ModelName} containing the updated values. </param>\r\n" +
            $"   public void Update{table.ModelName}({table.ModelName} update{table.ModelName});\r\n");
        crud.Append(
            "   /// <summary>\r\n" +
            $"   /// Deletes the given {table.ModelName}.\r\n" +
            "   /// </summary>\r\n" +
            $"   /// <param name=\"remove{table.ModelName}\"> The {table.ModelName} to remove. </param>\r\n" +
            $"   public void Delete{table.ModelName}({table.ModelName} remove{table.ModelName});\r\n");

        return crud.ToString();
    }
}