# Sample C# application using AWS Batch

This is a demo app of using AWS Batch with fargate containers to process the jobs. It includes a sample CDK project to stand up the bits in AWS.

The `cdk.json` file tells the CDK Toolkit how to execute your app.

It uses the [.NET Core CLI](https://docs.microsoft.com/dotnet/articles/core/) to compile and execute your project.

## Useful commands

* `dotnet build src` compile this app
* `cdk deploy`       deploy this stack to your default AWS account/region
* `cdk diff`         compare deployed stack with current state
* `cdk synth`        emits the synthesized CloudFormation template

Can then run this aws cli command to queue up jobs. Can run this several times.

```
aws batch submit-job --job-name Job001 --job-definition SampleJobDefinition --job-queue SampleJobQueue
```