using System.Text;
using System.Text.RegularExpressions;

namespace BackendTemplateCreator.Generator;

public class GeneratorHelper
{
    private static string _successColor = "green";
    private static string _failColor = "red";

    private static string _infoColor = "blue";
    private static string _warnColor = "yellow1";
    private static string _admonitionColor = "#ffaa00";
    private static string _errorColor = "#ff0000";

    private static string SubstituteMarker(string path, (string,string)[] substituents)
    {
        StringBuilder sb = new();
        sb.Append(File.ReadAllText(path));


        for (int i = 0; i < substituents.Length; i++)
        {
            sb.Replace($"${substituents[i].Item1}#", substituents[i].Item2);
        }

        return sb.ToString();
    }
    
    public static void StitchReplaceFile(string source, string destination, string templateName, string projectName, (string,string)[] substituents)
    {
        //Template name includes its extension
        string fileExtension = (projectName.Split('.').Length > 1 ? "" : Path.GetExtension(templateName));
        string convertedFile = SubstituteMarker(source + templateName, substituents);
        bool isSuccess = true;

        //Create Directory if it doesnt exist
        if (!Directory.Exists(destination)) Directory.CreateDirectory(destination);

        //Delete if exists
        string fullDestinationPath = destination + projectName + fileExtension;
        if (File.Exists(fullDestinationPath))
        {
            Warn($"Overwrote {projectName}{fileExtension} file. ");
            File.Delete(fullDestinationPath);
        }

        try
        {
            File.WriteAllText(fullDestinationPath, convertedFile);
        }
        catch (Exception e)
        {
            isSuccess = false;
            WriteLine($"Failed to create: {destination}{projectName}{fileExtension} Error: {e.Message}", _failColor);
        }
        if (isSuccess)
        {
            WriteLine($"Succesfully created: {destination}{projectName}{fileExtension}", _successColor);
        }
    }

    public static void CopyRenameFile(string source, string destination, string templateName, string projectName)
    {
        string fileExtension = (projectName.Split('.').Length > 1 ? "" : Path.GetExtension(templateName));
        bool isSuccess = true;

        //Create Directory if it doesnt exist
        if (!Directory.Exists(destination)) Directory.CreateDirectory(destination);

        //Delete if exists
        string fullDestinationPath = destination + projectName + fileExtension;
        if (File.Exists(fullDestinationPath)) File.Delete(fullDestinationPath);

        try
        {
            File.Copy(source + templateName, fullDestinationPath);
        }
        catch (Exception e)
        {
            isSuccess = false;
            WriteLine($"Failed to create: {destination}{projectName}{fileExtension} Error: {e.Message}", _failColor);
        }
        if (isSuccess)
        {
            WriteLine($"Succesfully created: {destination}{projectName}{fileExtension}", _successColor);
        }
    }

