using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

public class Program
{
    private static readonly ConcurrentBag<long> _successfulRequests = new ConcurrentBag<long>();
    private static readonly ConcurrentBag<long> _failedRequests = new ConcurrentBag<long>();
    private static readonly object _consoleLock = new object();

    static async Task Main(string[] args)
    {
        // Log input arguments before validation
        Console.WriteLine("Input Arguments:");
        for (int i = 0; i < args.Length; i++)
        {
            Console.WriteLine($"Argument {i}: {args[i]}");
        }

        // 1. Validate command-line arguments
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: ApiLoadTester <apiUrl> <virtualUsers> <ratePerVU> <durationSeconds>");
            Console.WriteLine("ERROR: Exactly 4 arguments are required.");
            return;
        }

        // 2. Parse command-line arguments
        string apiUrl = args[0];
        if (!int.TryParse(args[1], out int vus))
        {
            Console.WriteLine("ERROR: Invalid number of virtual users.");
            return;
        }
        if (!int.TryParse(args[2], out int ratePerVU))
        {
            Console.WriteLine("ERROR: Invalid rate per virtual user.");
            return;
        }
        if (!int.TryParse(args[3], out int durationSeconds))
        {
            Console.WriteLine("ERROR: Invalid duration seconds.");
            return;
        }

        Console.WriteLine($"Starting Load Test");
        Console.WriteLine($"Virtual Users (VUs): {vus}");
        Console.WriteLine($"Rate per VU: {ratePerVU} transactions/second");
        Console.WriteLine($"Duration: {durationSeconds} seconds");
        Console.WriteLine($"Target URL: {apiUrl}");
        Console.WriteLine("--------------------------------------");

        // 3. Prepare load test
        var duration = TimeSpan.FromSeconds(durationSeconds);
        var startTime = DateTime.UtcNow;
        var endTime = startTime.Add(duration);

        // 4. Create VU tasks
        var tasks = new List<Task>();
        for (int i = 0; i < vus; i++)
        {
            tasks.Add(RunUser(i + 1, apiUrl, ratePerVU, startTime, endTime));
        }

        // 5. Wait for all users to finish
        await Task.WhenAll(tasks);

        // 6. Calculate and display results
        var totalDuration = (DateTime.UtcNow - startTime).TotalSeconds;
        var totalSuccessfulRequests = _successfulRequests.Count;
        var totalFailedRequests = _failedRequests.Count;
        var overallTPS = totalSuccessfulRequests / totalDuration;

        Console.WriteLine("--------------------------------------");
        Console.WriteLine("Load Test Summary:");
        Console.WriteLine($"Total Duration: {totalDuration:F2} seconds");
        Console.WriteLine($"Total Successful Requests: {totalSuccessfulRequests}");
        Console.WriteLine($"Total Failed Requests: {totalFailedRequests}");
        Console.WriteLine($"Overall Transactions Per Second (TPS): {overallTPS:F2}");
        Console.WriteLine("Load Test Completed.");
    }

    // Virtual user behavior
    static async Task RunUser(int id, string url, int ratePerVU, DateTime testStart, DateTime testEnd)
    {
        using var client = new HttpClient();
        var requestInterval = TimeSpan.FromMilliseconds(1000.0 / ratePerVU);

        while (DateTime.UtcNow < testEnd)
        {
            var nextRequestTime = DateTime.UtcNow.Add(requestInterval);

            try
            {
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    _successfulRequests.Add(1);
                    LogRequest(id, response.StatusCode, true);
                }
                else
                {
                    _failedRequests.Add(1);
                    LogRequest(id, response.StatusCode, false);
                }
            }
            catch (Exception ex)
            {
                _failedRequests.Add(1);
                LogRequest(id, null, false, ex.Message);
            }

            // Rate limiting: wait until next allowed request time
            var delay = nextRequestTime - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay);
            }
        }
    }

    static void LogRequest(int vuId, System.Net.HttpStatusCode? statusCode, bool success, string errorMessage = null)
    {
        lock (_consoleLock)
        {
            Console.WriteLine(success
                ? $"VU-{vuId} → {statusCode}"
                : $"VU-{vuId} ERROR → {errorMessage ?? statusCode?.ToString()}");
        }
    }
}
