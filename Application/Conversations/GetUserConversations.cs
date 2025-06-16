using Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Application.Conversations;
public static class GetUserConversations
{
    public static async Task<Results<Ok<IEnumerable<ConversationInfo>>, NotFound>> Handle(UserProvider userProvider)
    {
        if (!userProvider.TryGetUser(out var user, out _))
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok<IEnumerable<ConversationInfo>>(await user.GetConversations());
    }
}
