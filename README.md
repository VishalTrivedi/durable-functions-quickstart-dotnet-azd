<!--
---
name: Durable Functions C# Fan-Out/Fan-In using Azure Developer CLI
description: This repository contains a Durable functions quickstart written in C# demonstrating the fan-out/fan-in pattern. It's deployed to Azure Functions Flex Consumption plan using the Azure Developer CLI (azd). The sample uses managed identity and a virtual network to make sure deployment is secure by default.
page_type: sample
products:
- azure-functions
- azure
- entra-id
urlFragment: starter-durable-fan-out-fan-in-csharp
languages:
- csharp
- bicep
- azdeveloper
---
-->

# Durable Functions Fan-Out/Fan-In using Azure Developer CLI

This template repository contains a Durable Functions sample demonstrating the fan-out/fan-in pattern in C# (isolated process model). The sample can be easily deployed to Azure using the Azure Developer CLI (`azd`). It uses managed identity and a virtual network to make sure deployment is secure by default. You can opt out of a VNet being used in the sample by setting VNET_ENABLED to false in the parameters.

[Durable Functions](https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-overview) is part of Azure Functions offering. It helps orchestrate stateful logic that's long-running or multi-step by providing *durable execution*. An execution is durable when it can continue in another process or machine from the point of failure in the face of interruptions or infrastructure failures. Durable Functions handles automatic retries and state persistence as your orchestrations run to ensure durable execution. 

Durable Functions needs a [backend provider](https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-storage-providers) to persist application states. This sample uses the Azure Storage backend. 

## Prerequisites

