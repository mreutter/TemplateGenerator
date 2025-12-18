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

    //Structure
    public bool UseAbstractFolder { get; set; }
    public bool UseSeperatedDTOFolders { get; set; }

    //SQL Generation
    public bool InferType { get; set; }
    public bool UseForeignKey { get; set; } //Establishes the relationship in Model -> can be changed in tables.json

    //Configuration
    public bool UseComments { get; set; }
    public bool UseControllerName { get; set; }
    public bool UseShared { get; set; } //Not implemented but easily doable
    public bool UseAsync { get; set; } //Not implemented

    public string CORSAllowIP { get; set; }


    //Auth
    public bool UseAuthentification { get; set; }
    public bool UseAuthorisation { get; set; }
    public string AuthType { get; set; }
    public string AuthIdentifer { get; set; }
    public string AuthDbSetName { get; set; }

    //Auth
    public string Key { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
}
