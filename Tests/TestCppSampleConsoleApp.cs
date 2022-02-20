using GitignoreParserNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Tests
{
    /**
     *  Before running the tests, make sure to build and run the projects in the following order:
     *  - CppEmbeddedHeaderGenerator
     *  - SampleConsoleApplication
     */
    [TestClass]
    public class TestCppSampleConsoleApp
    {
        static readonly char directorySeparator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? '\\' : '/';
        static readonly string embeddedDirectoryPath = $"..{directorySeparator}..{directorySeparator}..{directorySeparator}..{directorySeparator}Embedded";
        static readonly string embeddedHeaderFilePath = $"..{directorySeparator}..{directorySeparator}..{directorySeparator}..{directorySeparator}Output{directorySeparator}embedded.h";
#if DEBUG
        static readonly string cppSampleAppOutputPath = $"..{directorySeparator}..{directorySeparator}..{directorySeparator}SampleConsoleApplication{directorySeparator}bin{directorySeparator}x64{directorySeparator}Debug";
#else
        static readonly string cppSampleAppOutputPath = $"..{directorySeparator}..{directorySeparator}..{directorySeparator}SampleConsoleApplication{directorySeparator}bin{directorySeparator}x64{directorySeparator}Release";
#endif

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

        public TestCppSampleConsoleApp()
        {
            var files = ListFilePaths(embeddedDirectoryPath);

            var enbeedignoreFile = new FileInfo(Path.Combine(embeddedDirectoryPath, ".embedignore"));
            string enbeedignore = File.ReadAllText(enbeedignoreFile.FullName, Encoding.UTF8);
            var parser = new GitignoreParser(enbeedignore);

            DirectoryInfo enbeedDir = new(embeddedDirectoryPath);
            int enbeedDirPathLen = enbeedDir.FullName.Length + 1;

            var embedded = files.Where(file => parser.Accepts(file)).Select(path => path[enbeedDirPathLen..]).ToList();
            embedded.Remove(enbeedignoreFile.Name);

            embeddedFiles = embedded.Select(file =>
            {
                var name = file.Contains(directorySeparator) ? file[(file.LastIndexOf(directorySeparator) + 1)..] : file;
                if (name.StartsWith("ascii_")) name = name[6..];
                return (file, (file.Contains(directorySeparator) ? file[..(file.LastIndexOf(directorySeparator) + 1)] : "") + name);
            }).ToList();

            DirectoryInfo cppSampleAppDir = new(cppSampleAppOutputPath);
            int cppSampleAppDirPathLen = cppSampleAppDir.FullName.Length + 1;
            extractedFiles = ListFilePaths(cppSampleAppOutputPath).Select(path => path[cppSampleAppDirPathLen..]).ToList();
        }

        private readonly List<(string original, string extracted)> embeddedFiles;
        private readonly List<string> extractedFiles;

        [TestMethod]
        public void TestEmbeddedHeadedFileContainsAllFileNames()
        {
            if (embeddedFiles.Count == 0) Assert.Inconclusive("No files to embedded. Stopping the test!");

            Assert.IsTrue(File.Exists(embeddedHeaderFilePath), "The \"embedded.h\" header file not found!");
            string embeddedHeaderText = File.ReadAllText(embeddedHeaderFilePath);

            foreach (var (original, extracted) in embeddedFiles)
                Assert.IsTrue(embeddedHeaderText.Contains($"= std::string_view(\"{extracted.Replace(directorySeparator, '/')}\");"), $"\"{original}\" file wasn't embedded!");
        }

        [TestMethod]
        public void TestAllFilesWereExtracted()
        {
            if (embeddedFiles.Count == 0) Assert.Inconclusive("No files to embedded. Stopping the test!");

            foreach (var (original, extracted) in embeddedFiles)
                Assert.IsTrue(extractedFiles.Contains(extracted), $"\"{original}\" file wasn't extracted!");
        }

        [TestMethod]
        public void TestExtractedFileChecksums()
        {
            if (embeddedFiles.Count == 0) Assert.Inconclusive("No files to embedded. Stopping the test!");

            using var md5 = MD5.Create();

            foreach (var (original, extracted) in embeddedFiles)
            {
                var extractedFilePath = Path.Combine(cppSampleAppOutputPath, extracted);
                Assert.IsTrue(File.Exists(extractedFilePath), $"\"{original}\" file wasn't extracted!");

                using var originalStream = File.OpenRead(Path.Combine(embeddedDirectoryPath, original));
                using var extractedStream = File.OpenRead(extractedFilePath);

                var originalChecksum = Encoding.Default.GetString(md5.ComputeHash(originalStream));
                var extractedChecksum = Encoding.Default.GetString(md5.ComputeHash(extractedStream));

                Assert.AreEqual(originalChecksum, extractedChecksum, $"The checksums for the \"{original}\" file don't match!");
            }
        }
    }
}