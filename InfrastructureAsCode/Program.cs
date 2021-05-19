using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InfrastructureAsCode
{
    sealed class Program
    {
        public static string ACCOUNT => System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT");
        public static string REGION => System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION");

        public static void Main(string[] args)
        {
            var app = new App();
            var env = new Amazon.CDK.Environment
            {
                Account = ACCOUNT,
                Region = REGION
            };

            new AwsBatchPlaygroundStack(app, "AwsBatchPlaygroundStack", new StackProps
            {
                Env = env
            });

            new AwsStepFunctionStack(app, "AwsStepFunctionStack", new StackProps
            {
                Env = env
            });

            app.Synth();
        }
    }
}
