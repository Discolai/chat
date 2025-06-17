import { useChatContext } from "@/contexts/ChatContext";
import { Box, TextField, Paper, Button, Select, MenuItem } from "@mui/material";
import { useNavigate } from "@tanstack/react-router";
import { useCallback, useEffect, useState } from "react";
import NorthIcon from "@mui/icons-material/North";
import { useMutation } from "@tanstack/react-query";
import type { AIModel } from "@/apiClient/models";

export const MessageInput = () => {
  const {
    addChatMessage,
    createConversation,
    currentConversation,
    availableModels,
    switchModel,
  } = useChatContext();
  const navigate = useNavigate();
  const [message, setMessage] = useState("");

  const [selectedModel, setSelectedModel] = useState<AIModel | null>(null);
  useEffect(() => {
    setSelectedModel(
      currentConversation?.model ?? availableModels?.[0] ?? null
    );
  }, [availableModels, currentConversation?.model]);

  const handleMessageChange = useCallback<
    React.ChangeEventHandler<HTMLInputElement>
  >((e) => {
    setMessage(e.currentTarget.value);
  }, []);

  const sendMutation = useMutation({
    async mutationFn() {
      if (!message.trim()) return;

      let targetConversationId = currentConversation?.id;
      setMessage("");

      // If no conversation is selected, create a new one
      if (!targetConversationId) {
        targetConversationId = await createConversation(selectedModel, message);
        await navigate({
          to: "/conversation/$conversationId",
          params: { conversationId: targetConversationId },
        });
      } else {
        await addChatMessage(targetConversationId, message);
      }
    },
  });

  const handleKeyPress = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === "Enter" && !(e.shiftKey || e.ctrlKey)) {
        e.stopPropagation();
        e.preventDefault();
        sendMutation.mutate();
      }
    },
    [sendMutation]
  );

  const switchModelMutation = useMutation({
    async mutationFn(newModelName: string) {
      if (currentConversation == null) {
        setSelectedModel(
          availableModels?.find((x) => x.name === newModelName) ?? null
        );
        return;
      }
      await switchModel(currentConversation.id!, newModelName);
    },
  });

  return (
    <Paper
      sx={{ display: "flex", gap: 1, flexDirection: "column", padding: 2 }}
    >
      <TextField
        inputRef={(element: HTMLInputElement) => element?.focus()}
        fullWidth
        multiline
        value={message}
        onChange={handleMessageChange}
        minRows={2}
        maxRows={4}
        onKeyDown={handleKeyPress}
        placeholder="Write a message..."
        variant="standard"
      />
      <Box display="flex" justifyContent="space-between">
        {availableModels ? (
          <Select
            value={selectedModel?.name ?? ""}
            size="small"
            variant="standard"
            onChange={(e) => {
              switchModelMutation.mutate(e.target.value);
            }}
          >
            {availableModels?.map((x) => (
              <MenuItem key={x.name} value={x.name!}>
                {x.name}
              </MenuItem>
            ))}
          </Select>
        ) : null}
        <Button
          disabled={!message.trim()}
          onClick={() => sendMutation.mutate()}
          loading={sendMutation.isPending}
          variant="contained"
          color="primary"
          sx={{ minWidth: "unset", justifySelf: "flex-end" }}
        >
          <NorthIcon />
        </Button>
      </Box>
    </Paper>
  );
};
