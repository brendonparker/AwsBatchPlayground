using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Text;
using Batch = Amazon.CDK.AWS.Batch;
using Lambda = Amazon.CDK.AWS.Lambda;
using DDB = Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using SF = Amazon.CDK.AWS.StepFunctions;

namespace InfrastructureAsCode
{
    class AwsStepFunctionStack : Stack
    {
        const string TABLE_NAME = "JobTable";

        public AwsStepFunctionStack(Constructs.Construct scope = null, string id = null, IStackProps props = null) : base(scope, id, props)
        {
            var table = new DDB.Table(this, "JobTable", new DDB.TableProps
            {
                PartitionKey = new DDB.Attribute
                {
                    Name = "JobKey",
                    Type = DDB.AttributeType.STRING
                },
                TableName = TABLE_NAME
            });

            var dynamoAccessPolicyDocument = new PolicyDocument(new PolicyDocumentProps
            {
                Statements = new[]
                {
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Effect = Effect.ALLOW,
                        Actions = new []
                        {
                            "dynamodb:DescribeTable",
                            "dynamodb:GetItem",
                            "dynamodb:Query",
                            "dynamodb:Scan",
                            "dynamodb:PutItem",
                            "dynamodb:DeleteItem",
                        },
                        Resources = new [] { table.TableArn }
                    })
                }
            });

            var lambdaRole = new Role(this, "LambdaRole", new RoleProps
            {
                RoleName = "StepFunctionRole",
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
                ManagedPolicies = new[]
                        {
                        ManagedPolicy.FromManagedPolicyArn(this, "AWSLambdaDynamoDBExecutionRole", "arn:aws:iam::aws:policy/service-role/AWSLambdaDynamoDBExecutionRole")
                    },
                InlinePolicies = new Dictionary<string, PolicyDocument>
                    {
                        { "DynamoDBStuff", dynamoAccessPolicyDocument }
                    }
            });

            var lambdaDuplicateCheck = new Lambda.Function(this, "LambdaDuplicateCheck", new Lambda.FunctionProps
            {
                FunctionName = "Step-DuplicateCheck",
                Runtime = Lambda.Runtime.DOTNET_CORE_3_1,
                MemorySize = 1024,
                Code = Lambda.Code.FromAsset("./LambdaJobCheck/bin/Release/netcoreapp3.1/linux-x64"),
                Handler = "LambdaJobCheck::LambdaJobCheck.Function::CheckForDuplicateJobAsync",
                Timeout = Duration.Seconds(30),
                Environment = new Dictionary<string, string>
                {
                    { "TABLE_NAME", TABLE_NAME }
                },
                Role = lambdaRole
            });

            var lambdaCleanup = new Lambda.Function(this, "LambdaCleanup", new Lambda.FunctionProps
            {
                FunctionName = "Step-Cleanup",
                Runtime = Lambda.Runtime.DOTNET_CORE_3_1,
                MemorySize = 1024,
                Code = Lambda.Code.FromAsset("./LambdaJobCheck/bin/Release/netcoreapp3.1/linux-x64"),
                Handler = "LambdaJobCheck::LambdaJobCheck.Function::CleanupJobAsync",
                Timeout = Duration.Seconds(30),
                Environment = new Dictionary<string, string>
                {
                    { "TABLE_NAME", TABLE_NAME }
                },
                Role = lambdaRole
            });

            //var duplicateCheck = new SF.Tasks.LambdaInvoke(this, "DuplicateCheck", new SF.Tasks.LambdaInvokeProps
            //{
            //    LambdaFunction = lambdaDuplicateCheck,
            //    OutputPath = "$.Payload"
            //});

            var duplicateCheck = new SF.Tasks.DynamoPutItem(this, "DynamoDuplicateCheck", new SF.Tasks.DynamoPutItemProps
            {
                Table = table,
                Item = new Dictionary<string, SF.Tasks.DynamoAttributeValue>
                {
                    { "JobKey", SF.Tasks.DynamoAttributeValue.FromString(SF.JsonPath.StringAt("$.JobKey")) },
                    { "CustomerId", SF.Tasks.DynamoAttributeValue.FromString(SF.JsonPath.StringAt("$.CustomerId")) }
                },
                OutputPath = "$.Payload",
                ConditionExpression = "attribute_not_exists(#k)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#k", "JobKey" }
                }
            });

            var startJob = new SF.Tasks.BatchSubmitJob(this, "SubmitJob", new SF.Tasks.BatchSubmitJobProps
            {
                JobName = "SampleJob",
                JobDefinitionArn = Batch.JobDefinition.FromJobDefinitionName(this, "JobDefinition", "SampleJob").JobDefinitionArn,
                JobQueueArn = $"arn:aws:batch:{Program.REGION}:{Program.ACCOUNT}:job-queue/SampleJobQueue",
                ResultPath = SF.JsonPath.DISCARD,
                Payload = SF.TaskInput.FromObject(new Dictionary<string, object>
                {
                    // Upstream "JobAlreadyRunning" is getting added to the state as a bool.
                    // This blowsup since apparently can only pass objects with strings as a payload :/
                    { "CustomerId", SF.JsonPath.StringAt("$.CustomerId") },
                    { "JobKey", SF.JsonPath.StringAt("$.JobKey") }
                })
            });

            var cleanup = new SF.Tasks.LambdaInvoke(this, "Cleanup", new SF.Tasks.LambdaInvokeProps
            {
                LambdaFunction = lambdaCleanup
            });

            var stateAlreadyRunning = new SF.Succeed(this, "JobAlreadyRunning");

            var definition = duplicateCheck
                .Next(new SF.Choice(this, "JobRunningChoice")
                    .When(SF.Condition.BooleanEquals("$.JobAlreadyRunning", true), stateAlreadyRunning)
                    .When(SF.Condition.BooleanEquals("$.JobAlreadyRunning", false), startJob.Next(cleanup)));

            new SF.StateMachine(this, "StateMachine", new SF.StateMachineProps
            {
                StateMachineType = SF.StateMachineType.STANDARD,
                StateMachineName = "SampleStateMachine",
                Timeout = Duration.Hours(1),
                Definition = definition
            });
        }
    }
}
