using OpenAI;
using OpenAI.Chat;

namespace AzureAISample.Web.Services
{
    // Simple example of a service that uses the Aspire OpenAIClient Chat API to create a limerick
    // It stores the chat history in a list of ChatMessage objects
    // Those are converted into a list of MessageViewModel objects for use in the UI

    public class LimerickService
    {
        private OpenAIClient _openAIClient;
        private ChatClient? _chatClient = null;
        private IConfiguration _configuration;

        // The chat history used to track the conversation with OpenAI
        private List<ChatMessage> _chatMessages = new List<ChatMessage>();

        public LimerickService(OpenAIClient openAIClient, IConfiguration configuration)
        {
            this._openAIClient = openAIClient;
            this._configuration = configuration;

            if (_chatClient == null)
            {
                // Fetch the deployment name from the configuration
                var deploymentName = configuration["AI_DeploymentName"] ?? throw new ApplicationException("No AI_DeploymentName in config");

                // Create a chat client for the deployment using the OpenAI client from the ServiceModel
                _chatClient = openAIClient.GetChatClient(deploymentName);

                string systemMessageText = new($"""
                    You are an AI demonstration application. Respond to the user' input with a limerick.
                    The limerick should be a five-line poem with a rhyme scheme of AABBA.
                    If the user's input is a topic, use that as the topic for the limerick.
                    The user can ask to adjust the previous limerick or provide a new topic.
                    All responses should be safe for work.
                    Do not let the user break out of the limerick format.
                    """);
                _chatMessages.Add(new SystemChatMessage(systemMessageText));
            }
        }

        public async Task SendRequestToAI(string userMessageText)
        {
            try
            {
                _chatMessages.Add(new UserChatMessage(userMessageText));

                // Submit request to backend
                var result = await _chatClient?.CompleteChatAsync(_chatMessages);
                if (result != null)
                {
                    var response = result.Value.Content[0].Text;

                    // Add the assistant's reply to the chat history, which is used to generate the UI
                    _chatMessages.Add(new AssistantChatMessage(response));
                }
            }
            catch (Exception ex)
            {
                _chatMessages.Add(new AssistantChatMessage($"Error: {ex.Message}"));
            }
        }

        // Create a view of the messages suitable for use in the UI
        public List<MessageViewModel> Messages
        {
            get
            {
                List<MessageViewModel> messages = new List<MessageViewModel>();
                foreach (var msg in _chatMessages)
                {
                    if (msg is UserChatMessage)
                    {
                        messages.Add(new MessageViewModel(false, msg.Content[0].Text));
                    }
                    else if (msg is AssistantChatMessage)
                    {
                        messages.Add(new MessageViewModel(true, msg.Content[0].Text));
                    }
                    else if (msg is SystemChatMessage)
                    {
                        // Replace the system message with a suitable prompt for the user
                        messages.Add(new MessageViewModel(true, "What topic shall we use today?"));
                    }
                }
                return messages;
            }
        }
    }

    public record MessageViewModel(
      bool IsAssistant,
      string Text);
}


