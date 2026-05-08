using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace HelloWorldGenerator
{
    [Generator]
    public class HelloWorldGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {

            // Жёстко заданный C#-код, который мы "впрыснем" в компиляцию
            const string source = @"
namespace HelloWorldGenerator
{
    public static partial class HelloFromGenerator
    {
        public static string GetMessage() => ""Hello from Source Generator!"";
    }
}
";

            context.RegisterPostInitializationOutput(ctx =>
            {
                ctx.AddSource(
                    hintName: "HelloFromGenerator.g.cs",
                    SourceText.From(source, Encoding.UTF8));
            });
        }
    }
}
