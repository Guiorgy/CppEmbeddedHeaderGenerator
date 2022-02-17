using GitignoreParserNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace CppEmbeededHeaderGenerator
{
    public static class Program
    {
        static List<string> ListFileNames(string directoryPath)
        {
            DirectoryInfo dir = new(directoryPath);

            List<string> files = new();
            foreach (FileInfo file in dir.GetFiles())
                files.Add(file.FullName);

            foreach (DirectoryInfo subDir in dir.GetDirectories())
                files.AddRange(ListFileNames(subDir.FullName));

            return files;
        }

        static void Main(string[] args)
        {
            const string embeededPath = @"..\..\..\Embeeded";

            var files = ListFileNames(embeededPath);

            var enbeedignoreFile = new FileInfo(Path.Combine(embeededPath, ".embeedignore"));
            string enbeedignore = File.ReadAllText(enbeedignoreFile.FullName, Encoding.UTF8);
            var parser = new GitignoreParser(enbeedignore);

            var accepted = files.Where(file => parser.Accepts(file)).ToList();
            accepted.Remove(enbeedignoreFile.FullName);

            Console.WriteLine("Files to embeed:");
            foreach (string file in accepted)
                Console.WriteLine(file);
            Console.WriteLine();

            const string outputPath = @"..\..\..\Output";
            var outputDir = new DirectoryInfo(outputPath);
            if (!outputDir.Exists)
                outputDir.Create();
            const string resourceFilePath = @"..\..\..\Output\embeeded.h";

            using StreamWriter writer = new(resourceFilePath, false, Encoding.UTF8);
            writer.WriteLine("#ifndef EMBEEDED_RESOURCES_HEADER_FILE");
            writer.WriteLine("#define EMBEEDED_RESOURCES_HEADER_FILE");
            writer.WriteLine("");
            writer.WriteLine("#include <string>");
            writer.WriteLine("");
            writer.WriteLine("namespace embeed");
            writer.WriteLine("{");
            writer.WriteLine("");
            writer.WriteLine("\tstd::string empty = \"\";");

            char dirSep = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? '\\' : '/';
            string lineSep =
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"\r\n" :
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? @"\r" : @"\n";
            foreach (string filePath in accepted)
            {
                string name = filePath[(filePath.LastIndexOf(dirSep) + 1)..];
                bool isAscii = name.StartsWith("ascii_");
                if (isAscii) name = name[6..];
                string resname =
                    name
                    .Replace(' ', '_')
                    .Replace('-', '_')
                    .Replace('.', '_');
                if (Regex.IsMatch(resname, @"^\d"))
                    resname = '_' + resname;

                Console.WriteLine($"Creating a {(isAscii ? "string" : "byte array")} resource with name \"{resname}\"");
                writer.WriteLine($"\textern __declspec(selectany) std::string {resname}_name = \"{name}\";");

                if (isAscii)
                {
                    writer.WriteLine($"\textern __declspec(selectany) std::string {resname} = empty");
                    foreach (string line in File.ReadLines(filePath, Encoding.ASCII))
                    {
                        var eline =
                            line
                            .Replace(@"\", @"\\")
                            .Replace(@"""", @"\""");
                        writer.WriteLine($"\t\t+ \"{eline}{lineSep}\"");
                    }
                    writer.WriteLine($"\t\t;");
                }
                else
                {
                    bool first = true;
                    byte[] bytes = File.ReadAllBytes(filePath);
                    writer.WriteLine($"\textern __declspec(selectany) int {resname}_size = {bytes.Length};");
                    writer.Write($"\textern __declspec(selectany) char {resname}[{bytes.Length}] = {{");
                    foreach (byte b in bytes)
                    {
                        writer.Write($"{(first ? "" : ",")} {b}");
                        first = false;
                    }
                    writer.WriteLine($" }};");
                }
            }

            writer.WriteLine("");
            writer.WriteLine("}");
            writer.WriteLine("");
            writer.WriteLine("#endif");
        }
    }
}
