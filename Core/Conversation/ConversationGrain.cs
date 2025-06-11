using Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Core.Conversation;

[Alias("IConversationGrain")]
public interface IConversationGrain : IGrainWithGuidCompoundKey
{
    [Alias("SetModel")]
    Task<ConversationInfo> Initialize(AIModel model, string initialPrompt);

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
    private readonly IHubContext<ConversationHub> _conversationHub;
    private readonly ChatHistory _chatHistory = [];
    private Task? _promptTask = null;

    public ConversationGrain([PersistentState("conversationInfo")] IPersistentState<ConversationInfo> infoState,
        [PersistentState("messages")] IPersistentState<MessagesState> messages, IChatCompletionService chatCompletionService, IHubContext<ConversationHub> conversationHub)
    {
        _infoStore = infoState;
        _messagesStore = messages;
        _chatCompletionService = chatCompletionService;
        _conversationHub = conversationHub;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (_messagesStore.RecordExists)
        {
            _chatHistory.AddRange(GetChatHistory(_messagesStore.State));
        }

        return base.OnActivateAsync(cancellationToken);
    }

    private static IEnumerable<ChatMessageContent> GetChatHistory(MessagesState messageState) => messageState.Messages.Select(x => new ChatMessageContent
    {
        Role = x.Type == MessageType.Prompt ? AuthorRole.User : AuthorRole.Assistant,
        Content = x.Content,
    });

    public ValueTask<IEnumerable<Message>> GetMessages() =>
        ValueTask.FromResult(_messagesStore.RecordExists ? (IEnumerable<Message>)_messagesStore.State.Messages : []);

    public async Task<ConversationInfo> Initialize(AIModel model, string initialPrompt)
    {
        if (!_infoStore.RecordExists)
        {
            _infoStore.State = new ConversationInfo { Id = this.GetPrimaryKey(), Model = model, Title = initialPrompt };
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
        _ = Prompt(initialPrompt);
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
        var persistedMessagesCount = _messagesStore.State.Messages.Count;
        DateTime messagesPersistedTimeStamp = DateTime.UtcNow;
        _chatHistory.AddUserMessage(prompt);
        _messagesStore.State.Messages.Add(new Message
        {
            Type = MessageType.Prompt,
            Content = prompt
        });

        await foreach (var item in _chatCompletionService.GetStreamingChatMessageContentsAsync(_chatHistory))
        {
            if (item?.Content is null)
            {
                continue;
            }
            _chatHistory.AddAssistantMessage(item.Content);
            _messagesStore.State.Messages.Add(new Message
            {
                Type = MessageType.Response,
                Content = item.Content,
            });
            await _conversationHub.Clients.All.SendAsync("message", item.Content);
            if (persistedMessagesCount != _messagesStore.State.Messages.Count && messagesPersistedTimeStamp - DateTime.UtcNow > TimeSpan.FromSeconds(5))
            {
                await _messagesStore.WriteStateAsync();
                persistedMessagesCount = _messagesStore.State.Messages.Count;
            }
        }

        // invoke model
        // handle reasoning
        // handle response
        if (persistedMessagesCount != _messagesStore.State.Messages.Count)
        {
            await _messagesStore.WriteStateAsync();
        }
    }
}

[GenerateSerializer]
[Alias("MessagesState")]
public record MessagesState(List<Message> Messages);