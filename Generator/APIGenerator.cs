namespace BackendTemplateCreator.Generator;
public class APIGenerator
{
    private BackendGenerator _g;

    private string _targetDirectory;
    private string _templateDirectory;
    public APIGenerator(BackendGenerator backendGen)
    {
        _g = backendGen;

        _templateDirectory = _g.templateDirectory + "API\\";
        _targetDirectory = _g.targetDirectory + _g.APIPartition + "\\";

        if (!Directory.Exists(_targetDirectory))
        {
            Directory.CreateDirectory(_targetDirectory);
        }
    }

    public void GenerateAPIBoilerplate()
    {

        //appsettings.Development.json -> copy
        GeneratorHelper.CopyRenameFile(_templateDirectory, _targetDirectory, "appsettings.Development.json", "appsettings.Development.json");

        //Properties/launchsettings.json -> half change
        GeneratorHelper.CopyRenameFile(_templateDirectory, _targetDirectory + "Properties\\", "launchSettings.json", "launchSettings.json");

        //Project.user -> copy
        GeneratorHelper.CopyRenameFile(_templateDirectory, _targetDirectory, "Project.user", _g.APIPartition + ".user");

        //csproj -> change
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory,
            "Project.csproj",
            _g.APIPartition + ".csproj",
            parameters: new Dictionary<string, string>
            {
                { "logic",_g.logicPartition}
            },
            sections: new Dictionary<string, bool>
            {
                { "includeAuth", _g.config.UseAuthentification}
            }
        );

        //appsettings.json -> change
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory,
            "appsettings.json",
            "appsettings.json",
            sections: new Dictionary<string, bool>
            {
                {"useDBConnectionString", true },
                { "useAuthentication", _g.config.UseAuthentification}
            },
            sectionParameters: new Dictionary<string, Dictionary<string, string>>
            {
                {"useDBConnectionString", new Dictionary<string, string>
                    {
                    {"dbHost", _g.config.DbHost},
                    {"dbUser", _g.config.DbUser},
                    {"dbPassword", _g.config.DbPassword},
                    {"dbName", _g.config.DbName}
                    }
                },
                {"useAuthentication", new Dictionary<string, string>
                    {
                    {"key", _g.config.Key},
                    {"issuer", _g.config.Issuer},
                    {"audience", _g.config.Audience}
                    }
                }
            }
        );

        //.http -> change
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory,
            "Project.http",
            _g.APIPartition + ".http",
            parameters: new Dictionary<string, string> {{ "api", _g.APIPartition }}
        );

        //Program.cs
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory,
            "Program.txt",
            "Program.cs",
            parameters: new Dictionary<string, string> { { "logic", _g.logicPartition } },
            sections: new Dictionary<string, bool> 
            {
                {"includes",_g.config.UseAuthentification},
                { "declareAuthentication", _g.config.UseAuthentification },
                { "useAuthentication", _g.config.UseAuthentification},
                { "addAuthorisation", _g.config.UseAuthentification },
                { "useAuthorisation", _g.config.UseAuthorisation },
                { "enableSwaggerAuth", _g.config.UseAuthorisation }
            }
        );
    }

    //Roles and user verification from token 
    public void GenerateController(int tIndex)
    {
        Table table = _g.tables[tIndex];
        //Controller Folder
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "Controllers\\",
            "ModelController.txt",
            _g.controllerNames[tIndex] + ".cs",
            parameters: new Dictionary<string, string>
            {
                {"logic",_g.logicPartition},
                {"api",_g.APIPartition},
                {"controllerName",_g.names.ControllerSuffix == "Controller" ? _g.modelNames[tIndex] : _g.controllerNames[tIndex]},
                {"serviceName",_g.serviceNames[tIndex]},
                {"varServiceName",GeneratorHelper.ToLowerFirst(_g.serviceNames[tIndex])},
                {"modelName",_g.modelNames[tIndex]},
                {"varModelName",GeneratorHelper.ToLowerFirst(_g.modelNames[tIndex])},
                {"dtoName", _g.dtoNames[tIndex]}
            },
            sections: new Dictionary<string, bool>
            {
                {"useAuthorisation", _g.config.UseAuthorisation},
                {"comment", true}, //Use Comment?
                {"getById", table.UseGetById},
                {"getAll", table.UseGetAll},
                {"add", table.UseAdd},
                {"update", table.UseUpdate},
                {"delete", table.UseDelete}
            }
        );
    }

    public void GenerateAuthController()
    {
        GeneratorHelper.TemplateReplacer(
            _templateDirectory,
            _targetDirectory + "Controllers\\", //Custom controller directory
            "AuthController.txt",
            "AuthController.cs", //Custom auth controller name
            parameters: new Dictionary<string, string>
            {
                {"logic",_g.APIPartition },
                {"api",_g.APIPartition },
                { "identifier", _g.config.AuthIdentifer }
            }
        );
    }
}