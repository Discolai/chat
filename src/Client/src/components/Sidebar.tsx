import {
  Box,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  Typography,
  IconButton,
  Button,
  Divider,
  useTheme,
} from "@mui/material";
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  Chat as ChatIcon,
} from "@mui/icons-material";
import { useNavigate, useParams } from "@tanstack/react-router";
import { useChatContext } from "@/contexts/ChatContext";
import { useCallback } from "react";
import { UserButton, useUser } from "@clerk/clerk-react";

const Sidebar: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useUser();
  const { conversationId: currentConversationId } = useParams({
    strict: false,
  });
  const { conversations, createConversation, deleteConversation } =
    useChatContext();

  const handleNewChat = useCallback(async () => {
    const newId = await createConversation(null, null);
    await navigate({
      to: "/conversation/$conversationId",
      params: { conversationId: newId },
    });
  }, [createConversation, navigate]);

  const handleSelectConversation = useCallback(
    async (id: string) => {
      await navigate({
        to: "/conversation/$conversationId",
        params: { conversationId: id },
      });
    },
    [navigate]
  );

  const handleDeleteConversation = useCallback(
    async (e: React.MouseEvent, id: string) => {
      e.stopPropagation();
      await deleteConversation(id);
      if (currentConversationId === id) {
        await navigate({ to: "/" });
      }
    },
    [currentConversationId, deleteConversation, navigate]
  );

  const theme = useTheme();

  return (
    <Box
      sx={{
        width: 260,
        backgroundColor: "background.paper",
        display: "flex",
        flexDirection: "column",
        borderRight: `1px solid ${theme.palette.divider}`,
      }}
    >
      <Box sx={{ p: 2 }}>
        <Button
          fullWidth
          variant="outlined"
          color="primary"
          startIcon={<AddIcon />}
          onClick={handleNewChat}
        >
          New chat
        </Button>
      </Box>

      <Divider />

      <Box sx={{ flex: 1, overflow: "auto" }}>
        {conversations.length === 0 ? (
          <Box sx={{ p: 2, textAlign: "center" }}>
            <Typography variant="body2" color="text.secondary">
              No conversations yet
            </Typography>
          </Box>
        ) : (
          <List sx={{ p: 0 }}>
            {conversations.map((conversation) => (
              <ListItem
                key={conversation.id}
                disablePadding
                sx={{
                  backgroundColor:
                    currentConversationId === conversation.id
                      ? "grey.100"
                      : "transparent",
                  "&:hover": {
                    backgroundColor: "grey.50",
                  },
                }}
              >
                <ListItemButton
                  onClick={() => handleSelectConversation(conversation.id!)}
                  sx={{
                    py: 1.5,
                    px: 2,
                    display: "flex",
                    alignItems: "center",
                    gap: 1,
                  }}
                >
                  <ChatIcon sx={{ fontSize: 16, color: "text.secondary" }} />
                  <ListItemText
                    primary={conversation.title}
                    slotProps={{
                      primary: {
                        fontSize: "14px",
                        noWrap: true,
                        color: "text.primary",
                      },
                    }}
                    sx={{ flex: 1 }}
                  />
                  <IconButton
                    size="small"
                    onClick={(e) =>
                      handleDeleteConversation(e, conversation.id!)
                    }
                    sx={{
                      color: "text.secondary",
                      "&:hover": {
                        color: "text.primary",
                        backgroundColor: "grey.100",
                      },
                    }}
                  >
                    <DeleteIcon sx={{ fontSize: 16 }} />
                  </IconButton>
                </ListItemButton>
              </ListItem>
            ))}
          </List>
        )}
      </Box>

      <Divider />
      <Box
        sx={{
          p: 2,
          display: "flex",
          alignItems: "center",
          gap: 2,
        }}
      >
        <UserButton />
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Typography
            variant="body2"
            sx={{
              color: "text.primary",
              fontSize: "14px",
              fontWeight: 500,
              overflow: "hidden",
              textOverflow: "ellipsis",
              whiteSpace: "nowrap",
            }}
          >
            {user?.firstName || user?.emailAddresses[0]?.emailAddress || "User"}
          </Typography>
          <Typography
            variant="caption"
            sx={{
              color: "text.secondary",
              fontSize: "12px",
              overflow: "hidden",
              textOverflow: "ellipsis",
              whiteSpace: "nowrap",
            }}
          >
            {user?.emailAddresses[0]?.emailAddress}
          </Typography>
        </Box>
      </Box>
    </Box>
  );
};

export default Sidebar;
