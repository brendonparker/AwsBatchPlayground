using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LambdaJobCheck
{
    public class Function
    {
        private readonly AmazonDynamoDBClient ddbClient;
        private readonly Table table;
        private string TABLE_NAME => Environment.GetEnvironmentVariable("TABLE_NAME") ?? "JobTable";
        private const string FIELD_JOB_KEY = "JobKey";
        private const string FIELD_CUSTOMER_ID = "CustomerId";

        public Function()
        {
            ddbClient = new AmazonDynamoDBClient();
            table = Table.LoadTable(ddbClient, TABLE_NAME);
        }

        public async Task<State> CheckForDuplicateJobAsync(State state, ILambdaContext context)
        {
            var document = new Document();
            document[FIELD_JOB_KEY] = state.JobKey;
            document[FIELD_CUSTOMER_ID] = state.CustomerId;

            try
            {
                var res = await table.PutItemAsync(document, new PutItemOperationConfig
                {
                    ConditionalExpression = new Expression
                    {
                        ExpressionStatement = "attribute_not_exists(#k)",
                        ExpressionAttributeNames = new Dictionary<string, string>
                        {
                            { "#k", FIELD_JOB_KEY }
                        }
                    }
                });
            }
            catch (ConditionalCheckFailedException ex)
            {
                state.JobAlreadyRunning = true;
            }
            return state;
        }

        public async Task<State> CleanupJobAsync(State state, ILambdaContext context)
        {
            await ddbClient.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = TABLE_NAME,
                Key = new Dictionary<string, AttributeValue>
                {
                    { FIELD_JOB_KEY, new AttributeValue(state.JobKey) }
                }
            });

            return state;
        }
    }
}
