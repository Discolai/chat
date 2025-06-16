import { Box, Paper, Typography, useTheme } from "@mui/material";
import MarkdownRenderer from "./MarkdownRenderer";
import type { Message } from "@/apiClient/models";

interface MessageBoxProps {
  message: Message;
}

export const MessageBox = ({ message }: MessageBoxProps) => {
  const theme = useTheme();
  return (
    <Box
      sx={{
        display: "flex",
        gap: 2,
        mb: 3,
        alignItems: "flex-start",
        justifyContent: message.role === "user" ? "flex-end" : "flex-start",
      }}
    >
      <Paper
        elevation={0}
        sx={{
          p: 2,
          maxWidth: message.role === "user" ? "70%" : "100%",
          backgroundColor:
            message.role === "user" ? "background.paper" : "background.default",
          border:
            message.role === "user"
              ? `1px solid ${theme.palette.divider}`
              : "none",
          borderRadius:
            message.role === "user" ? "18px 18px 4px 18px" : undefined,
        }}
        component={"div"}
      >
        <Typography
          variant="body1"
          sx={{
            whiteSpace: "pre-wrap",
            wordBreak: "break-word",
            lineHeight: 1.6,
          }}
          component={"div"}
        >
          <MarkdownRenderer>{message.content}</MarkdownRenderer>
        </Typography>
      </Paper>
    </Box>
  );
};
