using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace CppSampleConsoleAppGenerator
{
    public static class Program
    {
        static void Main(string[] args)
        {
            var projectDir = (Directory.GetParent(Environment.CurrentDirectory) ?? Directory.GetParent(Directory.GetCurrentDirectory()))?.Parent?.Parent;
            if (projectDir == null) throw new GenerationException("Couldn't get current project directory!");

            var OutputDir = projectDir?.Parent?.Parent?.GetDirectories().Where(d => d.Name == "Output").ToList();
            if (OutputDir == null || OutputDir.Count != 1) throw new GenerationException("Couldn't get the Output directory!");

            const string embeddedHeaderFileName = "embedded.h";
            string embeddedHeaderFilePath = Path.Combine(OutputDir[0].FullName, embeddedHeaderFileName);

            var scaProjectDir = projectDir?.Parent?.GetDirectories().Where(d => d.Name == "SampleConsoleApplication").ToList();
            if (scaProjectDir == null || scaProjectDir.Count != 1) throw new GenerationException("Couldn't get SampleConsoleApplication project directory!");

            const string scaCodeFileName = "SampleConsoleApplication.cpp";
            string scaCodeFilePath = Path.Combine(scaProjectDir[0].FullName, scaCodeFileName);

            var code = new StringBuilder()
                .AppendLine("// This file uses the <filesystem> header, thus it needs to be compiled with a compiler that suports c++17.")
                .AppendLine("#include \"../../Output/embedded.h\"")
                .AppendLine("#include <iostream>")
                .AppendLine("#include <fstream>")
                .AppendLine("#include <string>")
                .AppendLine("#include <filesystem>")
                .AppendLine()
                .AppendLine("bool getDirectory(std::string const& filePath, std::string& directoryPath)")
                .AppendLine("{")
                .AppendLine("\tstd::string::size_type pos = filePath.find_last_of('/');")
                .AppendLine("\tif (pos != std::string::npos)")
                .AppendLine("\t{")
                .AppendLine("\t\tdirectoryPath = filePath.substr(0, pos);")
                .AppendLine("\t\treturn true;")
                .AppendLine("\t}")
                .AppendLine("\treturn false;")
                .AppendLine("}")
                .AppendLine()
                .AppendLine("int main()")
                .AppendLine("{")
                .AppendLine("\tstd::string dirPath;")
                .AppendLine("\tstd::ofstream file;")
                .AppendLine();

            var resources = GetResources(embeddedHeaderFilePath);
            foreach (var resource in resources)
            {
                code.AppendLine($"\tstd::cout << \"Extracting the \\\"\" << embedded::{resource.FileName} << \"\\\" resource file.\" << std::endl;");

                code
                    .AppendLine($"\tif (getDirectory(embedded::{resource.FileName}, dirPath))")
                    .AppendLine("\tstd::filesystem::create_directory(dirPath);");

                switch (resource.Type)
                {
                    case Resource.ResourceType.ASCII:
                        code.AppendLine($"\tfile.open(embedded::{resource.FileName});");
                        code.AppendLine($"\tfile << embedded::{resource.ResourceName};");
                        code.AppendLine("\tfile.close();");
                        break;
                    case Resource.ResourceType.Binary:
                        code.AppendLine($"\tfile.open(embedded::{resource.FileName}, std::ios::out | std::ios::binary);");
                        code.AppendLine($"\tfile.write((char*)&embedded::{resource.ResourceName}[0], embedded::{resource.SizeName});");
                        code.AppendLine("\tfile.close();");
                        break;
                }
                code.AppendLine();
            }

            code.AppendLine("\treturn 0;").AppendLine("}");

            File.WriteAllText(scaCodeFilePath, code.ToString());
        }

        public static List<Resource> GetResources(string embeddedHeaderFilePath)
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

        public class Resource
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

        [Serializable]
        public class GenerationException : Exception
        {
            public GenerationException()
            {
            }

            public GenerationException(string? message) : base(message)
            {
            }

            public GenerationException(string? message, Exception? innerException) : base(message, innerException)
            {
            }

            protected GenerationException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
    }
}
