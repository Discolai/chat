
using Core.User;
using Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Application.Conversations;

public class GetUserConversationsRequest
{
    public required Guid UserId { get; set; }
}

public static class GetUserConversations
{
    public static async Task<Results<Ok<IEnumerable<ConversationInfo>>, NotFound>> Handle(Guid userId, IClusterClient clusterClient)
    {
        var user = clusterClient.GetGrain<IUserGrain>(userId);
        if (user is null)
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok<IEnumerable<ConversationInfo>>(await user.GetConversations());
    }
}