+ [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
+ [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local?pivots=programming-language-csharp#install-the-azure-functions-core-tools)
+ To use Visual Studio to run and debug locally:
  + [Visual Studio 2022](https://visualstudio.microsoft.com/vs/).
  + Make sure to select the **Azure development** workload during installation.
+ To use Visual Studio Code to run and debug locally:
  + [Visual Studio Code](https://code.visualstudio.com/)
  + [Azure Functions extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azurefunctions)

## Initialize the local project

You can initialize a project from this `azd` template in one of these ways:

+ Use this `azd init` command from an empty local (root) folder:

    ```shell
    azd init --template durable-functions-quickstart-dotnet-azd
    ```

    Supply an environment name, such as `dfquickstart` when prompted. In `azd`, the environment is used to maintain a unique deployment context for your app.

+ Clone the GitHub template repository locally using the `git clone` command:

    ```shell
    git clone https://github.com/Azure-Samples/durable-functions-quickstart-dotnet-azd.git
    cd fanoutfanin
    ```

    You can also clone the repository from your own fork in GitHub.

## Prepare your local environment

Navigate to the `fanoutfanin` app folder and create a file in that folder named _local.settings.json_ that contains this JSON data:

```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
    }
}
```

### Start Azurite
The Functions runtime requires a storage component. The line `"AzureWebJobsStorage": "UseDevelopmentStorage=true"` above tells the runtime that the local storage emulator, Azurite, will be used. Azurite needs to be started before running the app, and there are two options to do that:
* Option 1: Run `npx azurite --skipApiVersionCheck --location ~/azurite-data`
* Option 2: Run `docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite`

## Run your app from the terminal

1. From the `fanoutfanin` folder, run this command to start the Functions host locally:

    ```shell
    func start
    ```

1. From your HTTP test tool in a new terminal (or from your browser), call the HTTP trigger endpoint: <http://localhost:7071/api/FetchOrchestration_HttpStart> to start a new orchestration instance. This orchestration then fans out to several activities to fetch the titles of Microsoft Learn articles in parallel. When the activities finish, the orchestration fans back in and returns the titles as a formatted string. 

    The HTTP endpoint should return several URLs (showing a few below for brevity). The `statusQueryGetUri` provides the orchestration status. 
    ```json
    {
        "id": "9addc67238604701a38d1470874a5f04",
        "statusQueryGetUri": "http://localhost:7071/runtime/webhooks/durabletask/instances/9addc67238604701a38d1470874a5f04?taskHub=TestHubName&connection=Storage&code=<code>",
        "sendEventPostUri": "http://localhost:7071/runtime/webhooks/durabletask/instances/9addc67238604701a38d1470874a5f04/raiseEvent/{eventName}?taskHub=TestHubName&connection=Storage&code=<code>",
        "terminatePostUri": "http://localhost:7071/runtime/webhooks/durabletask/instances/9addc67238604701a38d1470874a5f04/terminate?reason={text}&taskHub=TestHubName&connection=Storage&code<code>",
    }
    ```

1. When you're done, press Ctrl+C in the terminal window to stop the `func.exe` host process.

## Run your app using Visual Studio Code

1. Open the `fanoutfanin` app folder in a new terminal.
1. Run the `code .` code command to open the project in Visual Studio Code.
1. Press **Run/Debug (F5)** to run in the debugger. Select **Debug anyway** if prompted about local emulator not running.
1. From your HTTP test tool in a new terminal (or from your browser), call the HTTP trigger endpoint: <http://localhost:7071/api/FetchOrchestration_HttpStart> to start a new orchestration instance.
1. The HTTP endpoint should return several URLs. The `statusQueryGetUri` provides the orchestration status. 

## Run your app using Visual Studio

1. Open the `fanoutfanin.sln` solution file in Visual Studio.
1. Press **Run/F5** to run in the debugger. Make a note of the `localhost` URL endpoints, including the port, which might not be `7071`.
1. From your HTTP test tool in a new terminal (or from your browser), call the HTTP trigger endpoint: <http://localhost:7071/api/FetchOrchestration_HttpStart> to start a new orchestration instance.
1. The HTTP endpoint should return several URLs. The `statusQueryGetUri` provides the orchestration status. 

> ![NOTE] 
> If you run the app locally after running `azd up`, you may get `There was an error parsing the Connection String: Input contains invalid delimiters and cannot be parsed.` related to App Insights. Check for user secrets by running `dotnet user-secrets list`. If the list is populated, clear them by using `dotnet user-secrets clear` before running the app again. These secrets are auto populated by `azd up` during app deployment. 

## Source Code
Fanning out is easy to do with regular functions, simply send multiple messages to a queue. However, fanning in is more challenging, because you need to track when all the functions are completed and store the outputs.

Durable Functions makes implementing fan-out/fan-in easy for you. This sample uses a simple scenario of fetching article titles in parallel to demonstrate how you can implement the pattern with Durable Functions. In `FetchOrchestration`, the title fetching activities are tracked using a dynamic task list. The line `await Task.WhenAll(parallelTasks);` waits for all the called activities, which are run concurrently, to complete. When done, all outputs are aggregated as a formatted string. More sophisticated aggregation logic is probably required in real-world scenarios, such as uploading the result to storage or sending it downstream, which you can do by calling another activity function.

```csharp
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
        "https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-overview",
        "https://learn.microsoft.com/azure/azure-functions/durable/durable-task-scheduler/durable-task-scheduler",
        "https://learn.microsoft.com/azure/azure-functions/functions-scenarios",
        "https://learn.microsoft.com/azure/azure-functions/functions-create-ai-enabled-apps",
    };

    // Run fetching tasks in parallel
    foreach (var url in urls)
    {
        Task<string> task = context.CallActivityAsync<string>(nameof(FetchTitleAsync), url);
        parallelTasks.Add(task);
    }
    
    // Wait for all the parallel tasks to complete before continuing
    await Task.WhenAll(parallelTasks);
    
    // Return fetched titles as a formatted string
    return string.Join(", ", parallelTasks.Select(t => t.Result));
}
```


## Deploy to Azure

Run this command to provision the function app, with any required Azure resources, and deploy your code:

```shell
azd up
```

By default, this sample prompts to enable a virtual network for enhanced security. If you want to deploy without a virtual network without prompting, you can configure `VNET_ENABLED` to `false` before running `azd up`:

```bash
azd env set VNET_ENABLED false
azd up
```

You're prompted to supply these required deployment parameters:

| Parameter | Description |
| ---- | ---- |
| _Environment name_ | An environment that's used to maintain a unique deployment context for your app. You won't be prompted if you created the local project using `azd init`.|
| _Azure subscription_ | Subscription in which your resources are created.|
| _Azure location_ | Azure region in which to create the resource group that contains the new Azure resources. Only regions that currently support the Flex Consumption plan are shown.|

## Test deployed app

Once deployment is done, test the Durable Functions app by making an HTTP request to trigger the start of an orchestration. To get the endpoint quickly, run the following: 

    ```shell
    az functionapp function list --resource-group <resource-group-name> --name <function-app-name> --query "[].{name:name, url:invokeUrlTemplate}" --output table
    ```

The _function-app-name/http_start_ is the endpoint, and it should look like:
 
 ```
 https://<function-app-name>.azurewebsites.net/api/fetchorchestration_httpstart
 ```

## Redeploy your code

You can run the `azd up` command as many times as you need to both provision your Azure resources and deploy code updates to your function app.

>[!NOTE]
>Deployed code files are always overwritten by the latest deployment package.

## Clean up resources

When you're done working with your function app and related resources, you can use this command to delete the function app and its related resources from Azure and avoid incurring any further costs:

```shell
azd down
```