    public static void TemplateReplacer(string source, string destination, string templateName, string projectName, 
        Dictionary<string, string> parameters, 
        Dictionary<string, Dictionary<string, string>> sections, 
        Dictionary<string, Dictionary<string, string>[]> multipleSections)
    {
        //Create Directory if it doesnt exist
        string fullDestinationPath = destination + projectName;
        if (!Directory.Exists(destination)) Directory.CreateDirectory(destination);

        string fileContent = string.Empty;
        
        //Read File
        try
        {
            //Delete if exists
            if (File.Exists(fullDestinationPath))
            {
                Warn($"Overwrote {projectName}.");
                File.Delete(fullDestinationPath);
            }

            //Read File
            fileContent = File.ReadAllText(fullDestinationPath);
        }
        catch (Exception e)
        {
            Error("Couldnt manipulate file due to: " + e.Message, false);
        }

        //Extract Definition
        var definitiveSectionDefinition = Regex.Match(fileContent, @"\$!=\s*\{\n([\s\S]*?)\}#");
        var multipleSectionDefinitions = Regex.Matches(fileContent, @"\$\?\*\s*(\w+)\s*=\s*(\w*)\s*\{\n*([\s\S]*?)\}#");
        var sectionDefinitions = Regex.Matches(fileContent, @"\$\?\s*(\w+)\s*=\s*(\w*)\s*\{\n*([\s\S]*?)\}#");

        Dictionary<string, string> multipleSectionDefinitionMap = new();
        Dictionary<string, string> sectionDefinitionMap = new();
        for (int i = 0; i < multipleSectionDefinitions.Count; i++)
        {
            //Extract from Match
            GroupCollection currentDefinition = multipleSectionDefinitions[i].Groups;
            string identifier = currentDefinition[1].Value;
            string definition = currentDefinition[2].Value;

            multipleSectionDefinitionMap[identifier] = definition;
        }

        for (int i = 0; i < sectionDefinitions.Count; i++)
        {
            GroupCollection currentDefinition = sectionDefinitions[i].Groups;
            string identifier = currentDefinition[1].Value;
            string definition = currentDefinition[2].Value;

            sectionDefinitionMap[identifier] = definition;
        }



        //Find the used SectionLocations
        //Until no more sections are found replace by getting indices of start and end
        Regex greg = new Regex(@"\$\?(\*|)\s*(\w+)\s*#", RegexOptions.Compiled);
        Regex grog = new Regex(@"\$(\%|)\s*(\w+)\s*#", RegexOptions.Compiled);

        string combinedContent = definitiveSectionDefinition.Value;
        string[] sectionSplitContent;
        int recursion = 0;
        do
        {
            //Max Recursion to avoid infinite cycle
            if(++recursion > 5)
            {
                Error($"Recursion count exceeded: Template file {templateName} most likely contains a cyclic recursion, when used in combination with parameters for {projectName}.", false);
                Info($"Skipping generation of {projectName}.");
                return;
            }

            //Extract locations
            StringBuilder sectionInjectContent = new();
            sectionSplitContent = greg.Split(combinedContent);
            for (int i = 0; i < sectionSplitContent.Length - 3; i += 3)
            {
                StringBuilder parameterInjectSection = new();
                char sectionType = sectionSplitContent[i + 1][0];
                string sectionIdentifier = sectionSplitContent[i + 2];
                //after here it depends if its multipleSection or single section
                if(sectionType == '*')
                {
                    string definition = multipleSectionDefinitionMap[sectionIdentifier];

                    string[] parameterSplitSection = grog.Split(definition);
                    foreach (var sectionParameters in multipleSections[sectionIdentifier])
                    {
                        for (int j = 0; j < parameterSplitSection.Length - 3; j += 3)
                        {
                            char parameterType = parameterSplitSection[j + 1][0];
                            string parameterIdentifier = parameterSplitSection[j + 2];

                            parameterInjectSection.Append(parameterSplitSection[j]);
                            parameterInjectSection.Append(parameterType == '%' ? sectionParameters[parameterIdentifier] : parameters[parameterIdentifier]);
                        }
                        parameterInjectSection.Append(parameterSplitSection.Last());
                    }
                } 
                else
                {
                    string definition = sectionDefinitionMap[sectionIdentifier];

                    string[] parameterSplitSection = grog.Split(definition);
                    for (int j = 0; j < parameterSplitSection.Length - 3; j += 3)
                    {
                        char parameterType = parameterSplitSection[j + 1][0];
                        string parameterIdentifier = parameterSplitSection[j + 2];

                        parameterInjectSection.Append(parameterSplitSection[j]);
                        parameterInjectSection.Append(parameterType == '%' ? sections[sectionIdentifier][parameterIdentifier] : parameters[parameterIdentifier]);
                    }
                    parameterInjectSection.Append(parameterSplitSection.Last());
                }
                sectionInjectContent.Append(sectionSplitContent[i]);
                sectionInjectContent.Append(parameterInjectSection.ToString());
            }
        } while(sectionSplitContent.Length - 1 > 0);
    }
    
    public static void WriteLine(string message, string color)
    {
        Spectre.Console.AnsiConsole.Markup($"[{color}]{message}\n[/]");
    }

    public static void Error(string message, bool fatalError = true)
    {
        string errorModifer = fatalError ? "FATAL " : "";
        WriteLine($"{errorModifer}ERROR: {message}", _errorColor);
        if(fatalError)
        {
            Info("Cancelling generation process.");
            System.Environment.Exit(1);
        }
    }

    public static void Warn(string message, string prefix = "") { WriteLine($"{prefix}WARNING: {message}", _warnColor); }
    public static void Info(string message) { WriteLine($"INFO: {message}", _infoColor); }
    public static void Admonition(string message, string prefix = "") { WriteLine($"{prefix}ADMONITION: {message}", _admonitionColor); }
    
    public static string GenerateKey(int keyLength, bool useLower = true, bool useUpper = true, bool useNumber = true, bool useSpecial = false)
    {
        string keySelection = (useLower ? "abcdefghijklmnopqrstuvwxyz" : "") + (useUpper ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ" : "") + (useNumber ? "0123456789" : "") + (useSpecial ? "+\"*ç%&/()=?¦@#°§¬|¢[]{}<>\\,.-;:_!§°'" : "");  

        StringBuilder sb = new();
        Random rng = new();

        for (int i = 0; i < keyLength; i++)
        {
            char c = keySelection[rng.Next(keySelection.Length)];
            sb.Append(c);
        }

        return sb.ToString();
    }
}

