namespace Tests;
using OtusCSharpHW3;

public class SimpleStoreTests
{
    [Fact]
    public void MultiTaskTest()
    {
        using SimpleStore simpleStore = new SimpleStore();
        Task task1 = Task.Run(() =>
        {
            simpleStore.Set("3", new byte[2] { 0, 1 });
            simpleStore.Get("1");
        });

        Task task2 = Task.Run(() =>
        {
            simpleStore.Set("2", new byte[1] { 4 });
            simpleStore.Get("1");
            simpleStore.Delete("1");
        });

        Task task3 = Task.Run(() =>
        {
            simpleStore.Set("1", new byte[2] { 0, 1 });

            var bytes = simpleStore.Get("1");
           
            Assert.NotNull(bytes);
            Assert.Equal(1, bytes[1]);

            simpleStore.Delete("1");
            bytes = simpleStore.Get("1");
            Assert.Null(bytes);

            simpleStore.Delete("3");
        });

        Task task4 = Task.Run(() =>
        {
            simpleStore.Set("3", new byte[2] { 1, 0 });
            simpleStore.Get("3");
            
        });

        Task.WaitAll(new Task[4] { task1, task2, task3, task4 });

        var statistics = simpleStore.GetStatistics();
        Assert.Equal(4, statistics.Item1);
        Assert.Equal(5, statistics.Item2);
        Assert.Equal(3, statistics.Item3);



    }
   
}