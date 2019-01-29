
using CommandLine;
using System.IO;
namespace StandardImportToolPlugin
{
    public class CommandLineOptions {
        public string ConfigPath { get; set; }
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
                string json = ReadFile(a.ConfigPath);
                JsonConfigLoader jsonConfigLoader = new JsonConfigLoader();
                Sitplugin sitplugin = jsonConfigLoader.Load(json);
                sitplugin.Import();
            });
        }
    }
}
