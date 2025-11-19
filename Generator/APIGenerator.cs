using System.Text;

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
        string includeAuth = "\r\n    <PackageReference Include=\"Microsoft.AspNetCore.Authentication.JwtBearer\" Version=\"8.0.21\" />";
        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory,
            "Project.csproj",
            _g.APIPartition + ".csproj",
            [
                ("includeAuth",_g.config.UseAuthentification ? includeAuth : ""),
                ("logic",_g.APIPartition)
            ]
        );

        //appsettings.json -> change
        string connectionString = $"server={_g.config.DbHost};uid={_g.config.DbUser};pwd={_g.config.DbPassword};database={_g.config.DbName};";
        string UseDbConnectionString = ",\r\n    \"ConnectionStrings\": {\r\n        \"DefaultConnection\": \"" + connectionString +"\"\r\n    }";
        string UseJWT = ",\r\n    \"Jwt\": {\r\n        \"Key\": \"" + _g.config.Key + "\",\r\n        \"Issuer\": \"" + _g.config.Issuer + "\",\r\n        \"Audience\": \"" + _g.config.Audience + "\"\r\n    }";
        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory,
            "appsettings.json",
            "appsettings.json",
            [
                ("useDbConnectionString", UseDbConnectionString),
                ("useAuth",_g.config.UseAuthentification ? UseJWT : "")
            ]
        );

        //.http -> change
        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory,
            "Project.http",
            _g.APIPartition + ".http",
            [("api",_g.APIPartition)]
        );

        //Program.cs
        string includes = "using Microsoft.AspNetCore.Authentication.JwtBearer;\r\nusing Microsoft.IdentityModel.Tokens;\r\nusing System.Text;\r\nusing Microsoft.OpenApi.Models;\r\n";

        string auth =
@"
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Hier konfigurieren wir, WIE ein Token validiert werden soll
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        // Die Werte werden aus der appsettings.json gelesen
        ValidIssuer = builder.Configuration[""Jwt:Issuer""],
        ValidAudience = builder.Configuration[""Jwt:Audience""],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration[""Jwt:Key""]
                ?? throw new InvalidOperationException(""JWT Key is not configured."")))
    };
});
";
        string buildAddAuth = "\r\nbuilder.Services.AddAuthorization();\r\n";
        string enableSwaggerAuth = @"options =>
{
    options.AddSecurityDefinition(""Bearer"", new OpenApiSecurityScheme
    {
        Name = ""Authorization"",
        Description = ""JWT Authorization header using the Bearer scheme. Enter 'Bearer {token}'"",
        Type = SecuritySchemeType.Http,
        Scheme = ""Bearer"",
        BearerFormat = ""JWT"",
        In = ParameterLocation.Header
    });

    // Make Swagger UI use this security definition
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = ""Bearer""
                }
            },
            new string[] {} // An empty array (no specific scopes)
        }
    });
}";
        string useAuth = "\r\napp.UseAuthentication();\r\n";
        string useAuthorisation = "app.UseAuthorization();\r\n";

        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory,
            "Program.txt",
            "Program.cs",
            [
                ("includes",includes),
                ("logic",_g.APIPartition),
                ("auth",auth),
                ("addAuth",buildAddAuth),
                ("useAuth",useAuth),
                ("useAuthorisation",useAuthorisation),
                ("enableSwaggerAuth", enableSwaggerAuth)
            ]
        );
    }

    public void GenerateController(Table table)
    {
        //Controller Folder

        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory + "Controllers\\",
            "ModelController.txt",
            table.ModelName + "Controller.cs",
            [
                ("logic",_g.APIPartition),
                ("api",_g.APIPartition),
                ("useAuthorisation",_g.config.UseAuthorisation ? "\r\n[Authorize]" : ""),
                ("modelName",table.ModelName),
                ("smallModelName",table.ModelName.ToLower()),
                ("CRUD", xxx ? "\r\n" + GenerateControllerCRUD(table) : "")
            ]
        );
    }

    public void GenerateAuthController()
    {
        GeneratorHelper.StitchReplaceFile(
            _templateDirectory,
            _targetDirectory + "Controllers\\",
            "AuthController.txt",
            "AuthController.cs",
            [
                ("logic",_g.APIPartition),
                ("api",_g.APIPartition),
                ("identifier", _g.config.AuthIdentifer)
            ]
        );
    }

    private string GenerateControllerCRUD(Table table)
    {
        StringBuilder crud = new StringBuilder();
        string modelName = table.ModelName;
        string smallModelName = table.ModelName.ToLower();

        crud.Append(
"    [HttpGet(\"{id}\")]\r\n" +
$"    public ActionResult<{modelName}Dto?> Get{modelName}ById(int id)\r\n" +
"    {\r\n" +
$"        {modelName}Dto? {smallModelName}Dto = _{smallModelName}Service.Get{modelName}ById(id);\r\n" +
$"        return {smallModelName}Dto == null ? NotFound() : Ok({smallModelName}Dto);\r\n" +
"    }\r\n");

        crud.Append(
"    [HttpGet]\r\n" +
$"    public ActionResult<IEnumerable<{modelName}Dto>> GetAll{modelName}s()\r\n" +
"    {\r\n" +
$"        return Ok(_{smallModelName}Service.GetAll{modelName}s());\r\n" +
"    }\r\n");

        crud.Append(
"    [HttpPost]\r\n" +
$"    public ActionResult Add{modelName}(Create{modelName}Dto {smallModelName})\r\n" +
"    {\r\n" +
$"        int id = _{smallModelName}Service.Add{modelName}({smallModelName});\r\n" +
$"        return CreatedAtAction(nameof(Get{modelName}ById), new {{ id }}, id);\r\n" +
"    }\r\n");

        crud.Append(
"    [HttpPut]\r\n" +
$"    public ActionResult Update{modelName}(int id, Update{modelName}Dto {smallModelName})\r\n" +
"    {\r\n" +
$"        return _{smallModelName}Service.Update{modelName}(id, {smallModelName}) ? NoContent() : NotFound();\r\n" +
"    }\r\n");

        crud.Append(
"    [HttpDelete]\r\n" +
$"    public ActionResult Delete{modelName}(int id)\r\n" +
"    {\r\n" +
$"        return _{smallModelName}Service.Delete{modelName}(id) ? NoContent() : NotFound();\r\n" +
"    }\r\n");

        return crud.ToString();
    }
}