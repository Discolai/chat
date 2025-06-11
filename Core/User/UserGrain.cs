using Core.Conversation;
using Domain;

namespace Core.User;

[Alias("IUserGrain")]
public interface IUserGrain : IGrainWithGuidKey
{
    [Alias("CreateConversation")]
    Task<ConversationInfo> CreateConversation(AIModel model, string initialPrompt);

    [Alias("GetConversations")]
    ValueTask<List<ConversationInfo>> GetConversations();

    [Alias("GetConversationMessages")]
    ValueTask<IEnumerable<Message>> GetConversationMessages(Guid conversationId);
}

internal class UserGrain : Grain, IUserGrain
{
    private readonly IPersistentState<ConversationsState> _conversationsStore;

    public UserGrain([PersistentState("conversations")] IPersistentState<ConversationsState> conversationsState)
    {
        _conversationsStore = conversationsState;
    }

    public async Task<ConversationInfo> CreateConversation(AIModel model, string initialPrompt)
    {
        var conversationId = Guid.NewGuid();
        var conversation = GrainFactory.GetGrain<IConversationGrain>(conversationId, this.GetPrimaryKey().ToString());

        var conversationInfo = await conversation.Initialize(model, initialPrompt);
        
        if (!_conversationsStore.RecordExists)
        {
            _conversationsStore.State = new([]);
        }
        _conversationsStore.State.Conversations.Add(conversationInfo);
        await _conversationsStore.WriteStateAsync();
        return conversationInfo;
    }

    public async ValueTask<IEnumerable<Message>> GetConversationMessages(Guid conversationId)
    {
        if (!_conversationsStore.RecordExists || !_conversationsStore.State.Conversations.Any(x => x.Id == conversationId))
        {
            return [];
        }

        var conversation = GrainFactory.GetGrain<IConversationGrain>(conversationId, this.GetPrimaryKey().ToString());
        return await conversation.GetMessages();
    }

    public ValueTask<List<ConversationInfo>> GetConversations() => ValueTask.FromResult(_conversationsStore.State.Conversations);
}

[GenerateSerializer]
[Alias("ConversationsState")]
public record ConversationsState(List<ConversationInfo> Conversations);