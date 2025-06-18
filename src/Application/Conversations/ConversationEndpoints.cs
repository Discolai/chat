using Application.Conversations.Messages;

namespace Application.Conversations;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/conversations").RequireAuthorization();

        group.MapPost("/", CreateUserConversation.Handle);
        group.MapDelete("/{conversationId}", DeleteConversationEndpoint.Handle);
        group.MapGet("/", GetUserConversations.Handle);
        group.MapPost("/{conversationId}/prompt", PromptConversation.Handle);
        group.MapGet("/{conversationId}/messages", GetConversationMessages.Handle);
        group.MapPost("/{conversationId}/model", SwitchConversationModel.Handle);

        return builder;
    }
}
