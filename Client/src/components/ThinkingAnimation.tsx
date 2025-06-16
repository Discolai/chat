import { Box, keyframes } from "@mui/material";

const bounce = keyframes`
  0%, 60%, 100% {
    transform: translateY(0);
    opacity: 0.4;
  }
  30% {
    transform: translateY(-10px);
    opacity: 1;
  }
`;

export default function ThinkingAnimation() {
  return (
    <Box display="flex" alignItems="center" justifyContent="start" gap={0.5}>
      <Box
        sx={{
          width: 8,
          height: 8,
          borderRadius: "50%",
          backgroundColor: "primary.main",
          animation: `${bounce} 1.4s infinite ease-in-out`,
          animationDelay: "0s",
        }}
      />
      <Box
        sx={{
          width: 8,
          height: 8,
          borderRadius: "50%",
          backgroundColor: "primary.main",
          animation: `${bounce} 1.4s infinite ease-in-out`,
          animationDelay: "0.2s",
        }}
      />
      <Box
        sx={{
          width: 8,
          height: 8,
          borderRadius: "50%",
          backgroundColor: "primary.main",
          animation: `${bounce} 1.4s infinite ease-in-out`,
          animationDelay: "0.4s",
        }}
      />
    </Box>
  );
}
