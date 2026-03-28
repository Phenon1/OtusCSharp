using NBomber.Configuration;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomberTest;
using OtusCSharpModels;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;

namespace NBomber
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            /*
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            TcpBombClient client = new TcpBombClient();
            client.ConnectAsync("127.0.0.1", 8888, tokenSource.Token).Wait();
            client.SetAsync("T1", [1, 0]).Wait();
            client.SetAsync("T2", [1, 1]).Wait();
            client.GetAsync("T2").Wait();

            */


            var scenario = Scenario.Create("TcpServer bomb scenario", async context =>
            {
                string name = context.Random.GetString(['a', 'b', 'c', 'd', 'e', 'f', 'g','h','i','j'], 5);
                int id = context.Random.Next();

                UserProfile userProfile = new UserProfile(name);
                userProfile.CreatedOn = DateTime.Now;
                userProfile.Id = id;

                var step1 = await Step.Run("GetBytes", context, async () =>
                {
                    var bytes = JsonSerializer.SerializeToUtf8Bytes(userProfile);

                    using TcpBombClient client = new TcpBombClient();
                    CancellationTokenSource tokenSource = new CancellationTokenSource();

                    await client.ConnectAsync("127.0.0.1", 8888, tokenSource.Token);
                    await client.SetAsync(name, bytes);
                    bytes = await client.GetAsync(name);
                    UserProfile? userProfileRet = JsonSerializer.Deserialize<UserProfile>(bytes);

                    if (userProfileRet != null && userProfile.Id == userProfileRet.Id)
                        return Response.Ok();
                    else
                        return Response.Fail();
                });

            

                return Response.Ok();
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
            );
           

            NBomberRunner
                .RegisterScenarios(scenario)
                
                .Run();
            Console.ReadLine();
           
        }
    }
}
