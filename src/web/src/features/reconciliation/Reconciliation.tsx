import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import { useApi } from "../../shared/api";
import type { Case, CaseDetail, Page } from "../../shared/api";
import {
  ErrorNotice,
  Heading,
  Pager,
  PaymentEvidence,
  Status,
} from "../../components";
export default function Reconciliation() {
  const api = useApi();
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<string | null>(null);
  const [confirm, setConfirm] = useState(false);
  const [page, setPage] = useState(1);
  const q = useQuery({
    queryKey: ["cases", page],
    queryFn: () =>
      api<Page<Case>>(`/reconciliation-cases?page=${page}&pageSize=20`),
  });
  const d = useQuery({
    queryKey: ["case", selected],
    enabled: !!selected,
    queryFn: () => api<CaseDetail>(`/reconciliation-cases/${selected}`),
  });
  const mutation = useMutation({
    mutationFn: () =>
      api(`/reconciliation-cases/${selected}/recheck`, {
        method: "POST",
        headers: { "If-Match": `"${d.data!.case.version}"` },
        body: JSON.stringify({ commandId: crypto.randomUUID() }),
      }),
    onSuccess: () => {
      setConfirm(false);
      void queryClient.invalidateQueries();
    },
  });
  return (
    <>
      <Heading
        title="Reconciliation"
        subtitle="Investigate uncertain bank outcomes through evidence and safe status lookup."
      />
      <Alert severity="info" sx={{ mb: 3 }}>
        Operator actions are recorded in the audit. A status recheck queries the
        bank; it cannot submit a payment.
      </Alert>
      <ErrorNotice error={q.error} />
      <Box
        display="grid"
        gridTemplateColumns={{ xs: "1fr", md: "1fr 1.1fr" }}
        gap={3}
      >
        <Card>
          <Typography variant="h6" p={2}>
            Exception cases
          </Typography>
          {q.isPending ? (
            <CircularProgress sx={{ m: 3 }} />
          ) : q.data?.items.length ? (
            <>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Opened</TableCell>
                    <TableCell>Case status</TableCell>
                    <TableCell />
                  </TableRow>
                </TableHead>
                <TableBody>
                  {q.data.items.map((c) => (
                    <TableRow key={c.id} selected={selected === c.id}>
                      <TableCell>
                        {new Date(c.openedAt).toLocaleString()}
                      </TableCell>
                      <TableCell>
                        <Status value={c.status} />
                      </TableCell>
                      <TableCell>
                        <Button
                          onClick={() => {
                            setSelected(c.id);
                            mutation.reset();
                          }}
                        >
                          Inspect
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              <Pager page={page} total={q.data.total} setPage={setPage} />
            </>
          ) : (
            <Typography p={3} color="text.secondary">
              No reconciliation cases.
            </Typography>
          )}
        </Card>
        <Card>
          <CardContent>
            {!selected ? (
              <Typography color="text.secondary">
                Select a case to inspect its provider evidence and audit
                history.
              </Typography>
            ) : (
              <>
                <ErrorNotice error={d.error} />
                <ErrorNotice error={mutation.error} />
                {d.data ? (
                  <>
                    <Stack
                      direction="row"
                      spacing={2}
                      justifyContent="space-between"
                      mb={3}
                    >
                      <Status value={d.data.case.status} />
                      <Button
                        variant="contained"
                        disabled={
                          mutation.isPending ||
                          d.data.case.status === "Resolved" ||
                          d.data.case.status === "QueryingProvider"
                        }
                        onClick={() => setConfirm(true)}
                      >
                        Recheck bank status
                      </Button>
                    </Stack>
                    {mutation.isSuccess && (
                      <Alert severity="success" sx={{ mb: 2 }}>
                        Provider lookup completed. Current bank evidence is
                        shown below.
                      </Alert>
                    )}
                    <PaymentEvidence detail={d.data.detail} />
                  </>
                ) : (
                  <CircularProgress />
                )}
              </>
            )}
          </CardContent>
        </Card>
      </Box>
      <Dialog
        open={confirm}
        onClose={() => !mutation.isPending && setConfirm(false)}
      >
        <DialogTitle>Recheck bank status?</DialogTitle>
        <DialogContent>
          <ErrorNotice error={mutation.error} />
          This performs a read-only provider lookup and records the evidence in
          the audit history.
        </DialogContent>
        <DialogActions>
          <Button
            disabled={mutation.isPending}
            onClick={() => setConfirm(false)}
          >
            Cancel
          </Button>
          <Button
            disabled={mutation.isPending}
            variant="contained"
            onClick={() => mutation.mutate()}
          >
            {mutation.isPending ? "Checking…" : "Confirm recheck"}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
