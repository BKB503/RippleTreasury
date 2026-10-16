import { useState } from "react";
import type { ReactNode } from "react";
import { DemoUserContext } from "./DemoUserContext";
export function DemoUserProvider({ children }: { children: ReactNode }) {
  const [userId, setUserId] = useState("alice.treasury");
  return (
    <DemoUserContext.Provider value={{ userId, setUserId }}>
      {children}
    </DemoUserContext.Provider>
  );
}
