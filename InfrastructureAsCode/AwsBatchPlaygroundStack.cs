using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Ecr.Assets;
using System.Collections.Generic;
using Batch = Amazon.CDK.AWS.Batch;
using IAM = Amazon.CDK.AWS.IAM;

namespace AwsBatchPlayground
{
    public class AwsBatchPlaygroundStack : Stack
    {
        internal AwsBatchPlaygroundStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var vpc = Vpc.FromLookup(this, "vpc", new VpcLookupOptions
            {
                IsDefault = true
            });


            var imageAsset = new DockerImageAsset(this, "dkrimgasset", new DockerImageAssetProps
            {
#if DEBUG
                Directory = @"C:\DEV\deleteme\AwsBatchPlayground\DownloadDataBatchApp"
#else
                Directory = "./DownloadDataBatchApp"
#endif
            });

            var computeEnvironment = new Batch.ComputeEnvironment(this, "computeenv", new Batch.ComputeEnvironmentProps
            {
                ComputeEnvironmentName = "SampleComputeEnvironment",
                Enabled = true,
                ComputeResources = new Batch.ComputeResources
                {
                    Vpc = vpc
                }
            }).UseFargate();

            var jobQueue = new Batch.JobQueue(this, "jobqueue", new Batch.JobQueueProps
            {
                Enabled = true,
                JobQueueName = "SampleJobQueue",
                Priority = 1,
                ComputeEnvironments = new[]
                {
                    new Batch.JobQueueComputeEnvironment
                    {
                        ComputeEnvironment = computeEnvironment,
                        Order = 1
                    }
                }
            });

            var executionRole = new IAM.Role(this, "execrole", new IAM.RoleProps
            {
                RoleName = "SampleJobRole",
                AssumedBy = new IAM.ServicePrincipal("ecs-tasks.amazonaws.com"),
                InlinePolicies = new Dictionary<string, IAM.PolicyDocument>
                {
                    {
                        "AmazonECSTaskExecutionRolePolicy",
                        new IAM.PolicyDocument(new IAM.PolicyDocumentProps
                        {
                            Statements = new []
                            {
                                new IAM.PolicyStatement(new IAM.PolicyStatementProps
                                {
                                    Actions = new[] {
                                        "ecr:GetAuthorizationToken",
                                        "ecr:BatchCheckLayerAvailability",
                                        "ecr:GetDownloadUrlForLayer",
                                        "ecr:BatchGetImage",
                                        "logs:CreateLogStream",
                                        "logs:PutLogEvents"
                                    },
                                    Effect = IAM.Effect.ALLOW,
                                    Resources = new [] { "*" }
                                })
                            }
                        })
                    }
                }
            });

            var jobDefinition = new Batch.CfnJobDefinition(this, "jobdef2", new Batch.CfnJobDefinitionProps
            {
                Type = "container",
                JobDefinitionName = "SampleJobDefinition",
                PlatformCapabilities = new[] { "FARGATE" },
                Timeout = new Batch.CfnJobDefinition.TimeoutProperty
                {
                    AttemptDurationSeconds = 3600
                },
                ContainerProperties = new Batch.CfnJobDefinition.ContainerPropertiesProperty
                {
                    ExecutionRoleArn = executionRole.RoleArn,
                    Image = imageAsset.ImageUri,
                    NetworkConfiguration = new Batch.CfnJobDefinition.NetworkConfigurationProperty
                    {
                        // For a job that is running on Fargate resources in a private subnet to send 
                        // outbound traffic to the internet (for example, to pull container images), 
                        // the private subnet requires a NAT gateway be attached to route requests to the internet.
                        AssignPublicIp = "ENABLED"
                    },
                    ResourceRequirements = new[]
                    {
                        new Batch.CfnJobDefinition.ResourceRequirementProperty
                        {
                            Type = "MEMORY",
                            Value = "512"
                        },
                        new Batch.CfnJobDefinition.ResourceRequirementProperty
                        {
                            Type = "VCPU",
                            Value = "0.25"
                        }
                    }
                }
            });
        }
    }
}
