﻿using GitignoreParserNet;
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
    public static class EmbeddedHeaderGenerator
    {
        [SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "May revert in the future.")]
        /*private static readonly string lineSeparator =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"\r\n" :
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? @"\r" : @"\n";*/

        private static readonly string lineSeparator = @"\n";
        private static readonly char directorySeparator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? '\\' : '/';

        private static List<string> ListFilePaths(string directoryPath)
        {
            DirectoryInfo dir = new(directoryPath);

            List<string> files = new();
            foreach (FileInfo file in dir.GetFiles())
                files.Add(file.FullName);

            foreach (DirectoryInfo subDir in dir.GetDirectories())
                files.AddRange(ListFilePaths(subDir.FullName));

            return files;
        }

        public static void Generate(string embeddedDirectoryPath, string? enbeedignoreFilePath = null, string? outputDirectoryPath = null)
        {
            var files = ListFilePaths(embeddedDirectoryPath);

            var enbeedignoreFile = new FileInfo(enbeedignoreFilePath ?? Path.Combine(embeddedDirectoryPath, ".embedignore"));

            List<string> accepted;
            if (enbeedignoreFile.Exists)
            {
                string enbeedignore = File.ReadAllText(enbeedignoreFile.FullName, Encoding.UTF8);
                var parser = new GitignoreParser(enbeedignore);
                accepted = files.Where(file => parser.Accepts(file)).ToList();
            }
            else
            {
                accepted = files;
            }
            accepted.Remove(enbeedignoreFile.FullName);

            Console.WriteLine("Files to embedd:");
            foreach (string file in accepted)
                Console.WriteLine(file);
            Console.WriteLine();

            var outputDir = new DirectoryInfo(outputDirectoryPath ?? Environment.CurrentDirectory ?? Directory.GetCurrentDirectory());
            if (!outputDir.Exists) outputDir.Create();
            string resourceFilePath = outputDir.FullName + directorySeparator + "embedded.h";


            var code = new StringBuilder()
                .AppendLine("#ifndef EMBEDDED_RESOURCES_HEADER_FILE")
                .AppendLine("#define EMBEDDED_RESOURCES_HEADER_FILE")
                .AppendLine()
                .AppendLine("#include <string>")
                .AppendLine("#include <array>")
                .AppendLine()
                .AppendLine("namespace embedded")
                .AppendLine("{")
                .AppendLine()
                .AppendLine("#if __cplusplus < 202002L")
                .AppendLine("\ttemplate<typename _Tp, size_t _Nm>")
                .AppendLine("\t[[nodiscard]]")
                .AppendLine("\tconstexpr std::array<_Tp, _Nm> to_array(_Tp arr[_Nm]) noexcept {")
                .AppendLine("\treturn { {std::move(arr[_Nm])} };")
                .AppendLine("\t}")
                .AppendLine("#else")
                .AppendLine("#define to_array std::to_array")
                .AppendLine("#endif")
                .AppendLine();

            var embeddedDir = new DirectoryInfo(embeddedDirectoryPath);
            int embeddedDirPathLen = embeddedDir.FullName.Length + (embeddedDir.FullName.EndsWith(directorySeparator) ? 0 : 1);
            foreach (string filePath in accepted)
            {
                string name = filePath[embeddedDirPathLen..];
                bool isAscii = false;
                if (name.Contains(directorySeparator))
                {
                    var fileName = name[(name.LastIndexOf(directorySeparator) + 1)..];
                    if (fileName.StartsWith("ascii_"))
                    {
                        name = name[..(name.LastIndexOf(directorySeparator) + 1)] + fileName[6..];
                        isAscii = true;
                    }
                }
                else
                {
                    if (name.StartsWith("ascii_"))
                    {
                        name = name[6..];
                        isAscii = true;
                    }
                }
                string resname =
                    name.Replace(' ', '_')
                    .Replace('-', '_')
                    .Replace('.', '_')
                    .Replace($"{directorySeparator}", "_dirSep_");
                if (Regex.IsMatch(resname, @"^\d"))
                    resname = '_' + resname;

                Console.WriteLine($"Creating a {(isAscii ? "string" : "byte array")} resource with name \"{resname}\"");
                code.AppendLine($"\textern __declspec(selectany) constexpr std::string_view {resname}_name = std::string_view(\"{name.Replace('\\', '/')}\");");

                if (isAscii)
                {
                    static string PrepareLane(string line)
                    {
                        return line
                            .Replace(@"\", @"\\")
                            .Replace(@"""", @"\""");
                    }
                    code.Append($"\textern __declspec(selectany) constexpr std::string_view {resname} = std::string_view(\"");
                    var lines = File.ReadLines(filePath, Encoding.ASCII).ToList();
                    for (int i = 0; i < lines.Count - 1; i++)
                    {
                        var line = lines[i];
                        code.Append($"{PrepareLane(line)}{lineSeparator}");
                    }
                    code.AppendLine($"{PrepareLane(lines.Last())}\");");
                }
                else
                {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    code.AppendLine($"\textern __declspec(selectany) constexpr int {resname}_size = {bytes.Length};")
                        .Append($"\textern __declspec(selectany) constexpr char {resname}[{bytes.Length + 1}] = \"");
                    bool hex = false;
                    foreach (byte b in bytes)
                    {
                        string cchar;
                        if (b == 0)
                        {
                            cchar = "\\0";
                            hex = true;
                        }
                        else if (b == 34)
                            cchar = "\\\"";
                        else if (b == 92)
                            cchar = "\\\\";
                        else
                        {
                            if (
                                // Visible characters
                                32 <= b && b <= 126
                                && (!hex
                                    // Before numbers
                                    || b <= 47
                                    // Between numbers and capital letters
                                    || (58 <= b && b <= 64)
                                    // Between Capital F and small letters
                                    || (71 <= b && b <= 96)
                                    // After small f
                                    || 103 <= b))
                            {
                                cchar = ((char)b).ToString();
                                hex = false;
                            }
                            else
                            {
                                cchar = $"\\x{b:X}";
                                hex = true;
                            }
                        }
                        code.Append(cchar);
                    }
                    code.AppendLine($"\";");
                }
            }

            code.AppendLine()
                .AppendLine("}")
                .AppendLine()
                .AppendLine("#endif");

            File.WriteAllText(resourceFilePath, code.ToString());
        }
    }
}
