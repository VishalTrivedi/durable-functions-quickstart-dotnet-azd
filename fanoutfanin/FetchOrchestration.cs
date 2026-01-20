using System.Net.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace Company.Function
{
    public static class FetchOrchestration
    {
        [Function(nameof(FetchOrchestration))]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(FetchOrchestration));
            logger.LogInformation("Fetching data.");
            var parallelTasks = new List<Task<string>>();
            
            // List of URLs to fetch titles from
            var urls = new List<string>
            {
                "https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-task-scheduler/quickstart-durable-task-scheduler?pivots=csharp",
                "https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-isolated-create-first-csharp?pivots=code-editor-vscode",
                "https://learn.microsoft.com/en-us/azure/azure-functions/durable/quickstart-js-vscode?pivots=nodejs-model-v4",
                "https://learn.microsoft.com/en-us/azure/azure-functions/durable/quickstart-python-vscode?tabs=windows",
                "https://learn.microsoft.com/en-us/azure/azure-functions/durable/quickstart-powershell-vscode",
                "https://learn.microsoft.com/en-us/samples/browse/?term=durable%20functions&terms=durable%20functions",
                "https://learn.microsoft.com/en-us/samples/azure-samples/durable-functions-order-processing/durable-func-order-processing/",
                "https://learn.microsoft.com/en-us/samples/azure-samples/durable-functions-order-processing-python/durable-func-order-processing-py/",
                "https://learn.microsoft.com/en-us/samples/azure-samples/durablefunctions-apiscraping-dotnet/retrieve-opened-issue-count-on-github-with-azure-durable-functions/",
                "https://learn.microsoft.com/en-us/samples/azure/ai-document-processing-pipeline/azure-ai-document-processing-pipeline-python/",
                "https://learn.microsoft.com/en-us/samples/azure-samples/durablefunctions-apiscraping-nodejs/retrieve-opened-issue-count-on-github-with-azure-durable-functions/",
                "https://learn.microsoft.com/en-us/samples/azure-samples/durable-functions-quickstart-dotnet-azd/starter-durable-fan-out-fan-in-csharp/",
                "https://learn.microsoft.com/en-us/samples/azure-samples/indexadillo/template/",
                "https://learn.microsoft.com/en-us/samples/azure-samples/intelligent-pdf-summarizer-dotnet/durable-func-pdf-summarizer-csharp/",
                "https://learn.microsoft.com/en-us/samples/azure-samples/intelligent-pdf-summarizer/durable-func-pdf-summarizer/",
                "https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-types-features-overview",
                "https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-orchestrations?tabs=csharp-inproc",
                "https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-code-constraints?tabs=csharp",
                "https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-sub-orchestrations?tabs=csharp-inproc",
                "https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-custom-orchestration-status?tabs=csharp",
                "https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-timers?tabs=csharp",                
                "https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-overview",
                "https://learn.microsoft.com/azure/azure-functions/durable/durable-task-scheduler/durable-task-scheduler",
                "https://learn.microsoft.com/azure/azure-functions/functions-scenarios",
                "https://learn.microsoft.com/azure/azure-functions/functions-create-ai-enabled-apps",
            };

            const int maxParallelism = 3; // 👈 throttle
            var results = new List<string>();
        
            for (int i = 0; i < urls.Count; i += maxParallelism)
            {
                var batch = urls
                    .Skip(i)
                    .Take(maxParallelism)
                    .Select(url => context.CallActivityAsync<string>(nameof(FetchTitleAsync), url))
                    .ToList();
        
                // Wait for only this batch
                await Task.WhenAll(batch);
        
                results.AddRange(batch.Select(t => t.Result));
            }
        
            return string.Join(", ", results);
                    
/*
            // Run fetching tasks in parallel
            foreach (var url in urls)
            {
                Task<string> task = context.CallActivityAsync<string>(nameof(FetchTitleAsync), url);
                parallelTasks.Add(task);
            }
            
            // Wait for all the parallel tasks to complete before continuing
            await Task.WhenAll(parallelTasks);
           
            // Return fetched titles as a formatted string
            return string.Join("; ", parallelTasks.Select(t => t.Result));
*/            
        }

        [Function(nameof(FetchTitleAsync))]
        public static async Task<string> FetchTitleAsync([ActivityTrigger] string url, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("FetchTitleAsync");
            
            // Demo-friendly: random delay so the dashboard stays “alive”
            int minSeconds = int.TryParse(Environment.GetEnvironmentVariable("DEMO_MIN_DELAY_SECONDS"), out var min) ? min : 20;
            int maxSeconds = int.TryParse(Environment.GetEnvironmentVariable("DEMO_MAX_DELAY_SECONDS"), out var max) ? max : 60;
        
            int delaySeconds = Random.Shared.Next(minSeconds, maxSeconds + 1);
            logger.LogInformation("Delaying {DelaySeconds}s before fetching: {Url}", delaySeconds, url);
        
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

            logger.LogInformation("Fetching from url {url}.", url);

            HttpClient client = new HttpClient();

            try
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();

                // Extract page title
                var titleMatch = System.Text.RegularExpressions.Regex.Match(content, 
                    @"<title[^>]*>([^<]+?)\s*\|\s*Microsoft Learn</title>", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                string title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : "No title found";
                
                return title;
            }
            catch (HttpRequestException ex)
            {
                return $"Error fetching from {url}: {ex.Message}";
            }
        }

        [Function("FetchOrchestration_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("FetchOrchestration_HttpStart");

            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(FetchOrchestration));

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
    }
}




