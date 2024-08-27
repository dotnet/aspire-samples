using AzureAISample.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AzureAISample.Web.Components.Pages
{
    public partial class Chat
    {
        ElementReference writeMessageElement;
        string? userMessageText;

        [Inject]
        public LimerickService sampleChatService { get; init; }

        [Inject]
        public IJSRuntime JS { get; init; }

        //        @inject IJSRuntime JS
        //@inject SampleChatService sampleChatService

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            Console.WriteLine("OnAfterRenderAsync");
            if (firstRender)
            {
                //try
                //{
                //    await using var module = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Pages/Chat.razor.js");
                //    await module.InvokeVoidAsync("submitOnEnter", writeMessageElement);
                //}
                //catch (JSDisconnectedException)
                //{
                //    // Not an error
                //}
            }
        }

       async Task SendMessage()
        {
            if (!string.IsNullOrWhiteSpace(userMessageText))
            {
                string msg = userMessageText;
                userMessageText = null;
                // Add the user's message to the UI
                await sampleChatService.HandleUserInput(msg);
                StateHasChanged();
            }
        }

        //private void HandleResponseCompleted(MessageState state)
        //{
        //    // If it was cancelled before the response started, remove the message entirely
        //    // But if there was some text already, keep it
        //    if (string.IsNullOrEmpty(state.Text))
        //    {
        //        messages.Remove(state);
        //    }
        //}

  
    }
}
