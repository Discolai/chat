import ChatArea from "@/components/ChatArea";
import Sidebar from "@/components/Sidebar";
import { createFileRoute, redirect } from "@tanstack/react-router";

export const Route = createFileRoute("/conversation/$conversationId")({
  component: RouteComponent,
  async loader(context) {
    if (
      !(await context.context.chatContext.loadConversation(
        context.params.conversationId
      ))
    ) {
      throw redirect({ to: "/" });
    }
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
