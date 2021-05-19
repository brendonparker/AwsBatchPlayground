using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using System;
using System.Collections.Generic;
using System.Text;
using Batch = Amazon.CDK.AWS.Batch;
using IAM = Amazon.CDK.AWS.IAM;

namespace AwsBatchPlayground.CustomConstructs
{
    internal class FargateComputeEnvironment : Construct
    {
        private Batch.CfnComputeEnvironment computeEnv;

        public FargateComputeEnvironment(Constructs.Construct scope, string id, FargateComputeEnvironmentProps props) : base(scope, id)
        {
            if (props.Vpc == null) throw new ArgumentNullException(nameof(props.Vpc));
            if (props.SecurityGroup == null) throw new ArgumentNullException(nameof(props.SecurityGroup));
            if (props.Subnets == null) throw new ArgumentNullException(nameof(props.Subnets));
            if (props.ComputeEnvironmentName == null) throw new ArgumentNullException(nameof(props.ComputeEnvironmentName));

            computeEnv = new Batch.CfnComputeEnvironment(this, "0", new Batch.CfnComputeEnvironmentProps
            {
                ComputeEnvironmentName = props.ComputeEnvironmentName,
                State = props.Enabled ? "ENABLED" : "DISABLED",
                Type = "MANAGED",
                ServiceRole = (props.ServiceRole ?? BuildServiceRole()).RoleArn,
                ComputeResources = new Batch.CfnComputeEnvironment.ComputeResourcesProperty
                {
                    Type = "FARGATE",
                    MaxvCpus = 256,
                    SecurityGroupIds = new[] { props.SecurityGroup.SecurityGroupId },
                    Subnets = props.Subnets
                }
            });
        }

        public string Ref => computeEnv.Ref;

        private IAM.Role BuildServiceRole() =>
            new IAM.Role(this, "ServiceRole", new IAM.RoleProps
            {
                ManagedPolicies = new[]
                {
                    IAM.ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSBatchServiceRole")
                },
                AssumedBy = new IAM.ServicePrincipal("batch.amazonaws.com")
            });
    }

    class FargateComputeEnvironmentProps
    {
        public IVpc Vpc { get; set; }
        public string[] Subnets { get; set; }
        public ISecurityGroup SecurityGroup { get; set; }
        public string ComputeEnvironmentName { get; set; }
        public bool Enabled { get; set; } = true;
        public IAM.IRole ServiceRole { get; set; }
    }
}
