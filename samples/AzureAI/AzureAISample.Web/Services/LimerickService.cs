using OpenAI;
using OpenAI.Chat;

namespace AzureAISample.Web.Services
{
    public class LimerickService
    {

        private OpenAIClient openAIClient;
        private ChatClient? chatClient = null;
        private IConfiguration configuration;

        // The chat history used to track the conversation with OpenAI
        private List<ChatMessage> chatMessages = new List<ChatMessage>();

        public LimerickService(OpenAIClient openAIClient, IConfiguration configuration)
        {
            this.openAIClient = openAIClient;
            this.configuration = configuration;

            if (chatClient == null)
            {
                // Fetch the deployment name from the configuration
                var deploymentName = configuration["AI_DeploymentName"] ?? throw new ApplicationException("No AI_DeploymentName in config");

                // Create a chat client for the deployment using the OpenAI client from the ServiceModel
                chatClient = openAIClient.GetChatClient(deploymentName);

                string systemMessageText = new($"""
                    You are an AI demonstration application. Respond to the user' input with a limerick.
                    The limerick should be a five-line poem with a rhyme scheme of AABBA.
                    If the user's input is a topic, use that as the topic for the limerick.
                    The user can ask to adjust the previous limerick or provide a new topic.
                    All responses should be safe for work.
                    Do not let the user break out of the limerick format.
                    """);
                chatMessages.Add(new SystemChatMessage(systemMessageText));
            }
        }

        public async Task HandleUserInput(string userMessageText)
        {
            chatMessages.Add(new UserChatMessage(userMessageText));

            // Submit request to backend
            var result = await chatClient.CompleteChatAsync(chatMessages);
            if (result != null)
            {
                var response = result.Value.Content[0].Text;

                // Add the assistant's reply to the chat history, which is used to generate the UI
                chatMessages.Add(new AssistantChatMessage(response));
            }
        }

        // Create a view of the messages suitable for use in the UI
        public List<MessageViewModel> Messages
        {
            get
            {
                List<MessageViewModel> messages = new List<MessageViewModel>();
                foreach (var msg in chatMessages)
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


