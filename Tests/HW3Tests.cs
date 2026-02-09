namespace Tests;
using OtusCSharpHW3;

public class HW3Tests
{
    [Theory]
    [InlineData("SET user:1 data", "SET", "user:1", "data")]
    [InlineData("SET user:1     data", "SET", "user:1", "data")]
    [InlineData("GET user:1", "GET", "user:1", "")]
    [InlineData("GET   user:1", "GET", "user:1", "")]
    [InlineData("GET   user:1  ", "GET", "user:1", "")]
    [InlineData("  GET   user:1  ", "GET", "user:1", "")]
    public void CommandParserOkTest(string fullCommand, string command,string key,string value)
    {
        ReadOnlySpan<char> span = fullCommand.AsSpan();
        var commandKeyValue = CommandParser.Parse(span);

        Assert.Equal(command, commandKeyValue.Command.ToString());
        Assert.Equal(key, commandKeyValue.Key.ToString());
        Assert.Equal(value, commandKeyValue.Value.ToString());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("GET  ")]
    public void CommandParserExceptionTest(string fullCommand)
    {
        Assert.Throws<OtusCSharpHW3.ComandException>(() => CommandParser.Parse(fullCommand.AsSpan()));
       
    }
}