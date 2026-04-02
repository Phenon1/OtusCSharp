using System;

namespace OtusCSharpModels  
{
 

    internal class ProgramHW3
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            SimpleStore simple = new SimpleStore();
            simple.Get("xz");
            ReadOnlySpan<char> span = "GET user:1".AsSpan();
            var commandKeyValue = CommandParser.Parse(span);

        }
    }
}