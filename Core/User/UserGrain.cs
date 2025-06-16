using Core.Conversation;
using Domain;

namespace Core.User;

[Alias("IUserGrain")]
public interface IUserGrain : IGrainWithStringKey
{
    [Alias("CreateConversation")]
    Task<ConversationInfo> CreateConversation(AIModel model);

    [Alias("DeleteConversation")]
    Task<bool> DeleteConversation(Guid conversationId);

    [Alias("GetConversations")]
    ValueTask<List<ConversationInfo>> GetConversations();

    [Alias("GetConversationMessages")]
    ValueTask<GetMessagesResponse> GetConversationMessages(Guid conversationId, string? ifNoneMatch);
}

internal class UserGrain : Grain, IUserGrain
{
    private readonly IPersistentState<ConversationsState> _conversationsStore;

    public UserGrain([PersistentState("conversations")] IPersistentState<ConversationsState> conversationsState)
    {
        _conversationsStore = conversationsState;
    }

    public async Task<ConversationInfo> CreateConversation(AIModel model)
    {
        var conversationId = Guid.NewGuid();
        var conversation = GrainFactory.GetGrain<IConversationGrain>(conversationId, this.GetPrimaryKeyString());

        var conversationInfo = await conversation.Initialize(model);

        if (!_conversationsStore.RecordExists)
        {
            _conversationsStore.State = new([]);
        }
        _conversationsStore.State.Conversations.Add(conversationInfo);
        await _conversationsStore.WriteStateAsync();
        return conversationInfo;
    }

    public async Task<bool> DeleteConversation(Guid conversationId)
    {
        if (_conversationsStore.RecordExists)
        {
            return false;
        }
        var conversationInfo = _conversationsStore.State.Conversations.FirstOrDefault(x => x.Id == conversationId);
        if (conversationInfo is null)
        {
            return false;
        }
        await GrainFactory.GetGrain<IConversationGrain>(conversationId, this.GetPrimaryKeyString()).Delete();
        await _conversationsStore.WriteStateAsync();
        return true;
    }

    public async ValueTask<GetMessagesResponse> GetConversationMessages(Guid conversationId, string? ifNoneMatch)
    {
        if (!_conversationsStore.RecordExists || !_conversationsStore.State.Conversations.Any(x => x.Id == conversationId))
        {
            return GetMessagesResponse.Empty;
        }

        var conversation = GrainFactory.GetGrain<IConversationGrain>(conversationId, this.GetPrimaryKeyString());
        return await conversation.GetMessages(ifNoneMatch);
    }

    public ValueTask<List<ConversationInfo>> GetConversations() => ValueTask.FromResult(_conversationsStore.State.Conversations);
}

[GenerateSerializer]
[Alias("ConversationsState")]
public record ConversationsState(List<ConversationInfo> Conversations);