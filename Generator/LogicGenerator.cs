using Spectre.Console;
using System.Text;

namespace BackendTemplateCreator.Generator;
public class LogicGenerator
{
    private BackendGenerator _g;

    private string _targetDirectory;
    private string _templateDirectory;
    public LogicGenerator(BackendGenerator backendGen)
    {
        _g = backendGen;

        _templateDirectory = _g.templateDirectory + "Logic\\";
        _targetDirectory = _g.targetDirectory + _g.logicPartition + "\\";

        if (!Directory.Exists(_targetDirectory))
        {
            Directory.CreateDirectory(_targetDirectory);
        }
    }

    public void GenerateLogicBoilerplate()
    {
        //csproj change
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory,
            "Project.csproj",
            _g.logicPartition,
            parameters: new Dictionary<string, string>
            {
                {"dataAccess", _g.dataAccessPartition}
            },
            sections: new Dictionary<string, bool>
            {
                {"includeJWT", _g.config.UseAuthentification}
            }
        );

        Dictionary<string, string>[] serviceInjections = new Dictionary<string, string>[_g.serviceNames.Length];
        for (int i = 0; i < _g.serviceNames.Length; i++)
        {
            serviceInjections[i] = new Dictionary<string, string> { { "service", _g.serviceNames[i] } };
        }

