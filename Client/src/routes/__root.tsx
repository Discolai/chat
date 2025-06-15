import type { ApiClient } from "@/apiClient/apiClient";
import type { ChatContext } from "@/contexts/ChatContext";
import { Box } from "@mui/material";
import { Outlet, createRootRouteWithContext } from "@tanstack/react-router";
import { TanStackRouterDevtools } from "@tanstack/react-router-devtools";

interface RouterContext {
  apiClient: ApiClient;
  chatContext: ChatContext;
}

export const Route = createRootRouteWithContext<RouterContext>()({
  component: () => (
    <Box sx={{ height: "100vh", display: "flex" }}>
      <Outlet />
      <TanStackRouterDevtools />
    </Box>
  ),
});
