using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Text;
using Batch = Amazon.CDK.AWS.Batch;
using IAM = Amazon.CDK.AWS.IAM;

namespace InfrastructureAsCode.CustomConstructs
{
    internal class FargateJobDefinition : Construct
    {
        public FargateJobDefinition(Constructs.Construct scope, string id, FargateJobDefinitionProps props) : base(scope, id)
        {
            if (props.JobDefinitionName == null) throw new ArgumentNullException(nameof(props.JobDefinitionName));
            if (props.ImageUri == null) throw new ArgumentNullException(nameof(props.ImageUri));

            new Batch.CfnJobDefinition(this, "0", new Batch.CfnJobDefinitionProps
            {
                Type = "container",
                JobDefinitionName = props.JobDefinitionName,
                PlatformCapabilities = new[] { "FARGATE" },
                Timeout = props.Timeout == null ? null : new Batch.CfnJobDefinition.TimeoutProperty
                {
                    AttemptDurationSeconds = props.Timeout.ToSeconds()
                },
                Parameters = props.Parameters,
                ContainerProperties = new Batch.CfnJobDefinition.ContainerPropertiesProperty
                {
                    Command = props.Command,
                    ExecutionRoleArn = (props.ExecutionRole ?? BuildJobRole(props.JobDefinitionName)).RoleArn,
                    Image = props.ImageUri,
                    NetworkConfiguration = props.AssignPublicIp ? new Batch.CfnJobDefinition.NetworkConfigurationProperty
                    {
                        // For a job that is running on Fargate resources in a private subnet to send 
                        // outbound traffic to the internet (for example, to pull container images), 
                        // the private subnet requires a NAT gateway be attached to route requests to the internet.
                        AssignPublicIp = "ENABLED"
                    } : null,
                    ResourceRequirements = new[]
                    {
                        new Batch.CfnJobDefinition.ResourceRequirementProperty
                        {
                            Type = "MEMORY",
                            Value = props.Memory.ToString()
                        },
                        new Batch.CfnJobDefinition.ResourceRequirementProperty
                        {
                            Type = "VCPU",
                            Value = props.vCPU.ToString()
                        }
                    }
                }
            });
        }

        private IAM.IRole BuildJobRole(string jobName) =>
            new IAM.Role(this, "Role", new IAM.RoleProps
            {
                RoleName = jobName + "Role",
                AssumedBy = new IAM.ServicePrincipal("ecs-tasks.amazonaws.com"),
                ManagedPolicies = new[] { IAM.ManagedPolicy.FromManagedPolicyArn(this, "RolePolicy", "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy") }
            });
    }

    class FargateJobDefinitionProps
    {
        public string JobDefinitionName { get; set; }
        public Duration Timeout { get; set; }
        public IAM.IRole ExecutionRole { get; set; }
        public int Memory { get; set; } = 512;
        public decimal vCPU { get; set; } = 0.25M;
        public bool AssignPublicIp { get; set; } = true;
        public string ImageUri { get; set; }
        public string[] Command { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }
}
