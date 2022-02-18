using GitignoreParserNet;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace CppEmbeddedHeaderGenerator
{
    public static class Program
    {
        static List<string> ListFilePaths(string directoryPath)
        {
            DirectoryInfo dir = new(directoryPath);

            List<string> files = new();
            foreach (FileInfo file in dir.GetFiles())
                files.Add(file.FullName);

            foreach (DirectoryInfo subDir in dir.GetDirectories())
                files.AddRange(ListFilePaths(subDir.FullName));

            return files;
        }

        [SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "May revert in the future.")]
        static string LineSeparator
        {
            get
            {
                /*return // On Windows use "\r\n", on OSX use "\r", otherwise (on Linux and BSD) use "\n"
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"\r\n" :
                        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? @"\r" : @"\n";*/
                return @"\n";
            }
        }

        static void Main(string[] args)
        {
            const string embeddedPath = @"..\..\..\Embedded";

            var files = ListFilePaths(embeddedPath);

            var enbeedignoreFile = new FileInfo(Path.Combine(embeddedPath, ".embedignore"));
            string enbeedignore = File.ReadAllText(enbeedignoreFile.FullName, Encoding.UTF8);
            var parser = new GitignoreParser(enbeedignore);

            var accepted = files.Where(file => parser.Accepts(file)).ToList();
            accepted.Remove(enbeedignoreFile.FullName);

            Console.WriteLine("Files to embedded:");
            foreach (string file in accepted)
                Console.WriteLine(file);
            Console.WriteLine();

            const string outputPath = @"..\..\..\Output";
            var outputDir = new DirectoryInfo(outputPath);
            if (!outputDir.Exists)
                outputDir.Create();
            const string resourceFilePath = @"..\..\..\Output\embedded.h";

            using StreamWriter writer = new(resourceFilePath, false, Encoding.UTF8);
            writer.WriteLine("#ifndef EMBEDDED_RESOURCES_HEADER_FILE");
            writer.WriteLine("#define EMBEDDED_RESOURCES_HEADER_FILE");
            writer.WriteLine("");
            writer.WriteLine("#include <string>");
            writer.WriteLine("");
            writer.WriteLine("namespace embedded");
            writer.WriteLine("{");
            writer.WriteLine("");
            writer.WriteLine("\tstd::string empty = \"\";");

            char dirSep = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? '\\' : '/';
            string lineSep = LineSeparator;
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
                    static string PrepareLane(string line)
                    {
                        return line
                            .Replace(@"\", @"\\")
                            .Replace(@"""", @"\""");
                    }
                    writer.WriteLine($"\textern __declspec(selectany) std::string {resname} = empty");
                    var lines = File.ReadLines(filePath, Encoding.ASCII).ToList();
                    for (int i = 0; i < lines.Count - 1; i++)
                    {
                        var line = lines[i];
                        writer.WriteLine($"\t\t+ \"{PrepareLane(line)}{lineSep}\"");
                    }
                    writer.WriteLine($"\t\t+ \"{PrepareLane(lines.Last())}\"");
                    writer.WriteLine($"\t\t;");
                }
                else
                {
                    bool first = true;
                    byte[] bytes = File.ReadAllBytes(filePath);
                    writer.WriteLine($"\textern __declspec(selectany) int {resname}_size = {bytes.Length};");
                    writer.Write($"\textern __declspec(selectany) unsigned char {resname}[{bytes.Length}] = {{");
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
