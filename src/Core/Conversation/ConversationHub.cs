using Domain;
using Microsoft.AspNetCore.SignalR;

namespace Core.Conversation;

public interface IConversationClient
{
    Task ConversationCreated(ConversationInfo conversationInfo);

    Task ConversationDeleted(Guid conversationId);

    Task ConversationInfoUpdated(ConversationInfo conversationInfo);

    Task MessageStart(Guid conversationId);

    Task MessageContent(Guid conversationId, Guid messageId, string partialContent);

    Task PromptReceived(Guid conversationId, Message promptMessage, string eTag);

    Task MessageEnd(Guid conversationId, Message message, string eTag);

    Task MessageError(Guid conversationId, Guid promptMessageId, string eTag);
}

public class ConversationHub : Hub<IConversationClient>;