using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Ecr.Assets;
using AwsBatchPlayground.CustomConstructs;
using System.Collections.Generic;
using System.Linq;
using Batch = Amazon.CDK.AWS.Batch;

namespace AwsBatchPlayground
{
    public class AwsBatchPlaygroundStack : Stack
    {
        internal AwsBatchPlaygroundStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var vpc = Vpc.FromLookup(this, "VPC", new VpcLookupOptions
            {
                IsDefault = true
            });

            var sg = new SecurityGroup(this, "SecurityGroup", new SecurityGroupProps
            {
                Vpc = vpc,
                SecurityGroupName = "SampleComputeEnvironmentSg"
            });

            var computeEnv = new FargateComputeEnvironment(this, "FargateComputeEnv", new FargateComputeEnvironmentProps
            {
                Vpc = vpc,
                SecurityGroup = sg,
                ComputeEnvironmentName = "SampleComputeEnvironment",
                Subnets = vpc.PublicSubnets.Select(x => x.SubnetId).Take(2).ToArray()
            });

            var jobQueue = new Batch.CfnJobQueue(this, "JobQueue", new Batch.CfnJobQueueProps
            {
                State = "ENABLED",
                JobQueueName = "SampleJobQueue",
                Priority = 1,
                ComputeEnvironmentOrder = new []
                {
                    new Batch.CfnJobQueue.ComputeEnvironmentOrderProperty
                    {
                        ComputeEnvironment = computeEnv.Ref,
                        Order = 1
                    }
                }
            });

            var imageAsset = new DockerImageAsset(this, "DkrImgAsset", new DockerImageAssetProps
            {
                Directory = "./DownloadDataBatchApp"
            });

            var jobDefinition = new FargateJobDefinition(this, "FargateJobDef", new FargateJobDefinitionProps
            {
                JobDefinitionName = "SampleJob",
                ImageUri = imageAsset.ImageUri
            });
        }
    }
}
