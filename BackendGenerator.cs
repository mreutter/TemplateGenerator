using BackendTemplateCreator.Generator;
using BackendTemplateCreator.Configuration;
using System.Text.Json;

namespace BackendTemplateCreator;
public class BackendGenerator
{
    //File Paths
    public string templateDirectory = @"..\..\..\BoilerplateReference\";
    private string _configPath = @"..\..\..\Config\";

    private string _configFile = "config.json";
    private string _namesFile = "names.json";
    private string _tablesFile = "tables.json";
    private string _SQLFile = "file.sql";

    public string targetDirectory = string.Empty;

    //References
    public Config? config;
    public Names? names;
    public List<Table> tables = new();

    /*private APIGenerator _apiGen;
    private LogicGenerator _logicGen;
    private DataAccessGenerator _dataAccessGen;*/

    //Generated Names
    public string APIPartition;
    public string logicPartition;
    public string dataAccessPartition;

    public string[] dtoNames;
    public string[] modelNames;
    public string[] dbSetNames;
    public string[] repositoryNames;
    public string[] serviceNames;
    public string[] controllerNames;

    public void Greet()
    {
        GeneratorHelper.WriteLine(File.ReadAllText(_configPath + "greet.txt"), "bold");

        GeneratorHelper.Info("While generating the process inform about places which need checking / correction from your side.");
        GeneratorHelper.Info("Copy the Template first before modifying, or it will be overwritten by subsequent generation.");
        GeneratorHelper.Info("Depending on input results might not work as intended.");
    }
    
    public void ReadConfig()
    {
        if (!Path.Exists(_configPath + _configFile)) GeneratorHelper.Error("Config file not found.");
        if (!Path.Exists(_configPath + _namesFile)) GeneratorHelper.Error("Names file not found.");

        string config = File.ReadAllText(_configPath + _configFile);
        string names = File.ReadAllText(_configPath + _namesFile);

        this.config = JsonSerializer.Deserialize<Config>(config);
        this.names = JsonSerializer.Deserialize<Names>(names);

        if (this.config == null) GeneratorHelper.Error("Config file contains errors.");
        if (this.names == null) GeneratorHelper.Error("Names file contains errors.");
    }

    public void ReadSQLFile()
    {
        //Generate Tables from SQL File if exists.
        if (_SQLFile != string.Empty)
        {
            GeneratorHelper.Info("Generating Table from SQL");
            string sql = File.ReadAllText(_configPath + _SQLFile);
            tables = SQLParser.GenerateTablesFromSQL(sql);
        }
        else GeneratorHelper.Error("No SQL File supplied.");

        GeneratorHelper.Info("Successfully extracted tables from SQL File\n");
    }

    public void WriteTableJSON()
    {
        //Configure if tables.json created during this execution else skip.
        if (!File.Exists(_configPath + _tablesFile))
        {
            string tablesJSONFile = JsonSerializer.Serialize<List<Table>>(tables);
            File.WriteAllText(_configPath + _tablesFile, tablesJSONFile);
            GeneratorHelper.Admonition("You need to check the tables.json file. To continue press 'Enter'.");
            while (Console.ReadKey(false).KeyChar != '\r') { } //Wait until enter key is pressed
        }
        else GeneratorHelper.Warn("Generator has read tables from pre-existing tables.json file.");
    }

    public void ReadTableJSON()
    {
        string tablesJSONFile = File.ReadAllText(_configPath + _tablesFile);
        tables.Clear();
        tables = JsonSerializer.Deserialize<List<Table>>(tablesJSONFile);

        if (tables == null)
        {
            GeneratorHelper.Error("Cannot read from configured tables.json file. Delete it to generate a new one.");
        }
    }

    //Responsible for creating all names and ensuring they are consistent
    public void GenerateNames()
    {
        if (config.Directory == "") GeneratorHelper.Error("No target directory specified.");
        if (config.DbName == "") GeneratorHelper.Admonition("No database name specified.");

        //Generate Directories
        targetDirectory = config.Directory + "\\" + names.ProjectName + "\\";
        if (names.UseRootPrefix)
        {
            APIPartition = names.ProjectName + "." + names.APIPartitionName;
            logicPartition = names.ProjectName + "." + names.LogicPartitionName;
            dataAccessPartition = names.ProjectName + "." + names.DataAccessPartitionName;
        }

        //Generate Key
        if (config.Key == string.Empty)
        {
            config.Key = GeneratorHelper.GenerateKey(64);
            GeneratorHelper.Info("Generated Key.\n");
        }

        //Dont generate names if no tables exist
        int tablesCount = tables.Count;
        if (tablesCount == 0) return;

        dtoNames = new string[tablesCount];
        modelNames = new string[tablesCount];
        repositoryNames = new string[tablesCount];
        serviceNames = new string[tablesCount];
        controllerNames = new string[tablesCount];

        //Generate by keeping same order among lists -> avoids dictionairies & overhead
        for (int i = 0; i < tablesCount; i++)
        {
            if (tables[i].ModelName == "") tables[i].ModelName = tables[i].DatabaseName;
            dtoNames[i] = tables[i].ModelName + names.DtoSuffix;
            modelNames[i] = tables[i].ModelName + names.ModelSuffix;
            dbSetNames[i] = tables[i].ModelName + names.DbSetSuffix; // if is any other than "" need to use Table alias
            repositoryNames[i] = tables[i].ModelName + names.RepositorySuffix;
            serviceNames[i] = tables[i].ModelName + names.ServiceSuffix;
            controllerNames[i] = tables[i].ModelName + names.ControllerSuffix;
        }
    }

    public void Generate()
    {
        //Start Generation
        /*_dataAccessGen = new(this);
        _logicGen = new(this);
        _apiGen = new(this);*/

        GeneratorHelper.Info("Generating Boilerplate...");

        //sln
        GeneratorHelper.TemplateReplacer(
            templateDirectory,
            targetDirectory,
            "Project.sln",
            names.ProjectName + ".sln",
            parameters: new Dictionary<string, string> { { "api", APIPartition }, {"logic", logicPartition}, {"dataAccess", dataAccessPartition} }
        );
        /*
        //only apply auth if used

        //API
        GeneratorHelper.Info("Generating API Layer...");
        _apiGen.GenerateAPIBoilerplate();
        foreach(var table in tables) _apiGen.GenerateController(table);
        _apiGen.GenerateAuthController();

        //Logic
        GeneratorHelper.Info("Generating Logic Layer...");
        _logicGen.GenerateLogicBoilerplate();
        foreach (var table in tables) 
        {
            _logicGen.GenerateDto(table);
            _logicGen.GenerateService(table);
        }
        _logicGen.GenerateAuthService();
        _logicGen.GenerateAuthDto();

        //Data Access
        GeneratorHelper.Info("Generating DataAccess Layer...");
        _dataAccessGen.GenerateDataAccessBoilerplate();
        foreach (var table in tables)
        {
            _dataAccessGen.GenerateModel(table);
            _dataAccessGen.GenerateRepository(table);
        }
        _dataAccessGen.GenerateAuthRepository();
        */
    }
}
