import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { RouterProvider, createRouter } from "@tanstack/react-router";

import "@fontsource/roboto/300.css";
import "@fontsource/roboto/400.css";
import "@fontsource/roboto/500.css";
import "@fontsource/roboto/700.css";
import "./app.css";

// Import the generated route tree
import { routeTree } from "./routeTree.gen";

import reportWebVitals from "./reportWebVitals.ts";
// import { ConversationsHubProvider } from "./contexts/ConversationsHub.tsx";
import { ChatProvider, useChatContext } from "./contexts/ChatContext.tsx";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { SignalRProvider } from "./contexts/SignalRContext.tsx";
import { ClerkProvider } from "@clerk/clerk-react";
import {
  ApiClientProvider,
  useApiClient,
} from "./contexts/ApiClientContext.tsx";
import { ThemeProvider } from "@mui/material";
import { useClerkAppereance } from "./hooks/useClerkAppereance.ts";
import { theme } from "./theme.ts";

const queryClient = new QueryClient();
// Create a new router instance
const router = createRouter({
  routeTree,
  context: {
    apiClient: null,
    chatContext: null!,
  },
  defaultPreload: "intent",
  scrollRestoration: true,
  defaultStructuralSharing: true,
  defaultPreloadStaleTime: 0,
});

const PUBLISHABLE_KEY = import.meta.env.VITE_CLERK_PUBLISHABLE_KEY;

if (!PUBLISHABLE_KEY) {
  throw new Error("Missing Publishable Key");
}

// Register the router instance for type safety
declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

const App = () => {
  const clerkApperance = useClerkAppereance();
  return (
    <ClerkProvider
      publishableKey={PUBLISHABLE_KEY}
      afterSignOutUrl="/"
      appearance={clerkApperance}
    >
      <SignalRProvider>
        <ApiClientProvider>
          <QueryClientProvider client={queryClient}>
            <ChatProvider>
              <RouterEntry />
            </ChatProvider>
          </QueryClientProvider>
        </ApiClientProvider>
      </SignalRProvider>
    </ClerkProvider>
  );
};

const RouterEntry = () => {
  const apiClient = useApiClient();
  const chatContext = useChatContext();
  return (
    <RouterProvider router={router} context={{ apiClient, chatContext }} />
  );
};

// Render the app
const rootElement = document.getElementById("app");
if (rootElement && !rootElement.innerHTML) {
  const root = createRoot(rootElement);
  root.render(
    <StrictMode>
      <ThemeProvider theme={theme}>
        <App />
      </ThemeProvider>
    </StrictMode>
  );
}

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();
