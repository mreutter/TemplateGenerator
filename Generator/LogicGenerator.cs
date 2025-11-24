/*using System.Text;

namespace BackendTemplateCreator.Generator;
public class LogicGenerator
{
    private BackendGenerator _g;

    private string _targetDirectory;
    private string _templateDirectory;

    private Dictionary<string, string> _DtoNames = new();
    public LogicGenerator(BackendGenerator backendGen)
    {
        _g = backendGen;

        _templateDirectory = _g.templateDirectory + "Logic\\";
        _targetDirectory = _g.targetDirectory + _g.logicPartition + "\\";

        if (!Directory.Exists(_targetDirectory)){
            Directory.CreateDirectory(_targetDirectory);
        }
    }

    public void GenerateLogicBoilerplate()
    {
        //csproj change
        string includeJWT = "\r\n    <PackageReference Include=\"System.IdentityModel.Tokens.Jwt\" Version=\"8.14.0\" />";
        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory,
            "Project.csproj",
            _g.logicPartition,
            [("includeJWT",_g.config.UseAuthentification ?  includeJWT : ""), ("dataAccess",_g.dataAccessPartition)]
        );

        StringBuilder serviceInjections = new();
        foreach (Table table in _g.tables) //-----------------------------------------------------------------------------------------------Utilize Nameing
        {
            serviceInjections.Append($"\r\n        services.AddScoped<I{table.ModelName}Service, {table.ModelName}Service>();");
        }

        //dependency
        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory,
            "LogicDependency.txt",
            _g.names.LogicDependencyInjectName + ".cs",
            [
                ("dataAccess", _g.dataAccessPartition),
                ("logic", _g.logicPartition),
                ("logicDependencyInjectName", _g.names.LogicDependencyInjectName),
                ("serviceInjections", serviceInjections.ToString())
            ]
        );
    }
    public void GenerateService(Table table)
    {
        //Interface
        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory + "Service\\",
            "IModelService.txt",
            $"I{table.ModelName}Service.cs",
            [
                ("logic", _g.logicPartition),
                ("modelName", table.ModelName),
                ("CRUD", xxx ? GenerateServiceInterfaceCRUD(table) : "   ")
            ]
        );
        //Service
        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory + "Service\\",
            "ModelService.txt",
            $"{table.ModelName}Service.cs",
            [
                ("logic", _g.logicPartition),
                ("dataAccess", _g.dataAccessPartition),
                ("modelName", table.ModelName),
                ("smallModelName", table.ModelName.ToLower()),
                ("CRUD", xxx ? "\r\n" +GenerateServiceClassCRUD(table) : "")
            ]
        );
    }
    public void GenerateDto(Table table)
    {
        //Dto
        var properties = new StringBuilder();

        foreach (var col in table.Properties)
        {
            if (!col.IsDtoProperty) continue;

            if (col.IsRequired) properties.AppendLine("    [Required]"); //Auto add error message depending on options

            if (_g.config.InferType)//Multiple DataAnnotations in one by [,]
            {
                if (col.DatabaseName.ToLower().Contains("email")) properties.AppendLine("    [EmailAddress]");
                else if (col.DatabaseName.ToLower().Contains("phone")) properties.AppendLine("    [Phone]");
            }

            if (col.Length.HasValue)
                properties.AppendLine($"    [MaxLength({col.Length.Value})]");

            if (col.IsPrimaryKey)
                properties.AppendLine($"    [Key]");

            properties.AppendLine($"    public {col.CSharpType} {col.DatabaseName} {{ get; set; }}\r\n");
        }

        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory + "DTO\\",
            "ModelDto.txt",
            $"{_DtoNames[table.ModelName]}.cs",
            [
                ("logic", _g.logicPartition),
                ("dtoName", _DtoNames[table.ModelName]),
                ("properties", properties.ToString())
            ]
        );

        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory + "DTO\\",
            "ModelDto.txt",
            $"Update{_DtoNames[table.ModelName]}.cs",
            [
                ("logic", _g.logicPartition),
                ("dtoName", "Update"+_DtoNames[table.ModelName]),
                ("properties", properties.ToString())
            ]
        );

        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory + "DTO\\",
            "ModelDto.txt",
            $"Create{_DtoNames[table.ModelName]}.cs",
            [
                ("logic", _g.logicPartition),
                ("dtoName", "Create"+_DtoNames[table.ModelName]),
                ("properties", properties.ToString())
            ]
        );
    }

    public void GenerateAuthService() 
    {
        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory + "Service\\",
            "IAuthService.txt",
            "IAuthService.cs",
            [
                ("logic", _g.logicPartition),
                ("identifier", _g.config.AuthIdentifer)
            ]
        );
        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory + "Service\\",
            "AuthService.txt",
            "AuthService.cs",
            [
                ("dataAccess", _g.dataAccessPartition),
                ("logic", _g.logicPartition),
                ("authType", _g.config.AuthType),
                ("smallAuthType", _g.config.AuthType.ToLower()),
                ("identifier", _g.config.AuthIdentifer),
                ("smallIdentifier", _g.config.AuthIdentifer.ToLower())
            ]
        );
    }
    public void GenerateAuthDto() {
        *//*[Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }*//*

        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory + "DTO\\",
            "LoginRequestDto.txt",
            "LoginRequestDto.cs",
            [
                ("logic", _g.logicPartition),
                ("authProperties", "public string " + _g.config.AuthIdentifer + " { get; set; }")
            ]
        );
        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory + "DTO\\",
            "LoginResponseDto.txt",
            "LoginResponseDto.cs",
            [
                ("logic", _g.logicPartition)
            ]
        );
    }


    private string GenerateServiceClassCRUD(Table table)
    {
        StringBuilder crud = new StringBuilder();
        string comment = "\r\n   /// <inheritdoc />\r\n";

        string modelName = table.ModelName;
        string smallModelName = table.ModelName.ToLower();

        crud.Append(
        comment +
$"   public {modelName}Dto? Get{modelName}ById(int id)\r\n" +
"   {\r\n" +
$"       {modelName}? {smallModelName} = _{smallModelName}Repository.Get{modelName}ById(id);\r\n" +
$"       if ({smallModelName} == null) return null;\r\n" +
$"       {modelName}Dto {smallModelName}Dto = ToDto({smallModelName});\r\n" +
$"       return {smallModelName}Dto;\r\n" +
"   }\r\n");
        crud.Append(
        comment +
$"   public IEnumerable<{modelName}Dto> GetAll{modelName}s()\r\n" +
"   {\r\n" +
$"       return _{smallModelName}Repository.GetAll{modelName}s().Select(x => ToDto(x));\r\n" +
"   }\r\n");
        crud.Append(
        comment +
$"    public int Add{modelName}(Create{modelName}Dto {smallModelName})\r\n" +
"    {\r\n" +
$"        {modelName} created{modelName} = ToModel({smallModelName});\r\n" +
$"        _{smallModelName}Repository.Add{modelName}(created{modelName});\r\n" +
$"        return created{modelName}.Id;\r\n" +
"    }\r\n");
        crud.Append(
        comment +
$"   public bool Update{modelName}(int id, Update{modelName}Dto {smallModelName})\r\n" +
"   {\r\n" +
$"       {modelName} {smallModelName}ToUpdate = _{smallModelName}Repository.Get{modelName}ById(id);\r\n" +
$"       if ({smallModelName}ToUpdate == null) return false;\r\n" +
$"       //Conversion\r\n" +
$"       _{smallModelName}Repository.Update{modelName}({smallModelName}ToUpdate);\r\n" +
$"       return true;\r\n" +
"   }\r\n");
        crud.Append(
        comment +
$"    public bool Delete{modelName}(int id)\r\n" +
"    {\r\n" +
$"        {modelName} {smallModelName} = _{smallModelName}Repository.Get{modelName}ById(id);\r\n" +
$"        if ({smallModelName} == null) return false;\r\n" +
$"        _{smallModelName}Repository.Delete{modelName}({smallModelName});\r\n" +
$"        return true;\r\n" +
"    }\r\n");


        return crud.ToString();
    }

    private string GenerateServiceInterfaceCRUD(Table table)
    {
        StringBuilder crud = new StringBuilder();

        string modelName = table.ModelName;

        crud.Append(
$"    /// <summary>\r\n"+
$"    /// Gets the {modelName} with the given id.\r\n"+
$"    /// </summary>\r\n"+
$"    /// <param name=\"id\"> The id of the looked up {modelName}.</param>\r\n"+
$"    /// <returns>The {modelName}Dto of the Model with the specified id.</returns>\r\n"+
$"    {modelName}Dto? Get{modelName}ById(int id);\r\n");
        crud.Append(
$"    /// <summary>\r\n" +
$"    /// Gets all {modelName}s.\r\n" +
$"    /// </summary>\r\n" +
$"    /// <returns> A list of all {modelName}Dtos.</returns>\r\n" +
$"    IEnumerable <{modelName}Dto> GetAll{modelName}s();\r\n");
        crud.Append(
$"    /// <summary>\r\n" +
$"    /// Adds a {modelName} to the database via the Create{modelName}Dto.\r\n"+
$"    /// </summary>\r\n"+
$"    /// <param name=\"Create{modelName}Dto\">A Dto to be added to the database. </param>\r\n"+
$"    /// <returns> The id of the newly created {modelName}. </returns>\r\n"+
$"    int Add{modelName}(Create{modelName}Dto {modelName.ToLower()});\r\n");
        crud.Append(
$"    /// <summary>\r\n" +
$"    /// Updates a {modelName} of the database via the Update{modelName}Dto.\r\n" +
$"    /// </summary>\r\n" +
$"    /// <param name=\"Create{modelName}Dto\">The Dto to be changed in the database. </param>\r\n" +
$"    /// <returns> Wether or not the update was successful. </returns>\r\n" +
$"    bool Update{modelName}(int id, Update{modelName}Dto {modelName.ToLower()});\r\n");
        crud.Append(
$"    /// <summary>\r\n" +
$"    /// Deletes a {modelName} with the specified id.\r\n "+
$"    /// </summary>\r\n"+
$"    /// <param name=\"id\"> The id of the {modelName} to be deleted. </param>\r\n"+
$"    /// <returns> Wether or not the deletion was successful. </returns>\r\n"+
$"    bool Delete{modelName}(int id);\r\n");

        return crud.ToString();
    }
}*/