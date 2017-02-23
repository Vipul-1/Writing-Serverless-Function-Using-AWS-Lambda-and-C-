using System;
using Amazon.Lambda.Core;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.DynamoDBv2.Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using Amazon;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
namespace Registration
{
    public class Function
    {
       private readonly string _accessKey;
       private readonly string _secretKey;
       private readonly string _serviceUrl;
       private const string TableName = "Registration";

        public Function()
        {
            _accessKey = Environment.GetEnvironmentVariable("AccessKey");
            _secretKey = Environment.GetEnvironmentVariable("SecretKey");
            _serviceUrl = Environment.GetEnvironmentVariable("ServiceURL");
        }

        //Create Table if it does not exists
        private async Task CreateTable(IAmazonDynamoDB amazonDynamoDBclient,string tableName)
        {
            //Write Log to Cloud Watch using LambdaLogger.Log Method
            LambdaLogger.Log(string.Format("Creating {0} Table", tableName));

            var tableCollection = await amazonDynamoDBclient.ListTablesAsync();

            if (!tableCollection.TableNames.Contains(tableName))
                await amazonDynamoDBclient.CreateTableAsync(new CreateTableRequest
                {
                    TableName = tableName,
                    KeySchema = new List<KeySchemaElement> {
                        { new KeySchemaElement { AttributeName="Name",  KeyType= KeyType.HASH }},
                        new KeySchemaElement { AttributeName="EmailId",  KeyType= KeyType.RANGE }
                    },
                    AttributeDefinitions = new List<AttributeDefinition> {
                        new AttributeDefinition { AttributeName="Name", AttributeType="S" },
                        new AttributeDefinition { AttributeName ="EmailId",AttributeType="S"}
                 },
                    ProvisionedThroughput = new ProvisionedThroughput
                    {
                        ReadCapacityUnits = 5,
                        WriteCapacityUnits = 5
                    },
                });
        }

        //Function Handler is an entry point to start execution of Lambda Function.
        //It takes Input Data as First Parameter and ObjectContext as Second
        public async Task FunctionHandler(Customer customer, ILambdaContext context)
        {
            //Write Log to Cloud Watch using Console.WriteLline.  
            Console.WriteLine("Execution started for function -  {0} at {1}", 
                                context.FunctionName , DateTime.Now);

            // Create  dynamodb client
            var dynamoDbClient = new AmazonDynamoDBClient(
                new BasicAWSCredentials(_accessKey, _secretKey),
                new AmazonDynamoDBConfig { ServiceURL = _serviceUrl,
                    RegionEndpoint  = RegionEndpoint.APSoutheast2});
            
            //Create Table if it Does Not Exists
            await CreateTable(dynamoDbClient,TableName);

           // Insert record in dynamodbtable
            LambdaLogger.Log("Insert record in the table");
            await dynamoDbClient.PutItemAsync(TableName, new Dictionary<string, AttributeValue>
            {
                { "Name", new AttributeValue(customer.Name) },
                { "EmailId", new AttributeValue(customer.EmailId) },
             });

            //Write Log to cloud watch using context.Logger.Log Method
            context.Logger.Log(string.Format("Finished execution for function -- {0} at {1}",
                               context.FunctionName,DateTime.Now ));
        }
    }
}
