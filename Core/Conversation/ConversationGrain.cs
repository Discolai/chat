using Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

namespace Core.Conversation;

[Alias("IConversationGrain")]
public interface IConversationGrain : IGrainWithGuidCompoundKey
{
    [Alias("SetModel")]
    Task<ConversationInfo> Initialize(AIModel model);

    [Alias("Delete")]
    Task Delete();

    [Alias("Prompt")]
    Task Prompt(string prompt);

    [Alias("GetMessages")]
    ValueTask<IEnumerable<Message>> GetMessages();
}

internal class ConversationGrain : Grain, IConversationGrain
{
    private readonly IPersistentState<ConversationInfo> _infoStore;
    private readonly IPersistentState<MessagesState> _messagesStore;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly IHubContext<ConversationHub, IConversationClient> _conversationHub;
    private readonly ChatHistory _chatHistory = [];
    private Task? _promptTask = null;
    private Guid _conversationId;
    private string _userId = null!;

    public ConversationGrain([PersistentState("conversationInfo")] IPersistentState<ConversationInfo> infoState,
        [PersistentState("messages")] IPersistentState<MessagesState> messages, IChatCompletionService chatCompletionService, IHubContext<ConversationHub, IConversationClient> conversationHub)
    {
        _infoStore = infoState;
        _messagesStore = messages;
        _chatCompletionService = chatCompletionService;
        _conversationHub = conversationHub;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _chatHistory.AddSystemMessage("You are a chat assistant. Your output should always be written as markdown.");
        if (_messagesStore.RecordExists)
        {
            _chatHistory.AddRange(GetChatHistory(_messagesStore.State));
        }

        _conversationId = this.GetPrimaryKey(out _userId);
        return base.OnActivateAsync(cancellationToken);
    }

    private static IEnumerable<ChatMessageContent> GetChatHistory(MessagesState messageState) => messageState.Messages.Select(x => new ChatMessageContent
    {
        Role = x.Role == MessageRole.User ? AuthorRole.User : AuthorRole.Assistant,
        Content = x.Content,
    });

    public ValueTask<IEnumerable<Message>> GetMessages() =>
        ValueTask.FromResult(_messagesStore.RecordExists ? (IEnumerable<Message>)_messagesStore.State.Messages : []);

    public async Task<ConversationInfo> Initialize(AIModel model)
    {
        if (!_infoStore.RecordExists)
        {
            _infoStore.State = new ConversationInfo { Id = this.GetPrimaryKey(), Model = model, Title = "New chat" };
        }
        else
        {
            throw new ConversationAlreadyInitializedException();
        }

        if (!_messagesStore.RecordExists)
        {
            _messagesStore.State = new([]);
        }
        else
        {
            throw new ConversationAlreadyInitializedException();
        }
        await _infoStore.WriteStateAsync();
        await _messagesStore.WriteStateAsync();
        return _infoStore.State;
    }

    public Task Prompt(string prompt)
    {
        if (_promptTask is not null && !_promptTask.IsCompleted)
        {
            throw new InvalidOperationException("Another prompt is active");
        }
        _promptTask = PerformPrompt(prompt);
        return Task.CompletedTask;
    }

    private async Task PerformPrompt(string prompt)
    {
        _chatHistory.AddUserMessage(prompt);
        var promptMessage = new Message
        {
            Id = Guid.NewGuid(),
            Role = MessageRole.User,
            Content = prompt
        };
        _messagesStore.State.Messages.Add(promptMessage);
        await _messagesStore.WriteStateAsync();
        await _conversationHub.Clients.All.PromptReceived(_conversationId, promptMessage, _messagesStore.Etag);

        var responseMessage = new Message
        {
            Id = Guid.NewGuid(),
            Role = MessageRole.Assistant,
            Content = "",
        };
        _messagesStore.State.Messages.Add(responseMessage);
        var contentBuilder = new StringBuilder();

        await _conversationHub.Clients.All.MessageStart(_conversationId);

        await foreach (var item in _chatCompletionService.GetStreamingChatMessageContentsAsync(_chatHistory))
        {
            if (item?.Content is null)
            {
                continue;
            }
            contentBuilder.Append(item.Content);
            await _conversationHub.Clients.All.MessageContent(_conversationId, responseMessage.Id, item.Content);
        }
        responseMessage.Content = contentBuilder.ToString();
        _chatHistory.AddAssistantMessage(responseMessage.Content);
        await _messagesStore.WriteStateAsync();

        await _conversationHub.Clients.All.MessageEnd(_conversationId, responseMessage, _messagesStore.Etag);
    }

    public async Task Delete()
    {
        if (_infoStore.RecordExists)
        {
            await _infoStore.ClearStateAsync();
        }
        if (_messagesStore.RecordExists)
        {
            await _messagesStore.ClearStateAsync();
        }

        DeactivateOnIdle();
    }
}

[GenerateSerializer]
[Alias("MessagesState")]
public record MessagesState(List<Message> Messages);