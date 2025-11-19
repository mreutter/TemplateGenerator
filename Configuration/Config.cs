namespace BackendTemplateCreator.Configuration;

public class Config
{
    //File paths
    public string Directory { get; set; }

    //Datebase Info
    public string DbName { get; set; }
    public string DbHost { get; set; }
    public string DbUser { get; set; }
    public string DbPassword { get; set; }

    //SQL Generation
    public bool InferType { get; set; }
    public bool UseForeignKey { get; set; }

    //Configuration
    public bool UseControllerName { get; set; }
    public bool UseAuthentification { get; set; }
    public bool UseAuthorisation { get; set; }
    public bool UseShared { get; set; }
    public bool UseAsync { get; set; }

    public string CORSAllowIP { get; set; }


    //Single Identifer Auth
    public string AuthType { get; set; }
    public string AuthIdentifer { get; set; }
    public string AuthDbSetName { get; set; }

    //Auth
    public string Key { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
}
