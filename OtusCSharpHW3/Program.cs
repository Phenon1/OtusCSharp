using System;

namespace OtusCSharpHW3
{
    public class SimpleStore
    {
        private Dictionary<string, byte[]> _store;

        public SimpleStore()
        {
            _store=new Dictionary<string, byte[]>();
        }
        public void Set(string key, byte[] value)
        {
            _store[key]= value;
        }
        public byte[] Get(string key)
        {
            _store.TryGetValue(key, out var value);
            return value;
        }
        public void Delete(string key)
        {
            _store.Remove(key);
        }
    }

    public ref struct CommandKeyValue
    {
        public ReadOnlySpan<char> Command { init; get; }
        public ReadOnlySpan<char> Key { init; get; }
        public ReadOnlySpan<char> Value { init; get; }
    }

    public static class CommandParser
    {
        public static CommandKeyValue Parse(ReadOnlySpan<char> span)
        {
            span = TrimStart(span);
            int space = span.IndexOf(' ');

            if (space == -1)
                throw new ComandException();

            ReadOnlySpan<char> command = span[..(space)];



            if (space + 1 > span.Length)
                throw new ComandException();

            span = span[(space)..];
            span = TrimStart(span);
            space = span.IndexOf(' ');

            if ( span.Length==0 )
                throw new ComandException();
            else if ( space==-1)
                space = span.Length;


            
            ReadOnlySpan<char> key = span[..(space)];

            ReadOnlySpan<char>  value = new ReadOnlySpan<char>();

            if (space < span.Length)
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
            while(span.Length > 0)
            {
                if (span[0] == ' ')
                    span = span[1..];
                else return span;
            }
            return span;
        }

    }

    public class ComandException:Exception{}

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