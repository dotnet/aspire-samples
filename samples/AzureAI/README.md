# Simple AzureAI Sample for Aspire

This sample demonstrates how to use the Aspire AI components to talk to Azure OpenAI. 

The benefits to using Aspire integration for your AI applications are:
- Central configuration via the *AppHost* project.
- Design-time provisioning of Azure resources including Azure OpenAI
- OpenTelemetry configuration for collecting traces & metrics from the `Aspire.Azure.AI.OpenAI` 
component visualized in the Aspire Dashboard
- F5 orchestration so you can run multiple projects all at once at design-time

There are 4 main pieces to this sample:

- Use of `AddAzureOpenAI()` in _program.cs_ in the *AzureAISample.AppHost* project. 
This will perform design-time provisioning of an Azure OpenAI resource at design time,
based on properties that need to be supplied in the appsettings.development.json. 

  You need to supply a block similar to the following for the application to run:

   ``` json
    "Azure": {
        "SubscriptionId": "__your_subscription_guid__",
        "AllowResourceGroupCreation": true,
        "ResourceGroup": "__name_your_resource_group__",
        "Location": "__azure_region_name_such_as_`WestUS`__"
    }
   ```

- Use of the `Aspire.Azure.AI.OpenAI` component for creating a chat client. This references 
the name used in the *AppHost* definition to be able to access the deployed configuration. 
The component is consumed by the Limerick service.

- An example of using gen_AI with OpenAI to provide a Limerick service implemented in
 _Services/LimerikService.cs_  in the *AzureAISample.Web* project. This uses the `Aspire.Azure.AI.OpenAI`
component via DI to create an instance of the chat client. 
  
  The limerick scenario was chosen as its one of the simplest ways to demonstrate using 
  the AI for chat scenarios. For a more comprehensive set of samples for using AI in .NET
  see https://github.com/dotnet/ai-samples.

- A Blazor UI to request the topics for the limerick and present the results to the user.
