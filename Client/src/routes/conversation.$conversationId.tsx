import ChatArea from "@/components/ChatArea";
import Sidebar from "@/components/Sidebar";
import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/conversation/$conversationId")({
  component: RouteComponent,
  async loader(context) {
    await context.context.chatContext.loadConversation(
      context.params.conversationId
    );
  },
});

function RouteComponent() {
  return (
    <>
      <Sidebar />
      <ChatArea />
    </>
  );
}
