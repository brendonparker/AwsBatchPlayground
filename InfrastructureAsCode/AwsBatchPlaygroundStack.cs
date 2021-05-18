using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Ecr.Assets;
using Batch = Amazon.CDK.AWS.Batch;
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
                Directory = "./DownloadDataBatchApp"
            });

            var computeEnvironment = new Batch.ComputeEnvironment(this, "computeenv", new Batch.ComputeEnvironmentProps
            {
                ComputeEnvironmentName = "SampleComputeEnvironment",
                Enabled = true,
                ComputeResources = new Batch.ComputeResources
                {
                    Vpc = vpc
                }
            });

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

            var jobDefinition = new Batch.JobDefinition(this, "jobdef", new Batch.JobDefinitionProps
            {
                RetryAttempts = 1,
                JobDefinitionName = "SampleJobDefinition",
                Timeout = Duration.Hours(1),
                Container = new Batch.JobDefinitionContainer
                {
                    Image = Amazon.CDK.AWS.ECS.ContainerImage.FromDockerImageAsset(imageAsset),
                    MemoryLimitMiB = 256,
                    Command = new []
                    {
                        "dotnet",
                        "DownloadDataBatchApp.dll"
                    }
                }
            });
        }
    }
}
