using BenchmarkDotNet.Attributes;
using Dia2Lib;
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

        [Benchmark]
        public UserProfile NewtonsoftJson()
        {
            var json = JsonConvert.SerializeObject(_profile, _newtonsoftSettings);
            return JsonConvert.DeserializeObject<UserProfile>(json)!;
        }

        [Benchmark]
        public UserProfile SystemTextJson()
        {
            var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(_profile, _stjOptions);
            return System.Text.Json.JsonSerializer.Deserialize<UserProfile>(bytes)!;
        }

        [Benchmark(Baseline = true)]
        public UserProfile SourceGenerator()
        {
            using (var ms = new MemoryStream())
            {
                _profile.SerializeToBinary(ms);
                ms.Position = 0; 
                return UserProfile.DeserializeFromBinary(ms);
            }
        }
    }
}
