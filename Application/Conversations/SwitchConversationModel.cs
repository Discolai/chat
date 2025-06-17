using Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Application.Conversations;

public record SwitchConversationModelRequest(string Model);

public static class SwitchConversationModel
{
    public static async Task<Results<Ok, NotFound, BadRequest>> Handle(
        Guid conversationId,
        [FromBody]SwitchConversationModelRequest request,
        UserProvider userProvider,
        IClusterClient clusterClient,
        IOptions<AIModelConfiguration> aIModelConfiguration)
    {
        if (!userProvider.TryGetUser(out var user, out _))
        {
            return TypedResults.NotFound();
        }
        var model = aIModelConfiguration.Value.Models.FirstOrDefault(x => x.Name == request.Model);
        if (model is null)
        {
            return TypedResults.BadRequest();
        }
        await user.SwitchModel(conversationId, model.ToAIModel());
        return TypedResults.Ok();
    }
}