using Newtonsoft.Json;
using System;
using System.Threading;
using static System.Console;

namespace DownloadDataBatchApp
{
    class Program
    {
        public static void Main(string[] args)
        {
            WriteLine($"args: {JsonConvert.SerializeObject(args)}");
            WriteLine("env stuff: " + JsonConvert.SerializeObject(new
            {
                System.Environment.MachineName,
                System.Environment.OSVersion,
                System.Environment.ProcessorCount,
                System.Environment.Version,
                System.Environment.WorkingSet
            }));
            WriteLine("Starting...");
            Thread.Sleep(10000);
            WriteLine("Hello World!");
        }
    }
}
