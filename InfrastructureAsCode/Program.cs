using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AwsBatchPlayground
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new AwsBatchPlaygroundStack(app, "AwsBatchPlaygroundStack", new StackProps());

            app.Synth();
        }
    }
}
