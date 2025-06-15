import { Box, Typography } from "@mui/material";
import { MessageBox } from "./Message";
import { useChatContext } from "@/contexts/ChatContext";
import { SmartToy as BotIcon } from "@mui/icons-material";
import { useRef, useEffect } from "react";

export const MessagesSection = () => {
  const { currentConversationMessages, currentStreamingMessage } =
    useChatContext();

  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [currentConversationMessages, currentStreamingMessage]);
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
          <BotIcon sx={{ fontSize: 48, color: "#10a37f" }} />
          <Typography variant="h5" color="text.secondary">
            How can I help you today?
          </Typography>
        </Box>
      ) : (
        <Box sx={{ maxWidth: "800px", mx: "auto", width: "100%" }}>
          {currentConversationMessages.messages.map((message) => (
            <MessageBox key={message.id} message={message} />
          ))}
        </Box>
      )}
      {currentStreamingMessage ? (
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
