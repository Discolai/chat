using Domain;
using Microsoft.AspNetCore.SignalR;

namespace Core.Conversation;

public interface IConversationClient
{
    Task MessageStart(Guid conversationId);

    Task MessageContent(Guid conversationId, Guid messageId, string partialContent);

    Task PromptReceived(Guid conversationId, Message promptMessage, string eTag);

    Task MessageEnd(Guid conversationId, Message message, string eTag);
}

public class ConversationHub : Hub<IConversationClient>
{
    public override Task OnConnectedAsync()
    {
        return base.OnConnectedAsync();
    }
}