using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using OtusCSharpModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace PraktikumBenchmark
{
    [MemoryDiagnoser]
    public class GenerateBinarySerializerBenchmark
    {
        private readonly UserProfile _profile = new UserProfile("Test1");

        private readonly JsonSerializerOptions _stjOptions =
            new JsonSerializerOptions(JsonSerializerDefaults.General);

        private readonly JsonSerializerSettings _newtonsoftSettings =
            new JsonSerializerSettings();

        [Benchmark(Baseline = true)]
        public byte[] NewtonsoftJson()
        {
            var json = JsonConvert.SerializeObject(_profile, _newtonsoftSettings);
            return Encoding.UTF8.GetBytes(json);
        }

        [Benchmark]
        public byte[] SystemTextJson()
        {
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(_profile, _stjOptions);
        }

        [Benchmark]
        public void SourceGenerator()
        {
            using (var ms = new MemoryStream())
            {
                _profile.SerializeToBinary(ms);
            }
        }
    }
}
