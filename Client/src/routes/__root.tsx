import type { ApiClient } from "@/apiClient/apiClient";
import AuthPage from "@/components/AuthPage";
import type { ChatContext } from "@/contexts/ChatContext";
import { SignedOut, SignedIn } from "@clerk/clerk-react";
import { Box } from "@mui/material";
import { Outlet, createRootRouteWithContext } from "@tanstack/react-router";
import { TanStackRouterDevtools } from "@tanstack/react-router-devtools";

interface RouterContext {
  apiClient: ApiClient | null;
  chatContext: ChatContext;
}

export const Route = createRootRouteWithContext<RouterContext>()({
  component: () => (
    <>
      <SignedOut>
        <AuthPage />
      </SignedOut>
      <SignedIn>
        <Box sx={{ height: "100vh", display: "flex" }}>
          <Outlet />
          <TanStackRouterDevtools position="bottom-right" />
        </Box>
      </SignedIn>
    </>
  ),
});
