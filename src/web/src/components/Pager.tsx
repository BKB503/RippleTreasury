import { Stack, Typography, Button } from "@mui/material";
export function Pager({
  page,
  total,
  setPage,
}: {
  page: number;
  total: number;
  setPage: (page: number) => void;
}) {
  return (
    <Stack
      direction="row"
      justifyContent="flex-end"
      alignItems="center"
      spacing={2}
      p={2}
    >
      <Typography variant="body2">
        Page {page} · {total} records
      </Typography>
      <Button disabled={page === 1} onClick={() => setPage(page - 1)}>
        Previous
      </Button>
      <Button disabled={page * 20 >= total} onClick={() => setPage(page + 1)}>
        Next
      </Button>
    </Stack>
  );
}
