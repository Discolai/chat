import { Box, Avatar, Paper, Typography } from "@mui/material";
import MarkdownRenderer from "./MarkdownRenderer";
import type { Message } from "@/apiClient/models";
import { Person as PersonIcon, SmartToy as BotIcon } from "@mui/icons-material";

interface MessageBoxProps {
  message: Message;
}

export const MessageBox = ({ message }: MessageBoxProps) => {
  return (
    <Box
      sx={{
        display: "flex",
        gap: 2,
        mb: 3,
        alignItems: "flex-start",
      }}
    >
      <Avatar
        sx={{
          width: 32,
          height: 32,
          backgroundColor: message.role === "user" ? "#10a37f" : "#f7f7f8",
          color: message.role === "user" ? "white" : "#10a37f",
        }}
      >
        {message.role === "user" ? <PersonIcon /> : <BotIcon />}
      </Avatar>
      <Paper
        elevation={0}
        sx={{
          flex: 1,
          p: 2,
          backgroundColor: message.role === "user" ? "#f7f7f8" : "transparent",
          border: message.role === "user" ? "1px solid #e5e5e5" : "none",
        }}
      >
        <Typography
          variant="body1"
          sx={{
            whiteSpace: "pre-wrap",
            wordBreak: "break-word",
            lineHeight: 1.6,
          }}
          component="div"
        >
          {message.role === "user" ? (
            message.content
          ) : (
            <MarkdownRenderer>{message.content}</MarkdownRenderer>
          )}
        </Typography>
      </Paper>
    </Box>
  );
};
