using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace PraktikumBenchmark
{
    [MemoryDiagnoser]
    public class SumBench
    {
        private int[] data = Enumerable.Range(1, 1000).ToArray();

        [Benchmark]
        public int ForLoop()
        {
            int s = 0;
            for(int i=0; i<data.Length; i++ ) s += data[i];
            return s;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkRunner.Run<SumBench>();

            BenchmarkRunner.Run<GenerateBinarySerializerBenchmark>();
        }
    }
}
