import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
// import { ConversationsHub } from "./ConversationsHub";
import type { ConversationInfo, Message } from "@/apiClient/models";
import { HeadersInspectionOptions } from "@microsoft/kiota-http-fetchlibrary";

import { useQuery } from "@tanstack/react-query";
import { useSignalRContext } from "./SignalRContext";
import { useApiClient } from "./ApiClientContext";
import { useAuth } from "@clerk/clerk-react";

interface ConversationMessages {
  messages: Message[];
  eTag?: string;
}

export interface ChatContext {
  conversations: ConversationInfo[];
  currentConversationMessages: ConversationMessages | null;
  currentConversation: ConversationInfo | null;
  currentStreamingMessage: string | null;
  createConversation: (initialPrompt: string | null) => Promise<string>;
  loadConversation: (id: string) => Promise<boolean>;

  addChatMessage: (conversationId: string, message: string) => Promise<void>;
  deleteConversation: (id: string) => Promise<void>;
}

const ChatContext = createContext<ChatContext | undefined>(undefined);

export const useChatContext = () => {
  const context = useContext(ChatContext);
  if (!context) {
    throw new Error("useChatContext must be used within a ChatProvider");
  }
  return context;
};

export const ChatProvider = ({ children }: { children: ReactNode }) => {
  const apiClient = useApiClient();
  const { isSignedIn } = useAuth();
  const { data: conversationsResponse } = useQuery({
    queryKey: ["conversations"],
    queryFn: () => apiClient.api.conversations.get(),
    enabled: isSignedIn,
  });

  useEffect(() => {
    if (conversationsResponse) {
      setConversations(conversationsResponse);
    }
  }, [conversationsResponse]);
  const [currentConversationId, setCurrentConversationId] = useState<
    string | null
  >(null);

  const [conversations, setConversations] = useState<ConversationInfo[]>([]);
  const currentConversation = useMemo(
    () =>
      currentConversationId
        ? conversations?.find((x) => x.id === currentConversationId) ?? null
        : null,
    [conversations, currentConversationId]
  );
  const [currentStreamingMessage, setCurrentStreamingMessage] = useState<
    string | null
  >(null);

  const conversationMessages = useRef(new Map<string, ConversationMessages>());
  const [currentConversationMessages, setCurrentConversationMessages] =
    useState<ConversationMessages | null>(null);

  const addMessage = useCallback(
    (conversationId: string, message: Message, eTag: string) => {
      let messages = conversationMessages.current.get(conversationId);
      if (messages) {
        messages.eTag = eTag;
        messages.messages.push(message);
        return;
      } else {
        messages = {
          eTag,
          messages: [message],
        };
        conversationMessages.current.set(conversationId, messages);
      }
      if (conversationId === currentConversationId) {
        setCurrentConversationMessages({ ...messages });
      }
    },
    [currentConversationId]
  );

  const { useHubMethod } = useSignalRContext();

  useHubMethod(
    (conversationId: string) => {
      if (conversationId !== currentConversationId) {
        return;
      }
      setCurrentStreamingMessage("");
    },
    [currentConversationId],
    "MessageStart"
  );

  useHubMethod(
    (conversationId: string, _: string, partialContent: string) => {
      if (conversationId !== currentConversationId) {
        return;
      }
      setCurrentStreamingMessage((prev) => prev + partialContent);
    },
    [currentConversationId],
    "MessageContent"
  );

  useHubMethod(
    (conversationId: string, message: Message, eTag: string) => {
      addMessage(conversationId, message, eTag);
      if (conversationId !== currentConversationId) {
        return;
      }
      setCurrentStreamingMessage(null);
    },
    [addMessage, currentConversationId],
    "MessageEnd"
  );

  useHubMethod(
    (conversationId: string, message: Message, eTag: string) => {
      addMessage(conversationId, message, eTag);
    },
    [addMessage],
    "PromptReceived"
  );

  useHubMethod(
    (conversationId: string, promptMessageId: string, eTag: string) => {
      const messages = conversationMessages.current.get(conversationId);
      if (!messages) {
        return;
      }
      messages.eTag = eTag;
      messages.messages = messages.messages.map((message) => {
        if (message.id === promptMessageId) {
          message.hasError = true;
        }
        return message;
      });
      if (conversationId === currentConversationId) {
        setCurrentConversationMessages(messages);
      }
    },
    [currentConversationId],
    "MessageError"
  );

  const createConversation = useCallback(
    async (initialPrompt: string | null | undefined) => {
      const newConversation = await apiClient.api.conversations.post({
        model: {
          name: "some-model",
          provider: "some-provider",
          version: "1",
        },
        initialPrompt: initialPrompt,
      });
      if (!newConversation) {
        throw new Error("Could not create conversation");
      }

      setConversations((prev) => [newConversation, ...prev]);
      return newConversation.id!;
    },
    [apiClient.api.conversations]
  );

  const loadConversation = useCallback(
    async (id: string) => {
      if (!isSignedIn) {
        return false;
      }
      setCurrentConversationId(id);
      const messages = conversationMessages.current.get(id);
      const headers: Record<string, string> = {};

      if (messages?.eTag) {
        headers["if-none-match"] = messages.eTag;
      }

      const headerProbe = new HeadersInspectionOptions({
        inspectResponseHeaders: true,
      });
      const response = await apiClient.api.conversations
        .byConversationId(id)
        .messages.get({
          headers,
          options: [headerProbe],
        });
      if (response) {
        const responseEtag = headerProbe.getResponseHeaders().get("eTag");
        const newMessages = {
          messages: response,
          eTag:
            typeof responseEtag === "object" && responseEtag.size > 0
              ? [...responseEtag][0]
              : undefined,
        };
        conversationMessages.current.set(id, newMessages);
        setCurrentConversationMessages(newMessages);
      } else {
        setCurrentConversationMessages(messages ?? null);
      }
      return true;
    },
    [apiClient.api.conversations, isSignedIn]
  );

  const deleteConversation = useCallback(
    async (id: string) => {
      setConversations((prev) => prev.filter((c) => c.id !== id));
      if (currentConversation?.id === id) {
        await apiClient.api.conversations.byConversationId(id).delete();
        setCurrentConversationId(null);
      }
    },
    [apiClient.api.conversations, currentConversation?.id]
  );

  return (
    <ChatContext.Provider
      value={{
        conversations,
        currentConversation,
        currentConversationMessages,
        currentStreamingMessage,
        createConversation,
        loadConversation: loadConversation,
        addChatMessage: async (conversationId, message) => {
          await apiClient.api.conversations
            .byConversationId(conversationId)
            .prompt.post({
              prompt: message,
            });
        },
        deleteConversation,
      }}
    >
      {children}
    </ChatContext.Provider>
  );
};
