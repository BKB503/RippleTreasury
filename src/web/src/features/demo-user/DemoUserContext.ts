import { createContext } from "react";
export const DemoUserContext = createContext({
  userId: "alice.treasury",
  setUserId: (() => {}) as (id: string) => void,
});
