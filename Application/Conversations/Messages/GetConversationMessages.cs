using Core.User;
using Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Application.Conversations.Messages;
public static class GetConversationMessages
{
    public async static Task<Results<Ok<IEnumerable<Message>>, NotFound, IResult>> Handle(
        Guid userId, Guid conversationId, [FromHeader(Name = "if-none-match")] string? ifNoneMatch, HttpContext httpContext, IClusterClient clusterClient)
    {
        var user = clusterClient.GetGrain<IUserGrain>(userId);
        if (user is null)
        {
            return TypedResults.NotFound();
        }
        var messages = await user.GetConversationMessages(conversationId, ifNoneMatch);
        if (messages.NoChange(ifNoneMatch))
        {
            return TypedResults.StatusCode(304);
        }
        httpContext.Response.Headers.ETag = messages.ETag;
        return TypedResults.Ok(messages.Messages);
    }
}