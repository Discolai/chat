import { MessageInputMutationKey } from "@/components/MessageInput";
import { useIsMutating } from "@tanstack/react-query";

export const useMessageIsProcessing = () => {
  return useIsMutating({ mutationKey: MessageInputMutationKey });
};
