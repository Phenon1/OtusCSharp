namespace Tests;
using OtusCSharpModels;

public class SimpleStoreTests
{
    [Fact]
    public void MultiTaskTest()
    {
        UserProfile test1 =  new UserProfile("test1Name");
        test1.CreatedOn = DateTime.Now;
        test1.Id = 1;
        UserProfile test2 = new UserProfile("test2Name");

        using SimpleStore simpleStore = new SimpleStore();
        Task task1 = Task.Run(() =>
        {
            simpleStore.Set("3", test1);
            simpleStore.Get("1");
        });

        Task task2 = Task.Run(() =>
        {
            simpleStore.Set("2", test2);
            simpleStore.Get("1");
            simpleStore.Delete("1");
        });

        Task task3 = Task.Run(() =>
        {
            simpleStore.Set("1", test1);

            var profile = simpleStore.Get("1");
           
            Assert.NotNull(profile);
            Assert.Equal(test1.Username, profile.Username);

            simpleStore.Delete("1");
            profile = simpleStore.Get("1");
            Assert.Null(profile);

            simpleStore.Delete("3");
        });

        Task task4 = Task.Run(() =>
        {
            simpleStore.Set("3", test2);
            simpleStore.Get("3");
            
        });

        Task.WaitAll(new Task[4] { task1, task2, task3, task4 });

        var statistics = simpleStore.GetStatistics();
        Assert.Equal(4, statistics.Item1);
        Assert.Equal(5, statistics.Item2);
        Assert.Equal(3, statistics.Item3);



    }
   
}