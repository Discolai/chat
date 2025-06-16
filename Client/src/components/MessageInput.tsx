import { useChatContext } from "@/contexts/ChatContext";
import { Box, TextField, IconButton } from "@mui/material";
import { useNavigate } from "@tanstack/react-router";
import { useCallback, useState } from "react";
import { Send as SendIcon } from "@mui/icons-material";

export const MessageInput = () => {
  const { addChatMessage, createConversation, currentConversation } =
    useChatContext();
  const navigate = useNavigate();
  const [message, setMessage] = useState("");
  const handleMessageChange = useCallback<
    React.ChangeEventHandler<HTMLInputElement>
  >((e) => {
    setMessage(e.currentTarget.value);
  }, []);

  const handleSendMessage = useCallback(async () => {
    if (!message.trim()) return;

    let targetConversationId = currentConversation?.id;

    // If no conversation is selected, create a new one
    if (!targetConversationId) {
      targetConversationId = await createConversation(message);
      await navigate({
        to: "/conversation/$conversationId",
        params: { conversationId: targetConversationId },
      });
    } else {
      await addChatMessage(targetConversationId, message);
    }
    setMessage("");
  }, [
    addChatMessage,
    createConversation,
    currentConversation?.id,
    message,
    navigate,
  ]);

  const handleKeyPress = useCallback(
    async (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === "Enter" && !(e.shiftKey || e.ctrlKey)) {
        e.stopPropagation();
        e.preventDefault();
        await handleSendMessage();
      }
    },
    [handleSendMessage]
  );

  return (
    <Box sx={{ display: "flex", gap: 1, alignItems: "flex-end" }}>
      <TextField
        inputRef={(element: HTMLInputElement) => element?.focus()}
        fullWidth
        multiline
        value={message}
        onChange={handleMessageChange}
        maxRows={4}
        onKeyDown={handleKeyPress}
        placeholder="How can I help you today?"
        variant="outlined"
        sx={{
          "& .MuiOutlinedInput-root": {
            borderRadius: 2,
            backgroundColor: "#f7f7f8",
            "& fieldset": {
              borderColor: "#e5e5e5",
            },
            "&:hover fieldset": {
              borderColor: "#10a37f",
            },
            "&.Mui-focused fieldset": {
              borderColor: "#10a37f",
            },
          },
        }}
      />
      <IconButton
        disabled={!message.trim()}
        onClick={() => handleSendMessage()}
        sx={{
          backgroundColor: "#10a37f",
          color: "white",
          "&:hover": {
            backgroundColor: "#0d8f6f",
          },
          "&:disabled": {
            backgroundColor: "#e5e5e5",
            color: "#999",
          },
        }}
      >
        <SendIcon />
      </IconButton>
    </Box>
  );
};
