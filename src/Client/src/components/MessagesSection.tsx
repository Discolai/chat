import { Box, Typography } from "@mui/material";
import { MessageBox } from "./MessageBox";
import { useChatContext } from "@/contexts/ChatContext";
import { useRef, useEffect, useCallback, useMemo } from "react";
import ThinkingAnimation from "./ThinkingAnimation";

export const MessagesSection = () => {
  const { currentConversationMessages, currentStreamingMessage, isThinking } =
    useChatContext();

  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = useCallback(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, []);

  useEffect(() => {
    scrollToBottom();
  }, [currentConversationMessages, currentStreamingMessage, scrollToBottom]);

  const shouldRenderThinkingAnimation = useMemo(() => {
    if (isThinking && !currentStreamingMessage) {
      return true;
    }
    if (
      currentStreamingMessage ||
      !currentConversationMessages?.messages ||
      currentConversationMessages.messages.length === 0
    ) {
      return false;
    }

    const lastMessage =
      currentConversationMessages.messages[
        currentConversationMessages.messages.length - 1
      ];
    return lastMessage.role === "user";
  }, [
    currentConversationMessages?.messages,
    currentStreamingMessage,
    isThinking,
  ]);

  return (
    <Box
      sx={{
        flex: 1,
        overflowY: "auto",
        p: 2,
        display: "flex",
        flexDirection: "column",
      }}
    >
      {!currentConversationMessages ||
      currentConversationMessages.messages.length === 0 ? (
        <Box
          sx={{
            flex: 1,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            flexDirection: "column",
            gap: 2,
          }}
        >
          <Typography variant="h5" color="text.secondary">
            How can I help you today?
          </Typography>
        </Box>
      ) : (
        <Box sx={{ maxWidth: "800px", mx: "auto", width: "100%" }}>
          {currentConversationMessages.messages.map((message) => (
            <MessageBox key={message.id} message={message} />
          ))}
          {shouldRenderThinkingAnimation ? <ThinkingAnimation /> : null}
        </Box>
      )}
      {currentStreamingMessage != null ? (
        <Box sx={{ maxWidth: "800px", mx: "auto", width: "100%" }}>
          <MessageBox
            message={{
              id: "partial",
              content: currentStreamingMessage,
              role: "assistant",
            }}
          />
        </Box>
      ) : null}
      <div ref={messagesEndRef} />
    </Box>
  );
};
