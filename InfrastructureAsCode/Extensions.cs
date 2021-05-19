using Amazon.CDK.AWS.Batch;
using System;
using System.Collections.Generic;
using System.Text;

namespace AwsBatchPlayground
{
    public static class Extensions
    {
        public static ComputeEnvironment UseFargate(this ComputeEnvironment computeEnvironment)
        {
            var cfnComputeEnvironment = computeEnvironment.Node.DefaultChild as CfnComputeEnvironment;

            cfnComputeEnvironment.AddPropertyOverride("ComputeResources.Type", "FARGATE");
            cfnComputeEnvironment.AddPropertyDeletionOverride("ComputeResources.AllocationStrategy");
            cfnComputeEnvironment.AddPropertyDeletionOverride("ComputeResources.InstanceRole");
            cfnComputeEnvironment.AddPropertyDeletionOverride("ComputeResources.InstanceTypes");
            cfnComputeEnvironment.AddPropertyDeletionOverride("ComputeResources.MinvCpus");

            return computeEnvironment;
        }
    }
}
