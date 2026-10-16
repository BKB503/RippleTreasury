import { Chip } from "@mui/material";
export function Status({ value }: { value: string }) {
  return (
    <Chip
      size="small"
      label={value}
      color={
        value === "Accepted" || value === "Resolved"
          ? "success"
          : value === "Rejected"
            ? "error"
            : value.includes("Unknown") || value === "ActionRequired"
              ? "warning"
              : "default"
      }
      variant="outlined"
    />
  );
}
