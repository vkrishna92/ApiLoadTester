using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using ApiLoadTester.constants;
using Microsoft.Extensions.Logging;
using ApiLoadTester.Services;

public class Program
{
    private static readonly ConcurrentBag<long> _successfulRequests = new ConcurrentBag<long>();
    private static readonly ConcurrentBag<long> _failedRequests = new ConcurrentBag<long>();
    private static readonly object _consoleLock = new object();

    static async Task Main(string[] args)
    {
        // Configure logging
        using var loggerFactory = ConfigureLogging();

        // Initialize the LogHelper with a logger
        LogHelper.Initialize(loggerFactory.CreateLogger<Program>());

        // Log the start of the application
        LogHelper.LogInfo("ApiLoadTester application starting...");

        // Log input arguments before validation
        logUnparsedArgs(args);

        // 1. Validate and parse command-line arguments
        var parsedArgs = validateAndParseArgs(args);
        if (parsedArgs.Count == 0)
        {
            LogHelper.LogWarning("No valid arguments provided. Exiting.");
            return; // Exit if arguments are invalid
        }

        // 2. Extract parsed arguments
        string apiUrl = (string)parsedArgs[MainArgsKeys.ApiUrl];
        int vus = (int)parsedArgs[MainArgsKeys.VirtualUsers];
        int ratePerVU = (int)parsedArgs[MainArgsKeys.RatePerVU];
        int durationSeconds = (int)parsedArgs[MainArgsKeys.DurationSeconds];

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

        LogTestSummary(totalDuration, totalSuccessfulRequests, totalFailedRequests, overallTPS);
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
                LogHelper.LogError(ex, $"VU-{id} encountered an error");
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
            if (success)
            {
                LogHelper.LogDebug("VU-{VuId} → {StatusCode}", vuId, statusCode);
            }
            else
            {
                LogHelper.LogWarning("VU-{VuId} ERROR → {ErrorMessage}", vuId, errorMessage ?? statusCode?.ToString());
            }
        }
    }

    private static Dictionary<string, Object> validateAndParseArgs(string[] args)
    {
        Dictionary<string, Object> parsedArgs = new Dictionary<string, Object>();

        // 1. Validate command-line arguments
        if (args.Length != 4)
        {
            LogHelper.LogError(new ArgumentException("Invalid number of arguments"),
                "Usage: ApiLoadTester <apiUrl> <virtualUsers> <ratePerVU> <durationSeconds>");
            return parsedArgs;
        }

        // 2. Parse command-line arguments
        string apiUrl = args[0];
        if (!int.TryParse(args[1], out int vus))
        {
            LogHelper.LogError(new ArgumentException("Invalid number of virtual users"));
            return parsedArgs;
        }
        if (!int.TryParse(args[2], out int ratePerVU))
        {
            LogHelper.LogError(new ArgumentException("Invalid rate per virtual user"));
            return parsedArgs;
        }
        if (!int.TryParse(args[3], out int durationSeconds))
        {
            LogHelper.LogError(new ArgumentException("Invalid duration seconds"));
            return parsedArgs;
        }

        parsedArgs[MainArgsKeys.ApiUrl] = apiUrl;
        parsedArgs[MainArgsKeys.VirtualUsers] = vus;
        parsedArgs[MainArgsKeys.RatePerVU] = ratePerVU;
        parsedArgs[MainArgsKeys.DurationSeconds] = durationSeconds;

        // log parsed arguments
        LogHelper.LogInfo("Parsed Arguments:");
        LogHelper.LogInfo($"API URL: {apiUrl}");
        LogHelper.LogInfo($"Virtual Users: {vus}");
        LogHelper.LogInfo($"Rate per VU: {ratePerVU}");
        LogHelper.LogInfo($"Duration Seconds: {durationSeconds}");

        return parsedArgs;
    }

    private static void logUnparsedArgs(string[] args)
    {
        LogHelper.LogInfo("Input Arguments:");
        for (int i = 0; i < args.Length; i++)
        {
            LogHelper.LogInfo($"Argument {i}: {args[i]}");
        }
    }

    private static void LogTestSummary(double totalDuration, int totalSuccessfulRequests, int totalFailedRequests, double overallTPS)
    {
        LogHelper.LogInfo("Load Test Summary:");
        LogHelper.LogInfo($"Total Duration: {totalDuration:F2} seconds");
        LogHelper.LogInfo($"Total Successful Requests: {totalSuccessfulRequests}");
        LogHelper.LogInfo($"Total Failed Requests: {totalFailedRequests}");
        LogHelper.LogInfo($"Overall Transactions Per Second (TPS): {overallTPS:F2}");
        LogHelper.LogInfo("Load Test Completed.");
    }

    private static ILoggerFactory ConfigureLogging()
    {
        return LoggerFactory.Create(builder =>
        {
            builder
                .AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "HH:mm:ss ";
                    options.SingleLine = true;
                    options.IncludeScopes = false;
                })
                .SetMinimumLevel(LogLevel.Debug);
        });
    }
}
