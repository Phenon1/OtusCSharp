using HelloWorldGenerator;
using System;
using System.Text.Json;

namespace OtusCSharpModels  
{
 

    internal class ProgramHW3
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            var message = HelloFromGenerator.GetMessage();
            SimpleStore simple = new SimpleStore();
            simple.Get("xz");
            ReadOnlySpan<char> span = "GET user:1".AsSpan();
            var commandKeyValue = CommandParser.Parse(span);

            UserProfile userProfile = new UserProfile("asqqweq");
            using (var ms = new MemoryStream())
            {
                userProfile.SerializeToBinary(ms);
                Console.WriteLine("Serialized " + ms.Length + " bytes by Source Generator.");

                UserProfile? userProfileRet = JsonSerializer.Deserialize<UserProfile>(ms.ToArray()); 
            }

        }
    }
}