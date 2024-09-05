using AzureAISample.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AzureAISample.Web.Components.Pages
{
    public partial class Chat
    {
        ElementReference userMessage;
        ElementReference sendBtn;
        string? _userMessageText;
        int _pendingRequestCount = 0;

        [Inject, EditorRequired]
        public LimerickService _sampleChatService { get; init; }


        [Inject]
        private IJSRuntime JS { get; set; }

        async Task SendChatRequest()
        {
            if (!string.IsNullOrWhiteSpace(_userMessageText))
            {
                string msg = _userMessageText;
                _userMessageText = null;
                _pendingRequestCount++;

                //Call the chat service asynchronously to handle the user input
                await _sampleChatService.SendRequestToAI(msg);
                _pendingRequestCount--;

                StateHasChanged();

            }
        }

        // Injection of javascript to handle the enter key press
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            Console.WriteLine("OnAfterRenderAsync");
            if (firstRender)
            {
                try
                {
                    await using var module = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Pages/Chat.razor.js");
                    await module.InvokeVoidAsync("clickOnEnter", userMessage, sendBtn );
                }
                catch (JSDisconnectedException)
                {
                    // Not an error
                }
            }
        }
    }
}
