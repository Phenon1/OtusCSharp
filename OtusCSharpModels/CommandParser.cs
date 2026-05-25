using System;

namespace OtusCSharpModels
{
    public ref struct CommandKeyValue
    {
        public ReadOnlySpan<byte> Command { init; get; }
        public ReadOnlySpan<byte> Key { init; get; }
        public ReadOnlySpan<byte> Value { init; get; }

        public void Print()
        {
            Console.WriteLine(
                $"Command:{AsString(Command)} " +
                $"Key:{AsString(Key)} " +
                $"Value:{AsString(Value)}");
        }

        private static string AsString(ReadOnlySpan<byte> span)
        {
            return System.Text.Encoding.UTF8.GetString(span);
        }
    }

    public static class CommandParser
    {
        public static CommandKeyValue Parse(ReadOnlySpan<byte> span)
        {
            span = TrimStart(span);

            int space = span.IndexOf((byte)' ');

            if (space == -1)
                throw new ComandException();

            ReadOnlySpan<byte> command = span[..space];

            span = span[space..];
            span = TrimStart(span);

            if (span.Length == 0)
                throw new ComandException();

            space = span.IndexOf((byte)' ');

            if (space == -1)
                space = span.Length;

            ReadOnlySpan<byte> key = span[..space];

            ReadOnlySpan<byte> value = ReadOnlySpan<byte>.Empty;

            if (space < span.Length)
            {
                span = span[space..];

                if (span.Length > 0 && span[0] == (byte)' ')
                    span = span[1..];

                value = span;
            }

            return new CommandKeyValue
            {
                Command = command,
                Key = key,
                Value = value
            };
        }

        private static ReadOnlySpan<byte> TrimStart(ReadOnlySpan<byte> span)
        {
            while (span.Length > 0)
            {
                if (span[0] == (byte)' ')
                    span = span[1..];
                else
                    return span;
            }

            return span;
        }
    }

    public class ComandException : Exception
    {
    }
}