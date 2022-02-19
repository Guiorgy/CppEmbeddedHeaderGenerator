using CommandLine;
using System;

namespace CppEmbeddedHeaderGenerator
{
    public static class Program
    {
        public class Options
        {
            [Option('e', "embedded-dir", Required = true, HelpText = "Path to the directory containing the files to embed.")]
            public string EmbeddedDirectoryPath { get; set; } = Environment.CurrentDirectory;

            [Option('i', "ignorefile", Required = false, HelpText = "Path to the ignore file (in the format of a .gitignore). By default \".embedignore\" in the same directory as the files to embed.")]
            public string? IgnoreFilePath { get; set; } = null;

            [Option('o', "output-dir", Required = false, HelpText = "Path to the output directory. By default current directory.")]
            public string OutputDirectoryPath { get; set; } = Environment.CurrentDirectory;
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    EmbeddedHeaderGenerator.Generate(o.EmbeddedDirectoryPath, o.IgnoreFilePath, o.OutputDirectoryPath);
                    EmbeddedFileExtractorGenerator.Generate(o.OutputDirectoryPath);
                });
        }
    }
}
