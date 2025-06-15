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
import type { ApiClient } from "@/apiClient/apiClient";
import { HeadersInspectionOptions } from "@microsoft/kiota-http-fetchlibrary";

import { useQuery } from "@tanstack/react-query";
import { ConversationsHub } from "./ConversationsHub";

const userId = "73728b71-c7e3-4575-a72c-977603a9ada1";

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
  loadConversation: (id: string) => Promise<void>;

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

export const ChatProvider = ({
  children,
  apiClient,
}: {
  children: ReactNode;
  apiClient: ApiClient;
}) => {
  const { data: conversationsResponse } = useQuery({
    queryKey: ["conversations", userId],
    queryFn: async () =>
      apiClient.api.users.byUserId(userId).conversations.get(),
  });

  useEffect(() => {
    if (conversationsResponse) {
      setConversations(conversationsResponse);
    }
  }, [conversationsResponse]);
  const [currentConversationId, setCurrentConversationId] = useState<
    string | null
  >(null);
  const currentConversationIdRef = useRef<string | null>(null);

  // Workaround since useSignalREffect callbacks do not update when dependencies change
  useEffect(() => {
    currentConversationIdRef.current = currentConversationId;
  }, [currentConversationId]);

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

  ConversationsHub.useSignalREffect(
    "MessageStart",
    (conversationId: string) => {
      if (conversationId !== currentConversationIdRef.current) {
        return;
      }
      setCurrentStreamingMessage("");
    },
    []
  );

  ConversationsHub.useSignalREffect(
    "MessageContent",
    (conversationId: string, _: string, partialContent: string) => {
      if (conversationId !== currentConversationIdRef.current) {
        return;
      }
      setCurrentStreamingMessage((prev) => prev + partialContent);
    },
    []
  );

  ConversationsHub.useSignalREffect(
    "MessageEnd",
    (conversationId: string, message: Message, eTag: string) => {
      addMessage(conversationId, message, eTag);
      if (conversationId !== currentConversationIdRef.current) {
        return;
      }
      setCurrentStreamingMessage(null);
    },
    []
  );

  ConversationsHub.useSignalREffect(
    "PromptReceived",
    (conversationId: string, message: Message, eTag: string) => {
      addMessage(conversationId, message, eTag);
    },
    []
  );

  const createConversation = useCallback(
    async (initialPrompt: string | null | undefined) => {
      const newConversation = await apiClient.api.users
        .byUserId(userId)
        .conversations.post({
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
    [apiClient.api.users]
  );

  const loadConversation = useCallback(
    async (id: string) => {
      setCurrentConversationId(id);
      const messages = conversationMessages.current.get(id);
      const headers: Record<string, string> = {};

      if (messages?.eTag) {
        headers["if-none-match"] = messages.eTag;
      }

      const headerProbe = new HeadersInspectionOptions({
        inspectResponseHeaders: true,
      });
      const response = await apiClient.api.users
        .byUserId(userId)
        .conversations.byConversationId(id)
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
    },
    [apiClient.api.users]
  );

  const deleteConversation = useCallback(
    async (id: string) => {
      setConversations((prev) => prev.filter((c) => c.id !== id));
      if (currentConversation?.id === id) {
        await apiClient.api.users
          .byUserId(userId)
          .conversations.byConversationId(id)
          .delete();
        setCurrentConversationId(null);
      }
    },
    [apiClient.api.users, currentConversation?.id]
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
          await apiClient.api.users
            .byUserId(userId)
            .conversations.byConversationId(conversationId)
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
