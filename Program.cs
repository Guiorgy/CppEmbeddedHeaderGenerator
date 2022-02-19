namespace CppEmbeddedHeaderGenerator
{
    public static class Program
    {
        static void Main(string[] args)
        {
            EmbeddedHeaderGenerator.Generate(@"..\..\..\Embedded", null, @"..\..\..\Output");
            EmbeddedFileExtractorGenerator.Generate(@"..\..\..\Output");
        }
    }
}
