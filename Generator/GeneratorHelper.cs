using System.Text;
using System.Text.RegularExpressions;

namespace BackendTemplateCreator.Generator;

public class GeneratorHelper
{
    private static string _successColor = "green";
    private static string _infoColor = "blue";
    private static string _warnColor = "yellow1";
    private static string _admonitionColor = "#ffaa00";
    private static string _errorColor = "#ff0000";

    private static string? TryRead(string filePath, string name)
    {
        string? fileContent = null;
        try
        {
            //Read File
            fileContent = File.ReadAllText(filePath);
        }
        catch (Exception e)
        {
            Error("Couldnt read file due to: " + e.Message, false);
        }
        return fileContent;
    }
    private static bool TryWrite(string filePath, string name, string content)
    {
        bool isSuccess = true;
        try
        {
            File.WriteAllText(filePath, content);
        }
        catch (Exception e)
        {
            isSuccess = false;
            Error($"Failed to create \"{name}\": {e}", false);
        }
        if (isSuccess)
        {
            WriteLine($"Successfully created \"{name}\".", _successColor);
        }
        return isSuccess;
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
            Error($"Failed to create \"{projectName}\".", false);
        }
        if (isSuccess)
        {
            Info($"Successfully created \"{projectName}\".");
        }
    }

    private static Dictionary<string, string> AssignDefinitions(MatchCollection sectionDefinitions)
    {
        Dictionary<string, string> sectionDefinitionMap = new();
        for (int i = 0; i < sectionDefinitions.Count; i++)
        {
            GroupCollection currentDefinition = sectionDefinitions[i].Groups;
            string identifier = currentDefinition[1].Value;
            string definition = currentDefinition[3].Value;

            sectionDefinitionMap[identifier] = definition;
        }
        return sectionDefinitionMap;
    }

    private static string ReplaceParameters(Regex regex, string combinedString, Dictionary<string, string> parameters, string projectName)
    {
        string[] parameterSplit = regex.Split(combinedString);
        for (int i = 0; i < parameterSplit.Length - 2; i += 2)
        {
            if(!parameters.ContainsKey(parameterSplit[i + 1]))
            {
                Error($"Couldnt find the parameter \"{parameterSplit[i + 1]}\" while generating \"{projectName}\".", false);
                throw new Exception("Invalid Parameter");
            }
            parameterSplit[i + 1] = parameters[parameterSplit[i + 1]];
        }
        return string.Join("", parameterSplit);
    }

    public static void TemplateReplacer(string source, string destination, string templateName, string projectName,
        Dictionary<string, string> parameters = null,
        Dictionary<string, bool> sections = null,
        Dictionary<string, Dictionary<string, string>> sectionParameters = null,
        Dictionary<string, Dictionary<string, string>[]> multipleSectionParameters = null)
    {
        //Create Directory if it doesnt exist
        string fullDestinationPath = destination + projectName;
        if (!Directory.Exists(destination)) Directory.CreateDirectory(destination);

        string fileContent = TryRead(source + templateName, projectName);
        if (fileContent == null) { Info($"Skipping creation of {projectName}."); return; }

        //Extract Definition
        var definitiveSectionDefinition = Regex.Match(fileContent, @"\$!=\s*\{(?:\r\n|\n)?([\s\S]*?)\}#");
        var multipleSectionDefinitions = Regex.Matches(fileContent, @"\$\?\*\s*(\w+)\s*=\s*(\w*)\s*\{(?:\r\n|\n)?([\s\S]*?)\}#");
        var sectionDefinitions = Regex.Matches(fileContent, @"\$\?\s*(\w+)\s*=\s*(\w*)\s*\{(?:\r\n|\n)?([\s\S]*?)\}#");

        //Map Identifier to Definition
        Dictionary<string, string> multipleSectionDefinitionMap = AssignDefinitions(multipleSectionDefinitions);
        Dictionary<string, string> sectionDefinitionMap = AssignDefinitions(sectionDefinitions);

        //Section Definition Regex
        Regex greg = new Regex(@"\$\?(\*|)\s*(\w+)\s*#", RegexOptions.Compiled);
        //Local Parameter Regex
        Regex grog = new Regex(@"\$\%\s*(\w+)\s*#", RegexOptions.Compiled);
        //Global Parameter Regex
        Regex grug = new Regex(@"\$\s*(\w+)\s*#", RegexOptions.Compiled);

        string combinedContent = definitiveSectionDefinition.Groups[1].Value; //Extract Group instead of Value to avoid the "$!={"
        string[] sectionSplitContent;
        int recursion = 0;
        while (true)
        {
            //Max Recursion to avoid infinite cycle
            if (++recursion > 10)
            {
                Error($"Exceeded Max Recursion level in {projectName}.", false);
                Info($"Skipping generation of {projectName}.");
                return;
            }

            //Extract locations
            StringBuilder sectionInjectContent = new();
            sectionSplitContent = greg.Split(combinedContent);

            //Cant be split -> no more sections
            if (sectionSplitContent.Length == 1) break;

            for (int i = 0; i < sectionSplitContent.Length - 3; i += 3) // Needs to step by 3 because of capturing groups
            {
                string sectionIdentifier = sectionSplitContent[i + 2];
                StringBuilder filledSection = new();

                if (sectionSplitContent[i + 1] != string.Empty) //Multisection
                {
                    if (!multipleSectionDefinitionMap.ContainsKey(sectionIdentifier))
                    {
                        Error($"Couldnt find the multipleSection with \"{sectionIdentifier}\" while generating \"{projectName}\".", false);
                        Warn($"Skipping generation of {projectName}.");
                        return;
                    }
                    string definition = multipleSectionDefinitionMap[sectionIdentifier];
                    if (multipleSectionParameters != null)
                    {
                        foreach (var p in multipleSectionParameters[sectionIdentifier])
                        {
                            //Replace Local
                            string str;
                            try
                            {
                                str = ReplaceParameters(grog, definition, p, projectName);
                            }
                            catch (Exception e)
                            {
                                Warn($"Skipping generation of \"{projectName}\".");
                                return;
                            }
                            filledSection.Append(str);
                        }
                    }
                }
                else if (sections.ContainsKey(sectionIdentifier) && sectionDefinitionMap.ContainsKey(sectionIdentifier)) //Section (Could also regex.replace beforehand)
                {
                    //Replace Local
                    string definition = sectionDefinitionMap[sectionIdentifier];
                    if (sectionParameters != null && sectionParameters[sectionIdentifier] != null)
                    {
                        //Replace Local
                        string str;
                        try
                        {
                            str = ReplaceParameters(grog, definition, sectionParameters[sectionIdentifier], projectName);
                        }
                        catch (Exception e)
                        {
                            Warn($"Skipping generation of \"{projectName}\".");
                            return;
                        }
                        filledSection.Append(str);
                    }
                    else filledSection.Append(definition);
                } else
                {
                    Error($"Couldnt find parameter for \"{sectionIdentifier}\" in the Template: \"{templateName}\" while generating \"{projectName}\".", false);
                    Warn($"Skipping generation of {projectName}");
                    return;
                }

                //Add Section and definitive part
                sectionInjectContent.Append(sectionSplitContent[i]);
                sectionInjectContent.Append(filledSection.ToString());
            }
            sectionInjectContent.Append(sectionSplitContent.Last());
            
            //Combine Parts
            combinedContent = string.Join("", sectionInjectContent);
        }

        //Replace Global
        string result;
        try
        {
            result = ReplaceParameters(grug, combinedContent, parameters, projectName).Replace("\r\n!¬", "").Replace("¬", "\r\n");
            TryWrite(fullDestinationPath, projectName, result);
        }
        catch (Exception e)
        {
            Warn($"Skipping generation of \"{projectName}\".");
            return;
        }
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

    public static string ToLowerFirst(string str)
    {
        return char.ToLower(str[0]) + str.Substring(1);
    }
}

