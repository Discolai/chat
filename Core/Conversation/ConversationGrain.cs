using Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

namespace Core.Conversation;

[Alias("IConversationGrain")]
public interface IConversationGrain : IGrainWithGuidCompoundKey
{
    [Alias("SetModel")]
    Task<ConversationInfo> Initialize(AIModel model, string? initialPrompt);

    [Alias("Delete")]
    Task Delete();

    [Alias("Prompt")]
    Task Prompt(string prompt);

    [Alias("GetMessages")]
    ValueTask<GetMessagesResponse> GetMessages(string? ifNoneMatch);
}

internal class ConversationGrain : Grain, IConversationGrain
{
    private readonly IPersistentState<ConversationInfo> _infoStore;
    private readonly IPersistentState<MessagesState> _messagesStore;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly IHubContext<ConversationHub, IConversationClient> _conversationHub;
    private readonly ILogger<ConversationGrain> _logger;
    private readonly ChatHistory _chatHistory = [];
    private Task? _promptTask = null;
    private Guid _conversationId;
    private string _userId = null!;

    public ConversationGrain(
        [PersistentState("conversationInfo")] IPersistentState<ConversationInfo> infoState,
        [PersistentState("messages")] IPersistentState<MessagesState> messages,
        IChatCompletionService chatCompletionService, 
        IHubContext<ConversationHub, IConversationClient> conversationHub,
        ILogger<ConversationGrain> logger)
    {
        _infoStore = infoState;
        _messagesStore = messages;
        _chatCompletionService = chatCompletionService;
        _conversationHub = conversationHub;
        _logger = logger;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _chatHistory.AddSystemMessage("You are a chat assistant. If you need to perform any formatting of your output, always use markdown (commonmark).");
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

    public ValueTask<GetMessagesResponse> GetMessages(string? ifNoneMatch)
    {
        if (!_messagesStore.RecordExists)
        {
            return ValueTask.FromResult(GetMessagesResponse.Empty);
        }
        if (!string.IsNullOrEmpty(ifNoneMatch) &&  ifNoneMatch == _messagesStore.Etag)
        {
            return ValueTask.FromResult(GetMessagesResponse.Match(ifNoneMatch));
        }

        return ValueTask.FromResult(new GetMessagesResponse(_messagesStore.State.Messages, _messagesStore.Etag));
    }

    public async Task<ConversationInfo> Initialize(AIModel model, string? initialPrompt)
    {
        var title = "New chat";
        if (!string.IsNullOrEmpty(initialPrompt))
        {
            title = initialPrompt[..Math.Min(initialPrompt.Length, 30)];
        }
        if (!_infoStore.RecordExists)
        {
            _infoStore.State = new ConversationInfo { Id = this.GetPrimaryKey(), Model = model, Title = title };
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

        await _conversationHub.Clients.User(_userId).ConversationCreated(_infoStore.State);
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
        await _conversationHub.Clients.User(_userId).PromptReceived(_conversationId, promptMessage, _messagesStore.Etag);

        var responseMessage = new Message
        {
            Id = Guid.NewGuid(),
            Role = MessageRole.Assistant,
            Content = "",
        };
        var contentBuilder = new StringBuilder();

        await _conversationHub.Clients.User(_userId).MessageStart(_conversationId);
        try
        {
            await foreach (var item in _chatCompletionService.GetStreamingChatMessageContentsAsync(_chatHistory))
            {
                if (item?.Content is null)
                {
                    continue;
                }
                contentBuilder.Append(item.Content);
                await _conversationHub.Clients.User(_userId).MessageContent(_conversationId, responseMessage.Id, item.Content);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error producing chat response.");

            promptMessage.HasError = true;
            await _messagesStore.WriteStateAsync();
            await _conversationHub.Clients.User(_userId).MessageError(_conversationId, promptMessage.Id, _messagesStore.Etag);
            return;
        }
        responseMessage.Content = contentBuilder.ToString();
        _chatHistory.AddAssistantMessage(responseMessage.Content);
        _messagesStore.State.Messages.Add(responseMessage);
        await _messagesStore.WriteStateAsync();

        await _conversationHub.Clients.User(_userId).MessageEnd(_conversationId, responseMessage, _messagesStore.Etag);
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

[GenerateSerializer]
[Alias("Core.Conversation.GetMessagesResponse")]
public record GetMessagesResponse(IEnumerable<Message> Messages, string? ETag)
{
    public bool NoChange(string? ifNoMatch) => !string.IsNullOrEmpty(ifNoMatch) && ifNoMatch == ETag;

    public static GetMessagesResponse Match(string eTag) => new([], eTag);

    public static GetMessagesResponse Empty { get; } = new([], null);
}