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

            [Option('l', "literal-limit", Required = false, HelpText = "The maximum length of a string literal. by default 16300.")]
            public int StringLiteralLimit { get; set; } = 16_300;
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    EmbeddedHeaderGenerator.Generate(o.EmbeddedDirectoryPath, o.IgnoreFilePath, o.OutputDirectoryPath, o.StringLiteralLimit);
                    EmbeddedFileExtractorGenerator.Generate(o.OutputDirectoryPath);
                });
        }
    }
}
