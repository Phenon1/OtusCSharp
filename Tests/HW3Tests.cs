namespace Tests;
using OtusCSharpModels;
using System.Text;

public class HW3Tests
{
    [Theory]
    [InlineData("SET user:1 data", "SET", "user:1", "data")]
    [InlineData("SET    user:1 data", "SET", "user:1", "data")]
    [InlineData("GET user:1", "GET", "user:1", "")]
    [InlineData("GET  user:1", "GET", "user:1", "")]
    public void CommandParserOkTest(string fullCommand, string command,string key,string value)
    {
        ReadOnlySpan<byte> span = Encoding.UTF8.GetBytes(fullCommand);
        var commandKeyValue = CommandParser.Parse(span);

        Assert.Equal(
           command,
           Encoding.UTF8.GetString(commandKeyValue.Command));

        Assert.Equal(
            key,
            Encoding.UTF8.GetString(commandKeyValue.Key));

        Assert.Equal(
            value,
            Encoding.UTF8.GetString(commandKeyValue.Value));
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("GET  ")]
    public void CommandParserExceptionTest(string fullCommand)
    {
        Assert.Throws<OtusCSharpModels.ComandException>(() => CommandParser.Parse(Encoding.UTF8.GetBytes(fullCommand)));
       
    }
}