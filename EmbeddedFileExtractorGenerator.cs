using System;
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
            public string SizeName { get; set; }
            public string ResourceName { get; set; }
            public ResourceType Type { get; set; }

            public Resource(string fileName, string sizeName, string resourceName, ResourceType type)
            {
                FileName = fileName;
                SizeName = sizeName;
                ResourceName = resourceName;
                Type = type;
            }

            public enum ResourceType
            {
                ASCII,
                Binary
            }
        }

        private static List<Resource> GetResources(string embeddedHeaderFilePath)
        {
            var resources = new List<Resource>();

            var text = File.ReadAllText(embeddedHeaderFilePath);
            var matches = Regex.Matches(text, "extern __declspec\\(selectany\\) std::string (.*)_name = \"(.*)\";");
            foreach (Match match in matches)
            {
                var res = match.Groups[1].Value;
                var name = $"{res}_name";

                var ascii = Regex.Matches(text, $"extern __declspec\\(selectany\\) std::string {res} = ");
                if (ascii.Count == 1 && ascii[0].Success)
                {
                    resources.Add(new Resource(name, "", res, Resource.ResourceType.ASCII));
                    continue;
                }

                var binSize = Regex.Matches(text, $"extern __declspec\\(selectany\\) int {res}_size = (\\d+);");
                if (binSize.Count == 1 && binSize[0].Success)
                {
                    resources.Add(new Resource(name, $"{res}_size", res, Resource.ResourceType.Binary));
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
                .AppendLine("#include \"embedded.h\"")
                .AppendLine("#include <iostream>")
                .AppendLine("#include <fstream>")
                .AppendLine("#include <string>")
                .AppendLine("#include <filesystem>")
                .AppendLine()
                .AppendLine("namespace embedded")
                .AppendLine("{")
                .AppendLine()
                .AppendLine("\tbool getDirectory(std::string const& filePath, std::string& directoryPath)")
                .AppendLine("\t{")
                .AppendLine("\t\tstd::string::size_type pos = filePath.find_last_of('/');")
                .AppendLine("\t\tif (pos != std::string::npos)")
                .AppendLine("\t\t{")
                .AppendLine("\t\t\tdirectoryPath = filePath.substr(0, pos);")
                .AppendLine("\t\t\treturn true;")
                .AppendLine("\t\t}")
                .AppendLine("\t\treturn false;")
                .AppendLine("\t}")
                .AppendLine()
                .AppendLine("\tvoid extractAll(std::string const outputDir = \".\")")
                .AppendLine("\t{")
                .AppendLine("\t\tif (outputDir != \".\")")
                .AppendLine("\t\t\tstd::filesystem::create_directory(outputDir);")
                .AppendLine()
                .AppendLine("\t\tstd::string dirPath;")
                .AppendLine("\t\tstd::ofstream file;")
                .AppendLine();

            var resources = GetResources(embeddedHeaderFilePath);
            foreach (var resource in resources)
            {
                code.AppendLine($"\t\tstd::cout << \"Extracting the \\\"\" << embedded::{resource.FileName} << \"\\\" resource file.\" << std::endl;");

                code.AppendLine($"\t\tif (getDirectory(embedded::{resource.FileName}, dirPath))")
                    .AppendLine("\t\t\tstd::filesystem::create_directory(outputDir + \"/\" + dirPath);");

                switch (resource.Type)
                {
                    case Resource.ResourceType.ASCII:
                        code.AppendLine($"\t\tfile.open(outputDir + \"/\" + embedded::{resource.FileName});");
                        code.AppendLine($"\t\tfile << embedded::{resource.ResourceName};");
                        code.AppendLine("\t\tfile.close();");
                        break;
                    case Resource.ResourceType.Binary:
                        code.AppendLine($"\t\tfile.open(outputDir + \"/\" + embedded::{resource.FileName}, std::ios::out | std::ios::binary);");
                        code.AppendLine($"\t\tfile.write((char*)&embedded::{resource.ResourceName}[0], embedded::{resource.SizeName});");
                        code.AppendLine("\t\tfile.close();");
                        break;
                }
                code.AppendLine();
            }

            code.AppendLine("\t}")
                .AppendLine("")
                .AppendLine("}")
                .AppendLine("")
                .AppendLine("#endif");

            File.WriteAllText(embeddedFileExtractorFilePath, code.ToString());
        }
    }
}
