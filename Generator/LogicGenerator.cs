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
            _g.logicPartition + ".csproj",
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
            _targetDirectory + "Service\\" + (_g.config.UseAbstractFolder ? _g.names.AbtractFolderName+"\\" : ""),
            "IModelService.txt",
            $"I{_g.serviceNames[tIndex]}.cs",
            parameters: new Dictionary<string, string>
            {
                {"logic", _g.logicPartition },
                {"serviceName", _g.serviceNames[tIndex]},
                {"modelName", _g.modelNames[tIndex]},
                {"dtoName", _g.dtoNames[tIndex]},
                {"varModelName", GeneratorHelper.ToLowerFirst(_g.modelNames[tIndex])}
            },
            sections: new Dictionary<string, bool>
            {
                { "commentGetById", _g.config.UseComments },
                { "commentGetAll", _g.config.UseComments },
                { "commentAdd", _g.config.UseComments },
                { "commentUpdate", _g.config.UseComments },
                { "commentDelete", _g.config.UseComments },

                { "getById", table.UseGetById },
                { "getAll", table.UseGetAll },
                { "add", table.UseAdd },
                { "update", table.UseUpdate },
                { "delete", table.UseDelete },
            }
        );
        //Service
        List<Dictionary<string, string>> dtoConversionLines = new();
        List<Dictionary<string, string>> modelConversionLines = new();
        for (int i = 0; i < table.Properties.Count; i++)
        {
            if (table.Properties[i].IsDtoProperty) dtoConversionLines.Add(new Dictionary<string, string> { { "property", table.Properties[i].ModelName } });

            if (table.Properties[i].IsDtoProperty && table.Properties[i].IsChangeableByDto) modelConversionLines.Add(new Dictionary<string, string> { { "property", table.Properties[i].ModelName } });
        }

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
                { "comment", _g.config.UseComments },

                { "getById", table.UseGetById },
                { "getAll", table.UseGetAll },
                { "add", table.UseAdd },
                { "update", table.UseUpdate },
                { "delete", table.UseDelete },
            }, multipleSectionParameters: new Dictionary<string, Dictionary<string, string>[]>
            {
                {"toDtoConversion", dtoConversionLines.ToArray()},
                {"toModelConversion", modelConversionLines.ToArray() }
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
            


            dataAnnotations[i] = dataAnnotationsSB.ToString();
            dataAnnotationsSB.Clear();
        }

        List<Dictionary<string, string>> properties = new List<Dictionary<string, string>>();
        List<Dictionary<string, string>> modifyProperties = new List<Dictionary<string, string>>();
        for (int i = 0; i < table.Properties.Count; i++)
        {
            Property property = table.Properties[i];
            var prop = new Dictionary<string, string>
            {
                { "dataAnnotations", dataAnnotations[i] },
                { "type", property.CSharpType },
                { "identifier", property.ModelName }
            };
            if (property.IsDtoProperty) properties.Add(prop);
            if (property.IsDtoProperty && property.IsChangeableByDto) modifyProperties.Add(prop);
        }


        Dictionary<string,string>[] propertiesArray = properties.ToArray();
        Dictionary<string, string>[] modifyPropertiesArray = modifyProperties.ToArray();

        string addedPath = (_g.config.UseSeperatedDTOFolders ? _g.modelNames[tIndex] + "\\" : "");

        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "DTO\\" + addedPath,
            "ModelDto.txt",
            $"{_g.dtoNames[tIndex]}.cs",
            parameters: new Dictionary<string, string>
            {
                {"logic", _g.logicPartition},
                {"dtoName", _g.dtoNames[tIndex]},
            }, multipleSectionParameters: new Dictionary<string, Dictionary<string, string>[]>
            {
                {"properties", propertiesArray}
            }
        );

        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "DTO\\" + addedPath,
            "ModelDto.txt",
            $"Create{_g.dtoNames[tIndex]}.cs",
            parameters: new Dictionary<string, string>
            {
                {"logic", _g.logicPartition},
                {"dtoName", "Create"+_g.dtoNames[tIndex]},
                {"properties", modifyProperties.ToString()}
            }, multipleSectionParameters: new Dictionary<string, Dictionary<string, string>[]>
            {
                {"properties", modifyPropertiesArray}
            }
        );

        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "DTO\\" + addedPath,
            "ModelDto.txt",
            $"Update{_g.dtoNames[tIndex]}.cs",
            parameters: new Dictionary<string, string>
            {
                {"logic", _g.logicPartition},
                {"dtoName", "Update"+_g.dtoNames[tIndex]},
                {"properties", modifyProperties.ToString()}
            }, multipleSectionParameters: new Dictionary<string, Dictionary<string, string>[]>
            {
                {"properties", modifyPropertiesArray}
            }
        );
    }

    public void GenerateAuthService()
    {
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "Service\\" + (_g.config.UseAbstractFolder ? _g.names.AbtractFolderName+"\\" : ""),
            "IAuthService.txt",
            "IAuthService.cs",
            parameters: new Dictionary<string, string>
            {
                {"logic", _g.logicPartition },
                {"identifier", _g.config.AuthIdentifer }
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
            _targetDirectory + "DTO\\" + (_g.config.UseSeperatedDTOFolders ? "Auth\\" : ""),
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
            _targetDirectory + "DTO\\" + (_g.config.UseSeperatedDTOFolders ? "Auth\\" : ""),
            "LoginResponseDto.txt",
            "LoginResponseDto.cs",
            parameters: new Dictionary<string, string>
            {
                { "logic", _g.logicPartition }
            }
        );
    }
}