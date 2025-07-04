﻿using Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
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

    [Alias("SwitchModel")]
    Task SwitchModel(AIModel model);

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
    private readonly Kernel _kernel;
    private readonly IHubContext<ConversationHub, IConversationClient> _conversationHub;
    private readonly ILogger<ConversationGrain> _logger;
    private readonly ChatHistory _chatHistory = [];
    private Task? _promptTask = null;
    private Guid _conversationId;
    private string _userId = null!;

    public ConversationGrain(
        [PersistentState("conversationInfo")] IPersistentState<ConversationInfo> infoState,
        [PersistentState("messages")] IPersistentState<MessagesState> messages,
        Kernel kernel,
        IHubContext<ConversationHub, IConversationClient> conversationHub,
        ILogger<ConversationGrain> logger)
    {
        _infoStore = infoState;
        _messagesStore = messages;
        _kernel = kernel;
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
            _infoStore.State = new ConversationInfo(this.GetPrimaryKey(), model, title);
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

    public async Task SwitchModel(AIModel model)
    {
        if (!_messagesStore.RecordExists || model.Name == _infoStore.State.Model.Name)
        {
            return;
        }

        _infoStore.State = _infoStore.State with
        {
            Model = model,
        };

        await _infoStore.WriteStateAsync();
        await _conversationHub.Clients.User(_userId).ConversationInfoUpdated(_infoStore.State);
    }

    public Task Prompt(string prompt)
    {
        if (_promptTask is not null && !_promptTask.IsCompleted)
        {
            throw new InvalidOperationException("Another prompt is active");
        }
        _promptCts = new CancellationTokenSource();
        _promptTask = PerformPrompt(prompt, _promptCts);
        return Task.CompletedTask;
    }

    private CancellationTokenSource? _promptCts;

    private async Task PerformPrompt(string prompt, CancellationTokenSource cts)
    {
        using var _ = cts;

        var chatCompletionService = _kernel.Services.GetRequiredKeyedService<IChatCompletionService>(_infoStore.State.Model.Name);
        _chatHistory.AddUserMessage(prompt);
        var promptMessage = new Message
        {
            Id = Guid.NewGuid(),
            Role = MessageRole.User,
            Content = prompt
        };
        _messagesStore.State.Messages.Add(promptMessage);
        await _messagesStore.WriteStateAsync(cts.Token);
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
            await foreach (var item in chatCompletionService.GetStreamingChatMessageContentsAsync(_chatHistory, cancellationToken: cts.Token))
            {
                if (item?.Content is null)
                {
                    continue;
                }
                contentBuilder.Append(item.Content);
                await _conversationHub.Clients.User(_userId).MessageContent(_conversationId, responseMessage.Id, item.Content);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error producing chat response.");

            promptMessage.HasError = true;
            await _messagesStore.WriteStateAsync(cts.Token);
            await _conversationHub.Clients.User(_userId).MessageError(_conversationId, promptMessage.Id, _messagesStore.Etag);
            return;
        }
        responseMessage.Content = contentBuilder.ToString();
        _chatHistory.AddAssistantMessage(responseMessage.Content);
        _messagesStore.State.Messages.Add(responseMessage);
        await _messagesStore.WriteStateAsync(cts.Token);

        await _conversationHub.Clients.User(_userId).MessageEnd(_conversationId, responseMessage, _messagesStore.Etag);

        if (_promptCts == cts)
        {
            _promptCts = null;
        }
    }

    public async Task Delete()
    {
        _promptCts?.Cancel();
        if (_infoStore.RecordExists)
        {
            await _infoStore.ClearStateAsync();
        }
        if (_messagesStore.RecordExists)
        {
            await _messagesStore.ClearStateAsync();
        }
        await _conversationHub.Clients.User(_userId).ConversationDeleted(_conversationId);

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