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
        public ReadOnlySpan<string> Command { init; get; }
        public ReadOnlySpan<string> Key { init; get; }
        public ReadOnlySpan<string> Value { init; get; }
    }

    public static class CommandParser
    {
        public static CommandKeyValue Parse(ReadOnlySpan<string> strings)
        {
            return new CommandKeyValue()
            {
                Command =
            };
        }
    }
    internal class ProgramHW3
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            SimpleStore simple = new SimpleStore();
            simple.Get("xz");
        }
    }
}