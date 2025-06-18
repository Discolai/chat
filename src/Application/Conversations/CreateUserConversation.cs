using Core.Conversation;
using Core.User;
using Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Application.Conversations;

public class CreateUserConversationRequest
{
    public required string Model { get; set; }
    public string? InitialPrompt { get; set; }
}

public static class CreateUserConversation
{
    public static async Task<Results<Ok<ConversationInfo>, NotFound, BadRequest>> Handle(
        CreateUserConversationRequest request,
        IClusterClient clusterClient,
        UserProvider userProvider,
        IOptions<AIModelConfiguration> aIModelConfiguration)
    {
        if (!userProvider.TryGetUser(out var user, out var userId))
        {
            return TypedResults.NotFound();
        }
        var model = aIModelConfiguration.Value.Models.FirstOrDefault(x => x.Name == request.Model);
        if (model is null)
        {
            return TypedResults.BadRequest();
        }

        var conversationInfo = await user.CreateConversation(model.ToAIModel(), request.InitialPrompt);
        if (!string.IsNullOrEmpty(request.InitialPrompt))
        {
            await clusterClient
                .GetGrain<IConversationGrain>(conversationInfo.Id, userId)
                .Prompt(request.InitialPrompt);
        }
        return TypedResults.Ok(conversationInfo);
    }
}
