# ApiLoadTester

A lightweight, high-performance API load testing tool built in C# for simulating concurrent user load on HTTP endpoints.

## Features

- **Virtual Users (VUs)**: Simulate multiple concurrent users making requests
- **Configurable Request Rate**: Control the number of requests per second per virtual user
- **Duration-Based Testing**: Run tests for a specified duration
- **Real-Time Logging**: Monitor request success/failure in real-time with single-line formatted logs
- **Performance Metrics**: Get detailed statistics including:
  - Total successful requests
  - Total failed requests
  - Overall transactions per second (TPS)
  - Test duration

## Requirements

- .NET 6.0 or higher
- Internet connection (for testing external APIs)

## Installation

1. Clone the repository:

```bash
git clone https://github.com/vkrishna92/ApiLoadTester.git
cd ApiLoadTester
```

2. Build the project:

```bash
dotnet build
```

## Usage

Run the application with the following command-line arguments:

```bash
dotnet run --project ApiLoadTester <apiUrl> <virtualUsers> <ratePerVU> <durationSeconds>
```

### Parameters

- `apiUrl`: The URL of the API endpoint to test (must include protocol, e.g., https://api.example.com/endpoint)
- `virtualUsers`: Number of concurrent virtual users (positive integer)
- `ratePerVU`: Number of requests per second per virtual user (positive integer)
- `durationSeconds`: Duration of the load test in seconds (positive integer)

### Example

Test an API with 10 virtual users, each making 5 requests per second, for 60 seconds:

```bash
dotnet run --project ApiLoadTester https://jsonplaceholder.typicode.com/posts 10 5 60
```

This will generate:

- 10 concurrent users
- 50 total requests per second (10 users × 5 requests)
- Test runs for 60 seconds
- Approximately 3000 total requests (50 req/s × 60s)

## Output

### During Test Execution

The application provides real-time logging of:

- Application startup
- Parsed arguments
- Individual request results (debug level)
- Error messages for failed requests

Example:

```
01:33:38 info: ApiLoadTester application starting...
01:33:38 info: Input Arguments:
01:33:38 info: Argument 0: https://jsonplaceholder.typicode.com/posts
01:33:38 info: Argument 1: 10
01:33:38 info: Argument 2: 5
01:33:38 info: Argument 3: 60
01:33:38 info: Parsed Arguments:
01:33:38 info: API URL: https://jsonplaceholder.typicode.com/posts
01:33:38 info: Virtual Users: 10
01:33:38 info: Rate per VU: 5
01:33:38 info: Duration Seconds: 60
01:33:38 dbug: VU-1 → OK
01:33:38 dbug: VU-2 → OK
...
```

### Test Summary

After completion, the application displays:

```
01:34:38 info: Load Test Summary:
01:34:38 info: Total Duration: 60.02 seconds
01:34:38 info: Total Successful Requests: 2998
01:34:38 info: Total Failed Requests: 2
01:34:38 info: Overall Transactions Per Second (TPS): 49.97
01:34:38 info: Load Test Completed.
```

## Project Structure

```
ApiLoadTester/
├── ApiLoadTester/
│   ├── Program.cs                    # Main application logic
│   ├── Services/
│   │   └── LogHelper.cs             # Centralized logging utility
│   ├── constants/
│   │   └── MainArgsKeys.cs          # Argument key constants
│   └── ApiLoadTester.csproj         # Project configuration
├── Dockerfile                        # Docker containerization
├── ApiLoadTester.sln                # Solution file
└── README.md                        # This file
```

## Architecture

### Key Components

1. **Program.cs**: Main application logic

   - `ConfigureLogging()`: Sets up single-line console logging
   - `RunUser()`: Virtual user behavior simulation
   - `LogTestSummary()`: Displays test results
   - `validateAndParseArgs()`: Command-line argument validation

2. **LogHelper.cs**: Centralized logging service

   - Provides consistent logging across the application
   - Supports Info, Warning, Error, and Debug levels

3. **MainArgsKeys.cs**: Constants for argument keys
   - Maintains consistency in argument handling

## Docker Support

Build and run using Docker:

```bash
# Build the image
docker build -t apiloadtester .

# Run a test
docker run apiloadtester https://jsonplaceholder.typicode.com/posts 10 5 60
```

## AWS ECS Deployment

Deploy and run the ApiLoadTester on AWS Elastic Container Service (ECS).

### Prerequisites

- AWS CLI configured with appropriate credentials
- An AWS account with permissions to create ECR repositories, ECS clusters, and task definitions
- Docker installed locally

### Step 1: Push to Amazon ECR (Elastic Container Registry)

1. Create an ECR repository (if not already created):

```bash
aws ecr create-repository --repository-name apiloadtester --region us-east-1
```

2. Retrieve authentication commands from AWS Management Console:
   - Navigate to **Amazon ECR** → **Repositories** → Select your repository
   - Click **View push commands** button
   - Follow the displayed commands, which will look similar to:

```bash
# Authenticate Docker to ECR
aws ecr get-login-password --region us-west-2 | docker login --username AWS --password-stdin <Your Account>.dkr.ecr.us-west-2.amazonaws.com

# Build the Docker image
docker build -t dotnet/apiloadtester .

# Tag the image
docker tag dotnet/apiloadtester:latest <Your Account>.dkr.ecr.us-west-2.amazonaws.com/dotnet/apiloadtester:latest

# Push the image to ECR
docker push <You Account>.dkr.ecr.us-west-2.amazonaws.com/dotnet/apiloadtester:latest
```

### Step 2: Configure ECS Cluster

Create an ECS cluster to run your load testing tasks:

**Using AWS Console:**

1. Navigate to **Amazon ECS** → **Clusters**
2. Click **Create Cluster**
3. Select **AWS Fargate (serverless)** or **EC2** based on your preference
4. Configure cluster settings:
   - **Cluster name**: `apiloadtester-cluster`
   - **VPC**: Select your VPC or create new
   - **Subnets**: Select appropriate subnets
   - **Infrastructure**: Choose Fargate for serverless or EC2 for more control
5. Click **Create**

### Step 3: Configure Task Definition

Create a task definition that specifies how to run your container:

**Using AWS Console:**

1. Navigate to **Amazon ECS** → **Task Definitions**
2. Click **Create new Task Definition**
3. Configure the task definition:
   - **Task definition family**: `apiloadtester-task`
   - **Launch type**: Fargate or EC2
   - **Operating system/Architecture**: Linux/X86_64
   - **Task role**: Select appropriate IAM role (if needed)
   - **Task execution role**: ecsTaskExecutionRole
   - **Task memory**: 512 MB (adjust as needed)
   - **Task CPU**: 0.25 vCPU (adjust as needed)
4. Add container:
   - **Container name**: `apiloadtester`
   - **Image URI**: `<aws_account_id>.dkr.ecr.us-east-1.amazonaws.com/apiloadtester:latest`
   - **Essential container**: Yes
   - **Port mappings**: Not required for this application
   - **Environment**: Leave blank (will be overridden at runtime)
   - **Log configuration**:
     - Log driver: `awslogs`
     - Log group: `/ecs/apiloadtester`
     - Region: `us-east-1`
     - Stream prefix: `ecs`
5. Click **Create**

### Step 4: Run Task with Container Overrides

Run the task with specific test parameters using container overrides:

**Using AWS Console:**

1. Navigate to **Amazon ECS** → **Clusters** → Select your cluster
2. Click **Tasks** tab → **Run new task**
3. Configure:
   - **Compute options**: Launch type (Fargate or EC2)
   - **Task definition**: Select `apiloadtester-task`
   - **Revision**: Latest
   - **Desired tasks**: 1
   - **Task group**: (optional)
4. **Networking**:
   - **VPC**: Select your VPC
   - **Subnets**: Select public subnet (if external API) or private subnet
   - **Security group**: Allow outbound HTTPS (port 443)
   - **Auto-assign public IP**: Enabled (if using public subnet)
5. **Container overrides**:
   - Expand the `apiloadtester` container
   - **Command override**: Enter test parameters as comma-separated values:
     ```
     https://jsonplaceholder.typicode.com/posts,10,5,60
     ```
     (This translates to: API URL, 10 virtual users, 5 requests per VU, 60 seconds duration)
6. Click **Create**

### Container Override Command Examples

Different load test scenarios using command overrides:

```bash
# Light load: 5 users, 2 requests/second, 30 seconds
"command": ["https://api.example.com/endpoint", "5", "2", "30"]

# Medium load: 20 users, 10 requests/second, 120 seconds
"command": ["https://api.example.com/endpoint", "20", "10", "120"]

# Heavy load: 50 users, 20 requests/second, 300 seconds
"command": ["https://api.example.com/endpoint", "50", "20", "300"]

# Stress test: 100 users, 50 requests/second, 600 seconds
"command": ["https://api.example.com/endpoint", "100", "50", "600"]
```

### Viewing Logs

Monitor your load test execution:

**Using AWS Console:**

1. Navigate to **Amazon ECS** → **Clusters** → Your cluster
2. Click **Tasks** tab → Select your running task
3. Click **Logs** tab to view real-time output
4. Or navigate to **CloudWatch** → **Log groups** → `/ecs/apiloadtester`

**Using AWS CLI:**

```bash
# Get task ARN
aws ecs list-tasks --cluster apiloadtester-cluster

# View logs in CloudWatch
aws logs tail /ecs/apiloadtester --follow
```

### Cost Optimization

- Use **Fargate Spot** for non-critical load tests to save up to 70%
- Stop tasks immediately after completion
- Delete unused ECR images
- Use **EventBridge** to schedule load tests during off-peak hours

### Troubleshooting

- **Task fails to start**: Check security group allows outbound internet access
- **No logs appearing**: Verify CloudWatch log group exists and task execution role has permissions
- **Task stops immediately**: Check container override command format is correct
- **Authentication errors**: Ensure ECR repository policies allow ECS task execution role to pull images

## Error Handling

The application handles various error scenarios:

- Invalid number of arguments
- Non-numeric values for numeric parameters
- Network failures during requests
- HTTP error responses

All errors are logged with appropriate detail for troubleshooting.

## Performance Considerations

- Uses `HttpClient` with connection pooling for efficiency
- Implements precise rate limiting per virtual user
- Thread-safe request tracking using `ConcurrentBag`
- Minimal memory footprint for high-load scenarios

## Limitations

- Supports only GET requests (can be extended for POST/PUT/DELETE)
- No authentication mechanism (can be added as needed)
- Basic error reporting (detailed response analysis can be added)

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is open-source and available under the MIT License.

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/vkrishna92/ApiLoadTester).

## Changelog

### Version 1.0.0

- Initial release
- Virtual user simulation
- Configurable request rates
- Real-time logging with single-line format
- Performance metrics and summary
