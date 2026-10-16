import { useContext } from "react";
import { useQuery } from "@tanstack/react-query";
import { MenuItem, TextField } from "@mui/material";
import { api } from "../../shared/api/client";
import { DemoUserContext } from "./DemoUserContext";
type DemoUser = { id: string; displayName: string; kind: string };
export function DemoUserSelector() {
  const { userId, setUserId } = useContext(DemoUserContext);
  const users = useQuery({
    queryKey: ["demo-users"],
    queryFn: () => api<DemoUser[]>("/demo/users"),
    retry: false,
    refetchInterval: false,
  });
  if (!users.data) return null;
  return (
    <TextField
      select
      size="small"
      label="Operator"
      value={userId}
      onChange={(e) => setUserId(e.target.value)}
      sx={{ minWidth: 160, background: "white", borderRadius: 1 }}
    >
      {users.data.map((user) => (
        <MenuItem key={user.id} value={user.id}>
          {user.displayName}
        </MenuItem>
      ))}
    </TextField>
  );
}
