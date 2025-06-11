using Core.User;
using Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Application.Conversations;

public class CreateUserConversationRequest
{
    public required AIModel Model { get; set; }
    public required string InitialPrompt { get; set; }
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
        return TypedResults.Ok(await user.CreateConversation(request.Model, request.InitialPrompt));
    }
}
