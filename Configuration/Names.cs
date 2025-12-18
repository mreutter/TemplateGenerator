namespace BackendTemplateCreator.Configuration;

public class Names
{
    public string ProjectName { get; set; }

    //Partition Names
    public string APIPartitionName { get; set; }
    public string LogicPartitionName { get; set; }
    public string DataAccessPartitionName { get; set; }
    public bool UseRootPrefix { get; set; }

    //Folder Names
    public string DTOFolderName { get; set; }
    public string ModelFolderName { get; set; }
    public string RepositoryFolderName { get; set; }
    public string AbtractFolderName { get; set; }

    //Conventions
    public string DtoSuffix { get; set; }
    public string ModelSuffix { get; set; }
    public string ServiceSuffix { get; set; }
    public string RepositorySuffix { get; set; }
    public string ControllerSuffix { get; set; }
    public string DbSetSuffix { get; set; }


    //To implement
    public bool UpperCaseClass { get; set; }
    public bool LowerCaseVariable { get; set; } 

    //Misc. Names
    public string URLRoute { get; set; }
    public string DbContextName { get; set; }
    public string DataDependencyInjectName { get; set; }
    public string LogicDependencyInjectName { get; set; }
}
