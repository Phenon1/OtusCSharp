using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtusCSharpModels
{

    public ref struct CommandKeyValue
    {
        public ReadOnlySpan<char> Command { init; get; }
        public ReadOnlySpan<char> Key { init; get; }
        public ReadOnlySpan<char> Value { init; get; }

        public void Print()
        {
            Console.WriteLine($"Command:{Command.ToString()} Key:{Key.ToString()} Value:{Value.ToString()}");
        }
    }

    public static class CommandParser
    {
        public static CommandKeyValue Parse(ReadOnlySpan<char> span)
        {
            span = TrimStart(span);
            int space = span.IndexOf(' ');

            if ( space == -1 )
                throw new ComandException();

            ReadOnlySpan<char> command = span[..(space)];



            if ( space + 1 > span.Length )
                throw new ComandException();

            span = span[(space)..];
            span = TrimStart(span);
            space = span.IndexOf(' ');

            if ( span.Length == 0 )
                throw new ComandException();
            else if ( space == -1 )
                space = span.Length;



            ReadOnlySpan<char> key = span[..(space)];

            ReadOnlySpan<char>  value = new ReadOnlySpan<char>();

            if ( space < span.Length )
            {
                span = span[(space)..];
                span = TrimStart(span);
                value = span;
            }



            return new CommandKeyValue()
            {
                Command = command,
                Key = key,
                Value = value
            };
        }



        private static ReadOnlySpan<char> TrimStart(ReadOnlySpan<char> span)
        {
            while ( span.Length > 0 )
            {
                if ( span[0] == ' ' )
                    span = span[1..];
                else return span;
            }
            return span;
        }

    }

    public class ComandException :Exception { }
}
