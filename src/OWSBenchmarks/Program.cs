using BenchmarkDotNet.Running;
using OWSBenchmarks;
using System;

namespace OWSBenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ResponseBenchmarks>();
        }
    }
}
