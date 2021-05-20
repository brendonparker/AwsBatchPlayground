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

            var duplicateCheck = new SF.Tasks.DynamoPutItem(this, "DuplicateCheck", new SF.Tasks.DynamoPutItemProps
            {
                Table = table,
                Item = new Dictionary<string, SF.Tasks.DynamoAttributeValue>
                {
                    { "JobKey", SF.Tasks.DynamoAttributeValue.FromString(SF.JsonPath.StringAt("$.JobKey")) },
                    { "CustomerId", SF.Tasks.DynamoAttributeValue.FromString(SF.JsonPath.StringAt("$.CustomerId")) }
                },
                ConditionExpression = "attribute_not_exists(#k)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#k", "JobKey" }
                },
                // Keep original Input, tack on output into $.DuplicateCheck
                ResultPath = "$.DuplicateCheck"
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

            var cleanup = new SF.Tasks.DynamoDeleteItem(this, "Cleanup", new SF.Tasks.DynamoDeleteItemProps
            {
                Table = table,
                Key = new Dictionary<string, SF.Tasks.DynamoAttributeValue>
                {
                    { "JobKey", SF.Tasks.DynamoAttributeValue.FromString(SF.JsonPath.StringAt("$.JobKey")) }
                }
            });

            var stateAlreadyRunning = new SF.Succeed(this, "JobAlreadyRunning");

            var definition = duplicateCheck
                .AddCatch(stateAlreadyRunning, new SF.CatchProps
                {
                    Errors = new[] { "DynamoDB.ConditionalCheckFailedException" }
                })
                .Next(startJob)
                .Next(cleanup);

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
