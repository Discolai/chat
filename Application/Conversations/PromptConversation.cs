using Core.Conversation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Application.Conversations;

public record PromptRequest(string Prompt);

public static class PromptConversation
{
    public static async Task<Results<Ok, NotFound>> Handle(Guid userId, Guid conversationId, PromptRequest request, IClusterClient clusterClient)
    {
        var conversation = clusterClient.GetGrain<IConversationGrain>(conversationId, userId.ToString());
        if (conversation is null)
        {
            return TypedResults.NotFound();
        }
        await conversation.Prompt(request.Prompt);
        return TypedResults.Ok();
    }
}