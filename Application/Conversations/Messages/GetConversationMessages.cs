using Core.User;
using Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Application.Conversations.Messages;
public static class GetConversationMessages
{
    public async static Task<Results<Ok<IEnumerable<Message>>, NotFound>> Handle(Guid userId, Guid conversationId, IClusterClient clusterClient)
    {
        var user = clusterClient.GetGrain<IUserGrain>(userId);
        if (user is null)
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(await user.GetConversationMessages(conversationId));
    }
}