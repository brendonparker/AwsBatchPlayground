using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Text;
using Lambda = Amazon.CDK.AWS.Lambda;
using DDB = Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;

namespace InfrastructureAsCode
{
    class AwsStepFunctionStack : Stack
    {
        const string TABLE_NAME = "JobTable";

        public AwsStepFunctionStack(Constructs.Construct scope = null, string id = null, IStackProps props = null) : base(scope, id, props)
        {
            var lambdaJobCheck = new Lambda.Function(this, "LambdaJobCheck", new Lambda.FunctionProps
            {
                FunctionName = "JobCheck",
                Runtime = Lambda.Runtime.DOTNET_CORE_3_1,
                MemorySize = 1024,
                Code = Lambda.Code.FromAsset("./LambdaSource/JobCheck"),
                Handler = "LambdaJobCheck::LambdaJobCheck.Function::FunctionHandlerAsync",
                Timeout = Duration.Seconds(30),
                Environment = new Dictionary<string, string>
                {
                    { "TABLE_NAME", TABLE_NAME }
                },
                Role = new Role(this, "LambdaJobCheckRole", new RoleProps
                {
                    RoleName = "LambdaJobCheckRole",
                    AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
                    ManagedPolicies = new [] 
                    { 
                        ManagedPolicy.FromManagedPolicyArn(this, "AWSLambdaDynamoDBExecutionRole", "arn:aws:iam::aws:policy/service-role/AWSLambdaDynamoDBExecutionRole") 
                    }
                })
            });

            var table = new DDB.Table(this, "JobTable", new DDB.TableProps
            {
                PartitionKey = new DDB.Attribute
                {
                    Name = "JobKey",
                    Type = DDB.AttributeType.STRING
                },
                TableName = TABLE_NAME
            });
        }
    }
}
