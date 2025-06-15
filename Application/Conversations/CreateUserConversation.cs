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
    public static async Task<Results<Ok<ConversationInfo>, NotFound>> Handle(Guid userId, CreateUserConversationRequest request, IClusterClient clusterClient)
    {
        var user = clusterClient.GetGrain<IUserGrain>(userId);
        if (user is null)
        {
            return TypedResults.NotFound();
        }
        var conversationInfo = await user.CreateConversation(request.Model);
        if (!string.IsNullOrEmpty(request.InitialPrompt))
        {
            await clusterClient
                .GetGrain<IConversationGrain>(conversationInfo.Id, userId.ToString())
                .Prompt(request.InitialPrompt);
        }
        return TypedResults.Ok(conversationInfo);
    }
}
