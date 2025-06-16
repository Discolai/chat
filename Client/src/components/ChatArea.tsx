import { Box } from "@mui/material";
import { MessagesSection } from "./MessagesSection";
import { MessageInput } from "./MessageInput";

const ChatArea = () => {
  return (
    <Box
      sx={{
        flex: 1,
        display: "flex",
        flexDirection: "column",
      }}
    >
      <MessagesSection />

      <Box
        sx={{
          p: 2,
          backgroundColor: "background.default",
        }}
      >
        <Box sx={{ maxWidth: "800px", mx: "auto" }}>
          <MessageInput />
        </Box>
      </Box>
    </Box>
  );
};

export default ChatArea;
