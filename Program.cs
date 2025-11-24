namespace BackendTemplateCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            var BackendGen = new BackendGenerator();

            BackendGen.Greet();

            BackendGen.ReadConfig();

            //if tables file doesnt exist
            //BackendGen.ReadSQLFile();
            //BackendGen.WriteTableJSON();

            //BackendGen.ReadTableJSON();

            //if names file doesnt exist
            BackendGen.GenerateNames();

            BackendGen.Generate();
        }
    }
}