        //dependency
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory,
            "LogicDependency.txt",
            _g.names.LogicDependencyInjectName + ".cs",
            parameters: new Dictionary<string, string>
            {
                { "dataAccess", _g.dataAccessPartition},
                {"logic", _g.logicPartition},
                {"logicDependencyInjectName", _g.names.LogicDependencyInjectName},
            },
            multipleSectionParameters: new Dictionary<string, Dictionary<string, string>[]>
            {
                { "serviceInjections", serviceInjections }
            }
        );
    }
    public void GenerateService(int tIndex)
    {
        Table table = _g.tables[tIndex];
        //Interface
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "Service\\",
            "IModelService.txt",
            $"I{_g.serviceNames[tIndex]}.cs",
            parameters: new Dictionary<string, string>
            {
                {"logic", _g.logicPartition },
                {"serviceName", _g.serviceNames[tIndex]},
                {"modelName", _g.modelNames[tIndex]},
                {"dtoName", _g.dtoNames[tIndex]}
            },
            sections: new Dictionary<string, bool>
            {
                { "commentGetById", true },
                { "commentGetAll", true },
                { "commentAdd", true },
                { "commentUpdate", true },
                { "commentDelete", true },

                { "getById", table.UseGetById },
                { "getAll", table.UseGetAll },
                { "add", table.UseAdd },
                { "update", table.UseUpdate },
                { "delete", table.UseDelete },
            }
        );
        //Service
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "Service\\",
            "ModelService.txt",
            $"{_g.serviceNames[tIndex]}.cs",
            parameters: new Dictionary<string, string>
            {
                { "dataAccess", _g.dataAccessPartition },
                { "logic", _g.logicPartition },
                { "serviceName", _g.serviceNames[tIndex]},
                { "modelName", _g.modelNames[tIndex]},
                { "dtoName", _g.dtoNames[tIndex]},
                { "repoName", _g.repositoryNames[tIndex]},
                { "varRepoName", GeneratorHelper.ToLowerFirst(_g.repositoryNames[tIndex])},
                { "varModelName", GeneratorHelper.ToLowerFirst(_g.modelNames[tIndex])}
            },
            sections: new Dictionary<string, bool>
            {
                { "comment", true },

                { "getById", table.UseGetById },
                { "getAll", table.UseGetAll },
                { "add", table.UseAdd },
                { "update", table.UseUpdate },
                { "delete", table.UseDelete },
            }
        );
    }
    public void GenerateDto(int tIndex)
    {
        Table table = _g.tables[tIndex];
        //Dto
        var dataAnnotationsSB = new StringBuilder();
        string[] dataAnnotations = new string[table.Properties.Count];
        for (int i = 0; i < table.Properties.Count; i++)
        {
            Property col = table.Properties[i];

            if (!col.IsDtoProperty) continue;

            if (col.DatabaseName != col.ModelName) dataAnnotationsSB.AppendLine($"    [Column({col.DatabaseName})]");

            if (col.IsRequired) dataAnnotationsSB.AppendLine("    [Required]"); //Auto add error message depending on options

            //Option to infer type
            if (_g.config.InferType)
            {
                if (col.DatabaseName.ToLower().Contains("email")) { dataAnnotationsSB.AppendLine("    [EmailAddress]"); GeneratorHelper.Warn($"In table {table.ModelName} column {col.DatabaseName} is thought to be an Email Address."); }
                else if (col.DatabaseName.ToLower().Contains("phone")) { dataAnnotationsSB.AppendLine("    [Phone]"); GeneratorHelper.Warn($"In table {table.ModelName} column {col.DatabaseName} is thought to be an Phone Number."); }
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
            _targetDirectory + "DTO\\",
            "ModelDto.txt",
            $"{_g.dtoNames[tIndex]}.cs",
            parameters: new Dictionary<string, string>
            {
                {"logic", _g.logicPartition},
                {"dtoName", _g.dtoNames[tIndex]},
                {"properties", properties.ToString()}
            }, multipleSectionParameters: new Dictionary<string, Dictionary<string, string>[]>
            {
                {"properties", properties}
            }
        );

        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "DTO\\",
            "ModelDto.txt",
            $"Create{_g.dtoNames[tIndex]}.cs",
            parameters: new Dictionary<string, string>
            {
                {"logic", _g.logicPartition},
                {"dtoName", "Create"+_g.dtoNames[tIndex]},
                {"properties", properties.ToString()}
            }, multipleSectionParameters: new Dictionary<string, Dictionary<string, string>[]>
            {
                {"properties", properties}
            }
        );

        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "DTO\\",
            "ModelDto.txt",
            $"Update{_g.dtoNames[tIndex]}.cs",
            parameters: new Dictionary<string, string>
            {
                {"logic", _g.logicPartition},
                {"dtoName", "Update"+_g.dtoNames[tIndex]},
                {"properties", properties.ToString()}
            }, multipleSectionParameters: new Dictionary<string, Dictionary<string, string>[]>
            {
                {"properties", properties}
            }
        );
    }

    public void GenerateAuthService()
    {
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "Service\\",
            "IAuthService.txt",
            "IAuthService.cs",
            parameters: new Dictionary<string, string>
            {
                {"logic", _g.logicPartition },
                { "identifier", _g.config.AuthIdentifer }
            }
        );
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "Service\\",
            "AuthService.txt",
            "AuthService.cs",
            parameters: new Dictionary<string, string>
            {
                {"dataAccess", _g.dataAccessPartition},
                {"logic", _g.logicPartition},
                {"authType", _g.config.AuthType},
                {"varAuthType", GeneratorHelper.ToLowerFirst(_g.config.AuthType)},
                {"identifier", _g.config.AuthIdentifer}
                //Claims
                //Multiple Credentials
                //Custom DTOs
            }
        );
    }
    public void GenerateAuthDto() //Better accomodation for future auth types (Also 2FA)
    {
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "DTO\\",
            "LoginRequestDto.txt",
            "LoginRequestDto.cs",
            parameters: new Dictionary<string, string>
            {
                { "logic", _g.logicPartition },
                {"authProperties", "public string " + _g.config.AuthIdentifer + " { get; set; }"} //Custom multipe credentials
            }
        );
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "DTO\\",
            "LoginResponseDto.txt",
            "LoginResponseDto.cs",
            parameters: new Dictionary<string, string>
            {
                { "logic", _g.logicPartition }
            }
        );
    }
}