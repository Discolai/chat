import ChatArea from "@/components/ChatArea";
import Sidebar from "@/components/Sidebar";
import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/")({
  component: App,
});

function App() {
  return (
    <>
      <Sidebar />
      <ChatArea />
    </>
  );
}
