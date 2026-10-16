import { useContext } from "react";
import { DemoUserContext } from "../../features/demo-user/DemoUserContext";
import { api } from "./client";
export function useApi() {
  const { userId } = useContext(DemoUserContext);
  return <T>(path: string, options?: RequestInit) =>
    api<T>(path, {
      ...options,
      headers: { ...options?.headers, "X-Demo-User": userId },
    });
}
