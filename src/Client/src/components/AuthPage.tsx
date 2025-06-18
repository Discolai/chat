import { Box } from "@mui/material";
import { SignIn, SignUp } from "@clerk/clerk-react";
import { useState } from "react";

const AuthPage: React.FC = () => {
  const [isSignUp] = useState(false);
  return (
    <Box
      sx={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
      }}
    >
      <Box
        sx={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
        }}
      >
        {isSignUp ? <SignUp /> : <SignIn />}
      </Box>
    </Box>
  );
};

export default AuthPage;
