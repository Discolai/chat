using Core.Conversation;
using Core.User;
using Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Application.Conversations;

public class CreateUserConversationRequest
{
    public required AIModel Model { get; set; }
    public string? InitialPrompt { get; set; }
}

public static class CreateUserConversation
{
    public static async Task<Results<Ok<ConversationInfo>, NotFound>> Handle(CreateUserConversationRequest request, IClusterClient clusterClient, UserProvider userProvider)
    {
        if (!userProvider.TryGetUser(out var user, out var userId))
        {
            return TypedResults.NotFound();
        }
        var conversationInfo = await user.CreateConversation(request.Model, request.InitialPrompt);
        if (!string.IsNullOrEmpty(request.InitialPrompt))
        {
            await clusterClient
                .GetGrain<IConversationGrain>(conversationInfo.Id, userId)
                .Prompt(request.InitialPrompt);
        }
        return TypedResults.Ok(conversationInfo);
    }
}
