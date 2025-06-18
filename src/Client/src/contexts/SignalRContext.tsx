import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type DependencyList,
  type ReactNode,
} from "react";
import {
  HubConnectionBuilder,
  HubConnection,
  HttpTransportType,
} from "@microsoft/signalr";
import { useAuth } from "@clerk/clerk-react";

interface SignalRContextType {
  connection: HubConnection | null;
  useHubMethod: UseHubMethodFunction;
}

const SignalRContext = createContext<SignalRContextType | undefined>(undefined);

type UseHubMethodFunction = (
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  newMethod: (...args: any[]) => any,
  dependencies: DependencyList,
  method: string
) => void;

const createUseHubMethod = (connection: HubConnection | null) => {
  const useHubMethod: UseHubMethodFunction = (
    newMethod,
    dependencies,
    method
  ) => {
    useEffect(() => {
      connection?.on(method, newMethod);
      return () => {
        connection?.off(method, newMethod);
      };
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [method, newMethod, ...dependencies]);
  };

  return useHubMethod;
};

interface SignalRProviderProps {
  children: ReactNode;
}

export const useSignalRContext = () => {
  const context = useContext(SignalRContext);
  if (!context) {
    throw new Error("useSignalRContext must be used within a SignalRProvider");
  }
  return context;
};

export const SignalRProvider = ({ children }: SignalRProviderProps) => {
  const connectionRef = useRef<HubConnection | null>(null);
  const [connection, setConnection] = useState<HubConnection | null>(null);

  const { getToken, isSignedIn } = useAuth();

  useEffect(() => {
    if (!isSignedIn || connectionRef.current) {
      setConnection(connectionRef.current);
      return;
    }

    let url = "/hubs/conversations";
    if (import.meta.env.DEV) {
      url = import.meta.env.VITE_API_BASE + url;
    } else {
      url = window.location.origin + url;
    }

    const newConnection = new HubConnectionBuilder()
      .withAutomaticReconnect()
      .withUrl(url, {
        async accessTokenFactory() {
          return (await getToken())!;
        },
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets,
      })
      .build();

    connectionRef.current = newConnection;
    setConnection(newConnection);
    void newConnection.start();
  }, [getToken, isSignedIn]);

  const useHubMethod = useMemo(
    () => createUseHubMethod(connection),
    [connection]
  );

  const contextValue = useMemo(
    () => ({
      connection,
      useHubMethod,
    }),
    [connection, useHubMethod]
  );

  return (
    <SignalRContext.Provider value={contextValue}>
      {children}
    </SignalRContext.Provider>
  );
};
