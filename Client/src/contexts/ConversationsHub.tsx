import { createSignalRContext } from "react-signalr/signalr";
import type { ProviderProps } from "react-signalr/src/signalr/provider";

const { Provider, ...signalRContext } = createSignalRContext({
  shareConnectionBetweenTab: true,
});
export const ConversationsHub = { ...signalRContext };

export const ConversationsHubProvider = (props: Omit<ProviderProps, "url">) => {
  return (
    <Provider url="https://localhost:7111/hubs/conversations" {...props} />
  );
};
