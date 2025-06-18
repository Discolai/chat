using Core.Conversation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Application.Conversations;

public record PromptRequest(string Prompt);

public static class PromptConversation
{
    public static async Task<Results<Ok, NotFound>> Handle(Guid conversationId, PromptRequest request, UserProvider userProvider, IClusterClient clusterClient)
    {
        if (!userProvider.TryGetUserId(out var userId))
        {
            return TypedResults.NotFound();
        }
        var conversation = clusterClient.GetGrain<IConversationGrain>(conversationId, userId);
        if (conversation is null)
        {
            return TypedResults.NotFound();
        }
        await conversation.Prompt(request.Prompt);
        return TypedResults.Ok();
    }
}