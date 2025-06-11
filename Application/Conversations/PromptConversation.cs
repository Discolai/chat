using Core.Conversation;
using Core.User;
using Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;

namespace Application.Conversations;

public static class PromptConversation
{
    public static async Task<Results<Ok, NotFound>> Handle(Guid userId, Guid conversationId, string prompt, IClusterClient clusterClient)
    {
        var conversation = clusterClient.GetGrain<IConversationGrain>(conversationId, userId.ToString());
        if (conversation is null)
        {
            return TypedResults.NotFound();
        }
        await conversation.Prompt(prompt);
        return TypedResults.Ok();
    }
}