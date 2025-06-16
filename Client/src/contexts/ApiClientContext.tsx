import { type ApiClient, createApiClient } from "@/apiClient/apiClient";
import { useAuth } from "@clerk/clerk-react";
import {
  type AccessTokenProvider,
  AllowedHostsValidator,
  AnonymousAuthenticationProvider,
  type AuthenticationProvider,
  BaseBearerTokenAuthenticationProvider,
} from "@microsoft/kiota-abstractions";
import { FetchRequestAdapter } from "@microsoft/kiota-http-fetchlibrary";
import { createContext, type ReactNode, useMemo, useContext } from "react";

interface ApiClientContextType {
  apiClient: ApiClient;
}

const ApiClientContext = createContext<ApiClientContextType | undefined>(
  undefined
);

class ClerkAccessTokenProvider implements AccessTokenProvider {
  getAuthorizationToken: (
    url?: string,
    additionalAuthenticationContext?: Record<string, unknown>
  ) => Promise<string>;

  constructor(getToken: ReturnType<typeof useAuth>["getToken"]) {
    this.getAuthorizationToken = async () => (await getToken())!;
  }

  public getAllowedHostsValidator() {
    return new AllowedHostsValidator();
  }
}

export const ApiClientProvider = ({ children }: { children: ReactNode }) => {
  const { getToken, isSignedIn } = useAuth();

  const apiClient = useMemo<ApiClient>(() => {
    let authProvider: AuthenticationProvider;
    if (!isSignedIn) {
      authProvider = new AnonymousAuthenticationProvider();
    } else {
      authProvider = new BaseBearerTokenAuthenticationProvider(
        new ClerkAccessTokenProvider(getToken)
      );
    }

    const adapter = new FetchRequestAdapter(authProvider);
    adapter.baseUrl = import.meta.env.VITE_API_BASE;

    return createApiClient(adapter);
  }, [getToken, isSignedIn]);

  return (
    <ApiClientContext.Provider value={{ apiClient }}>
      {children}
    </ApiClientContext.Provider>
  );
};

export const useApiClient = (): ApiClient => {
  const context = useContext(ApiClientContext);
  if (!context) {
    throw new Error("useApiClient must be used within an ApiClientProvider");
  }
  return context.apiClient;
};
