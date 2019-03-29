
using CommandLine;
using System.IO;
using System.Globalization;
namespace StandardImportToolPlugin
{
    public class CommandLineOptions {
        [Option(
            shortName: 'c',
            longName: "config_path",
            Required = true,
            HelpText = "Path to a json formatted file specifying SIT import configuration"
            )]
        public string ConfigPath { get; set; }

        [Option(
         shortName: 'l',
         longName: "culture_info",
         Required= false,
         Default=null,
         HelpText = "A culture info name for one of the languages " +
            "supported by the SIT for error and log feedback. " +
            "For example (en-CA, fr-CA, es-MX, pl-PL or ru-RU)"
         )]
        public string CultureInfoName { get; set; }
    }

    class Program
    {
        static string ReadFile(string path)
        {
            using (StreamReader reader = new StreamReader(File.OpenRead(path)))
            {
                return reader.ReadToEnd();
            }
        }
        static void Main(string[] args)
        {

            var options = new CommandLineOptions();
            var result = Parser.Default.ParseArguments<CommandLineOptions>(args);
            result.WithParsed(a => {
                if(a.CultureInfoName != null){
                    Global.StringUtilities.ci = new CultureInfo(a.CultureInfoName, true);
                }
                string json = ReadFile(a.ConfigPath);
                JsonConfigLoader jsonConfigLoader = new JsonConfigLoader();
                Sitplugin sitplugin = jsonConfigLoader.Load(json);
                try
                {
                    sitplugin.Import();
                }
                catch(CBMSIT.ProjectCreation.ProjectCreationException ex)
                {
                    System.Console.WriteLine(ex.Message);
                    throw ex;
                }
                
            });
        }
    }
}
