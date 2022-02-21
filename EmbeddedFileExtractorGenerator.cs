﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace CppEmbeddedHeaderGenerator
{
    public static class EmbeddedFileExtractorGenerator
    {
        private static readonly char directorySeparator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? '\\' : '/';

        protected class Resource
        {
            public string FileName { get; set; }
            public string[] SizeNames { get; set; }
            public string ResourceName { get; set; }
            public int SplitChunks { get; set; }
            public ResourceType Type { get; set; }

            public Resource(string fileName, string[] sizeNames, string resourceName, ResourceType type, int splitChunks = 0)
            {
                FileName = fileName;
                SizeNames = sizeNames;
                ResourceName = resourceName;
                Type = type;
                SplitChunks = splitChunks;
            }

            public enum ResourceType
            {
                ASCII,
                ASCIISplit,
                Binary,
                BinarySplit
            }
        }

        private static List<Resource> GetResources(string embeddedHeaderFilePath)
        {
            var resources = new List<Resource>();

            var text = File.ReadAllText(embeddedHeaderFilePath);
            var matches = Regex.Matches(text, "extern __declspec\\(selectany\\) constexpr std::string_view (.*)_name = std::string_view\\(\"(.*)\"\\);");
            foreach (Match match in matches)
            {
                var res = match.Groups[1].Value;
                var name = $"{res}_name";

                var ascii = Regex.Matches(text, $"extern __declspec\\(selectany\\) constexpr std::string_view {res} = ");
                if (ascii.Count == 1 && ascii[0].Success)
                {
                    resources.Add(new Resource(name, new string[] { "" }, res, Resource.ResourceType.ASCII));
                    continue;
                }

                var asciiChunks = Regex.Matches(text, $"extern __declspec\\(selectany\\) constexpr int {res}__ascii_chunks = (\\d+);");
                if (asciiChunks.Count == 1 && asciiChunks[0].Success)
                {
                    int chunks = int.Parse(asciiChunks[0].Groups[1].Value);
                    resources.Add(new Resource(name, new string[] { "" }, res, Resource.ResourceType.ASCIISplit, chunks));
                }

                var binSize = Regex.Matches(text, $"extern __declspec\\(selectany\\) constexpr int {res}_size = (\\d+);");
                if (binSize.Count == 1 && binSize[0].Success)
                {
                    resources.Add(new Resource(name, new string[] { $"{res}_size" }, res, Resource.ResourceType.Binary));
                }

                var binChunks = Regex.Matches(text, $"extern __declspec\\(selectany\\) constexpr int {res}__blob_chunks = (\\d+);");
                if (binChunks.Count == 1 && binChunks[0].Success)
                {
                    int chunks = int.Parse(binChunks[0].Groups[1].Value);
                    List<string> sizeNames = new();
                    for (int i = 0; i < chunks; i++) sizeNames.Add($"{res}_size_{i}");
                    resources.Add(new Resource(name, sizeNames.ToArray(), res, Resource.ResourceType.BinarySplit, chunks));
                }
            }

            return resources;
        }

        public static void Generate(string? outputDirectoryPath = null)
        {
            var outputDir = new DirectoryInfo(outputDirectoryPath ?? Environment.CurrentDirectory ?? Directory.GetCurrentDirectory());
            string embeddedHeaderFilePath = outputDir.FullName + directorySeparator + "embedded.h";
            string embeddedFileExtractorFilePath = outputDir.FullName + directorySeparator + "embedded-extractor.h";

            var code = new StringBuilder()
                .AppendLine("// This file uses the <filesystem> header, thus it needs to be compiled with a compiler that suports c++17.")
                .AppendLine("#ifndef EMBEDDED_RESOURCES_EXTRACTOR_HEADER_FILE")
                .AppendLine("#define EMBEDDED_RESOURCES_EXTRACTOR_HEADER_FILE")
                .AppendLine()
                .AppendLine("#if __cplusplus < 201703L")
                .AppendLine("#error The \"embedded\" namespace needs at least a C++17 compliant compiler")
                .AppendLine("#endif")
                .AppendLine()
                .AppendLine("#include \"embedded.h\"")
                .AppendLine("#include <iostream>")
                .AppendLine("#include <fstream>")
                .AppendLine("#include <filesystem>")
                .AppendLine()
                .AppendLine("namespace embedded")
                .AppendLine("{")
                .AppendLine()
                .AppendLine("\tbool _getDirectory(const std::string_view filePath, std::string& directoryPath)")
                .AppendLine("\t{")
                .AppendLine("\t\tif (std::string::size_type pos = filePath.find_last_of('/'); pos != std::string::npos)")
                .AppendLine("\t\t{")
                .AppendLine("\t\t\tdirectoryPath = filePath.substr(0, pos);")
                .AppendLine("\t\t\treturn true;")
                .AppendLine("\t\t}")
                .AppendLine("\t\treturn false;")
                .AppendLine("\t}")
                .AppendLine();

            var resources = GetResources(embeddedHeaderFilePath);
            foreach (var resource in resources)
            {
                code.AppendLine($"\tvoid extract_{resource.ResourceName}(std::string const outputDir = \".\", bool verbose = false)")
                    .AppendLine("\t{")
                    .AppendLine("\t\tif (outputDir != \".\")")
                    .AppendLine("\t\t{")
                    .AppendLine("\t\t\tif (verbose) std::cout << \"Creating the \\\"\" << outputDir << \"\\\" directory.\" << std::endl;")
                    .AppendLine("\t\t\tstd::filesystem::create_directory(outputDir);")
                    .AppendLine("\t\t}")
                    .AppendLine()
                    .AppendLine("\t\tstd::string dirPath;")
                    .AppendLine("\t\tstd::ofstream file;")
                    .AppendLine();

                code.AppendLine($"\t\tif (verbose) std::cout << \"Extracting the \\\"\" << embedded::{resource.FileName} << \"\\\" resource file.\" << std::endl;");

                code.AppendLine($"\t\tif (_getDirectory(embedded::{resource.FileName}, dirPath))")
                    .AppendLine("\t\t{")
                    .AppendLine("\t\t\tdirPath = outputDir + \"/\" + dirPath;")
                    .AppendLine("\t\t\tif (verbose) std::cout << \"Creating the \\\"\" << dirPath << \"\\\" directory.\" << std::endl;")
                    .AppendLine("\t\t\tstd::filesystem::create_directory(dirPath);")
                    .AppendLine("\t\t}");

                switch (resource.Type)
                {
                    case Resource.ResourceType.ASCII:
                        code.AppendLine($"\t\tfile.open(outputDir + \"/\" + embedded::{resource.FileName}.data());");
                        code.AppendLine($"\t\tfile << embedded::{resource.ResourceName};");
                        code.AppendLine("\t\tfile.close();");
                        break;
                    case Resource.ResourceType.ASCIISplit:
                        code.AppendLine($"\t\tfile.open(outputDir + \"/\" + embedded::{resource.FileName}.data());");
                        for (int i = 0; i < resource.SplitChunks; i++)
                            code.AppendLine($"\t\tfile << embedded::{resource.ResourceName}__ascii_chunk_{i};");
                        code.AppendLine("\t\tfile.close();");
                        break;
                    case Resource.ResourceType.Binary:
                        code.AppendLine($"\t\tfile.open(outputDir + \"/\" + embedded::{resource.FileName}.data(), std::ios::out | std::ios::binary);");
                        code.AppendLine($"\t\tfile.write(&embedded::{resource.ResourceName}[0], embedded::{resource.SizeNames[0]});");
                        code.AppendLine("\t\tfile.close();");
                        break;
                    case Resource.ResourceType.BinarySplit:
                        code.AppendLine($"\t\tfile.open(outputDir + \"/\" + embedded::{resource.FileName}.data(), std::ios::out | std::ios::binary);");
                        for (int i = 0; i < resource.SplitChunks; i++)
                            code.AppendLine($"\t\tfile.write(&embedded::{resource.ResourceName}__blob_chunk_{i}[0], embedded::{resource.SizeNames[i]});");
                        code.AppendLine("\t\tfile.close();");
                        break;
                }
                code.AppendLine("\t}")
                    .AppendLine();
            }

            code.AppendLine("\tvoid extractAll(std::string const outputDir = \".\", bool verbose = false)")
                .AppendLine("\t{")
                .AppendLine("\t\tif (outputDir != \".\")")
                .AppendLine("\t\t{")
                .AppendLine("\t\t\tif (verbose) std::cout << \"Creating the \\\"\" << outputDir << \"\\\" directory.\" << std::endl;")
                .AppendLine("\t\t\tstd::filesystem::create_directory(outputDir);")
                .AppendLine("\t\t}")
                .AppendLine();
            foreach (var res in resources)
                code.AppendLine($"\t\textract_{res.ResourceName}(outputDir, verbose);");

            code.AppendLine("\t}")
                .AppendLine()
                .AppendLine("}")
                .AppendLine()
                .AppendLine("#endif");

            File.WriteAllText(embeddedFileExtractorFilePath, code.ToString());
        }
    }
}
