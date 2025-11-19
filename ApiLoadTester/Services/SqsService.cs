using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;

namespace ApiLoadTester.Services
{
    public class SqsService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly ILogger _logger;
        private readonly string _queueUrl;

        public SqsService(string queueUrl, ILogger logger)
        {
            _logger = logger;
            _queueUrl = queueUrl;

            // Use FallbackCredentialsFactory which checks credentials in this order:
            // 1. Environment variables (AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY)
            // 2. AWS credentials file (~/.aws/credentials)
            // 3. ECS container credentials (from task role)
            // 4. EC2 instance metadata (instance profile)
            var credentials = FallbackCredentialsFactory.GetCredentials();
            _sqsClient = new AmazonSQSClient(credentials);

            _logger.LogInformation("AWS SQS client initialized successfully using AWS credential chain");
        }

        public async Task SendLoadTestSummaryAsync(LoadTestSummary summary)
        {
            try
            {
                var messageBody = JsonSerializer.Serialize(summary, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                var sendMessageRequest = new SendMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MessageBody = messageBody
                };

                var response = await _sqsClient.SendMessageAsync(sendMessageRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation($"Successfully sent load test summary to SQS. MessageId: {response.MessageId}");
                }
                else
                {
                    _logger.LogWarning($"Failed to send message to SQS. Status code: {response.HttpStatusCode}");
                }
            }
            catch (Amazon.Runtime.AmazonServiceException ex) when (ex.Message.Contains("Unable to get IAM security credentials"))
            {
                _logger.LogError(ex, "Unable to send load test summary to SQS: AWS credentials not available. " +
                    "Ensure AWS credentials are configured (e.g., AWS_ACCESS_KEY_ID/AWS_SECRET_ACCESS_KEY environment variables, " +
                    "~/.aws/credentials file, or ECS task role when running in ECS)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending load test summary to SQS");
            }
        }
    }

    public class LoadTestSummary
    {
        public string TestId { get; set; } = string.Empty;
        public double Duration { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double Tps { get; set; }
        public string TargetUrl { get; set; } = string.Empty;
    }
}
