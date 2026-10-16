import { Alert, Typography } from "@mui/material";
import { ApiError } from "../shared/api/client";
export function ErrorNotice({ error }: { error: Error | null }) {
  return error ? (
    <Alert severity="error">
      {error.message}
      {error instanceof ApiError && (
        <Typography variant="caption" display="block">
          Support correlation: {error.correlationId}
        </Typography>
      )}
    </Alert>
  ) : null;
}
