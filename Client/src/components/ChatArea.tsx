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
        backgroundColor: "#ffffff",
      }}
    >
      <MessagesSection />

      <Box
        sx={{
          p: 2,
          borderTop: "1px solid #e5e5e5",
          backgroundColor: "#ffffff",
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
