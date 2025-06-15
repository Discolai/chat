using Application.Conversations.Messages;

namespace Application.Conversations;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/users/{userId}");

        group.MapPost("/conversations", CreateUserConversation.Handle);
        group.MapDelete("/conversations/{conversationId}", DeleteConversationEndpoint.Handle);
        group.MapGet("/conversations", GetUserConversations.Handle);
        group.MapPost("/conversations/{conversationId}/prompt", PromptConversation.Handle);
        group.MapGet("/conversations/{conversationId}/messages", GetConversationMessages.Handle);

        return builder;
    }
}
