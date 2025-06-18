using Core.User;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Application.Conversations;

public static class DeleteConversationEndpoint
{
    public static async Task<Results<Ok, NotFound>> Handle(Guid conversationId, UserProvider userProvider)
    {
        if (!userProvider.TryGetUser(out var user, out _))
        {
            return TypedResults.NotFound();
        }
        if (!await user.DeleteConversation(conversationId))
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok();
    }
}
