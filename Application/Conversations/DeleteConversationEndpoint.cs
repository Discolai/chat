using Core.User;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Application.Conversations;

public static class DeleteConversationEndpoint
{
    public static async Task<Results<Ok, NotFound>> Handle(Guid userId, Guid conversationId, IClusterClient clusterClient)
    {
        var user = clusterClient.GetGrain<IUserGrain>(userId);
        if (!await user.DeleteConversation(conversationId))
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok();
    }
}
