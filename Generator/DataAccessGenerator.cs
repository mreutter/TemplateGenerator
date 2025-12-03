using System.Text;

namespace BackendTemplateCreator.Generator;
public class DataAccessGenerator
{
    private BackendGenerator _g;

    //Local folder scoped directories
    private string _targetDirectory;
    private string _templateDirectory;

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
        Dictionary<string, string>[] repos = new Dictionary<string, string>[_g.repositoryNames.Length];
        for (int i = 0; i < _g.repositoryNames.Length; i++)
        {
            repos[i] = new Dictionary<string, string> { { "repo", _g.repositoryNames[i] } };
        }

        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory,
            "DataDependency.txt",
            _g.names.DataDependencyInjectName + ".cs",
            parameters: new Dictionary<string, string> 
            {
                {"dataAccess",_g.dataAccessPartition},
                {"dbContextName",_g.names.DbContextName},
                {"dataDependencyInjectName",_g.names.DataDependencyInjectName},
            },
            multipleSectionParameters: new Dictionary<string, Dictionary<string, string>[]>
            {
                {"repoInjections", repos}
            }
        );

        //Db
        Dictionary<string, string>[] DbSets = new Dictionary<string, string>[_g.tables.Count];
        for (int i = 0; i < _g.tables.Count; i++)
        {
            DbSets[i] = new Dictionary<string, string> 
            {
                { "modelName", _g.tables[i].ModelName }, 
                { "dbSetName", _g.tables[i].DatabaseName }
            };
        }
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory,
            "DbContext.txt",
            _g.names.DbContextName + ".cs",
            parameters: new Dictionary<string, string>
            {
                {"dataAccess",_g.dataAccessPartition },
                {"dbContextName",_g.names.DbContextName },
            },
            sections: new Dictionary<string, bool>
            {
                {"comment", true }
            },
            multipleSectionParameters: new Dictionary<string, Dictionary<string, string>[]>
            {
                { "dbSets", DbSets }
            }
        );

    }

    public void GenerateModel(int tIndex)
    {
        Table table = _g.tables[tIndex];

        //Model
        var dataAnnotationsSB = new StringBuilder();
        string[] dataAnnotations = new string[table.Properties.Count];
        for (int i = 0; i < table.Properties.Count; i++)
        {
            Property col = table.Properties[i];

            if (col.DatabaseName != col.ModelName) dataAnnotationsSB.AppendLine($"    [Column({col.DatabaseName})]");

            if (col.IsRequired) dataAnnotationsSB.AppendLine("    [Required]"); //Auto add error message depending on options

            //Option to infer type
            if (_g.config.InferType)
            {
                if (col.DatabaseName.ToLower().Contains("email")) { dataAnnotationsSB.AppendLine("    [EmailAddress]"); GeneratorHelper.Warn($"In table \"{table.ModelName}\" column \"{col.DatabaseName}\" is thought to be an Email Address."); }
                else if (col.DatabaseName.ToLower().Contains("phone")) { dataAnnotationsSB.AppendLine("    [Phone]"); GeneratorHelper.Warn($"In table \"{table.ModelName}\" column \"{col.DatabaseName}\" is thought to be a Phone Number."); }
            }

            if (col.Length.HasValue)
                dataAnnotationsSB.AppendLine($"    [MaxLength({col.Length.Value})]");

            if (col.IsPrimaryKey)
                dataAnnotationsSB.AppendLine($"    [Key]");
            // Also automatically convert fk 1:n (how to detect) relationships into collections

            dataAnnotations[i] = dataAnnotationsSB.ToString();
            dataAnnotationsSB.Clear();
        }

        Dictionary<string, string>[] properties = new Dictionary<string, string>[table.Properties.Count];
        for (int i = 0; i < table.Properties.Count; i++)
        {
            properties[i] = new Dictionary<string, string>
            {
                { "dataAnnotations", dataAnnotations[i] },
                { "type", table.Properties[i].CSharpType },
                { "modelName", table.Properties[i].ModelName }
            };
        }
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "Model\\",
            "Model.txt",
            $"{table.ModelName}.cs",
            parameters: new Dictionary<string, string>
            {
                {"dataAccess", _g.dataAccessPartition },
                { "modelName", table.ModelName},
                { "dbName", table.DatabaseName},
                { "properties", properties.ToString()}
            },
            sections: new Dictionary<string, bool>
            {
                {"databaseName", table.ModelName != table.DatabaseName }
            }
        );
    }

    public void GenerateRepository(int tIndex)
    {
        Table table = _g.tables[tIndex];
        //Interface
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "Repository\\",
            "IModelRepository.txt",
            $"I{_g.repositoryNames[tIndex]}.cs",
            parameters: new Dictionary<string, string>
            {
                {"dataAccess", _g.dataAccessPartition },
                { "repoName", _g.repositoryNames[tIndex]}
            },
            sections: new Dictionary<string, bool>
            {
                {"getByIdComment", true },
                {"getAllComment", true },
                {"addComment", true },
                {"updateComment", true },
                {"deleteComment", true },

                {"getById", table.UseGetById},
                {"getAll", table.UseGetAll},
                {"add", table.UseAdd},
                {"update", table.UseUpdate},
                {"delete", table.UseDelete}
            }
        );

        //Class
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "Repository\\",
            "ModelRepository.txt",
            $"{_g.repositoryNames[tIndex]}.cs", // Replace name
            parameters: new Dictionary<string, string>
            {
                {"dataAccess", _g.dataAccessPartition},
                {"repoName", _g.repositoryNames[tIndex]},
                {"dbContextName", _g.names.DbContextName},
                {"modelName", table.ModelName},
                {"dbSetName", _g.dbSetNames[tIndex]}
            },
            sections: new Dictionary<string, bool>
            {
                {"comment", true },
                {"getById", table.UseGetById},
                {"getAll", table.UseGetAll},
                {"add", table.UseAdd},
                {"update", table.UseUpdate},
                {"delete", table.UseDelete}
            }
        );
    }

    public void GenerateAuthRepository()
    {
        //Interface
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "Repository\\",
            "IAuthRepository.txt",
            "IAuthRepository.cs", //Consistent custom auth name
            parameters: new Dictionary<string, string>
            {
                {"dataAccess", _g.dataAccessPartition },
                {"authType", _g.config.AuthType},
                {"identifier", _g.config.AuthIdentifer},
                {"varIdentifer", GeneratorHelper.ToLowerFirst(_g.config.AuthIdentifer)}
            }
        );

        //class
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "Repository\\",
            "AuthRepository.txt",
            "AuthRepository.cs",
            parameters: new Dictionary<string, string>
            {
                {"dataAccess", _g.dataAccessPartition},
                {"authType", _g.config.AuthType},
                {"dbContextName", _g.names.DbContextName},
                {"identifer", _g.config.AuthIdentifer},
                {"varIdentifer", GeneratorHelper.ToLowerFirst(_g.config.AuthIdentifer)},
                {"dbSetName", _g.config.AuthDbSetName }
            }
        );
    }
}