import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Box,
  Button,
  Card,
  CircularProgress,
  Drawer,
  MenuItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { useApi, money } from "../../shared/api";
import type { Batch, Detail, Page, Payment } from "../../shared/api";
import {
  ErrorNotice,
  Heading,
  Pager,
  PaymentEvidence,
  Status,
} from "../../components";
export default function Batches() {
  const api = useApi();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("");
  const [selected, setSelected] = useState<Batch | null>(null);
  const [paymentPage, setPaymentPage] = useState(1);
  const [payment, setPayment] = useState<string | null>(null);
  const q = useQuery({
    queryKey: ["batches", page, status],
    queryFn: () =>
      api<Page<Batch>>(
        `/payment-batches?page=${page}&pageSize=20${status ? "&status=" + status : ""}`,
      ),
  });
  const p = useQuery({
    queryKey: ["payments", selected?.id, paymentPage],
    enabled: !!selected,
    queryFn: () =>
      api<Page<Payment>>(
        `/payment-batches/${selected!.id}/payments?page=${paymentPage}&pageSize=20`,
      ),
  });
  const d = useQuery({
    queryKey: ["payment", payment],
    enabled: !!payment,
    queryFn: () => api<Detail>(`/payments/${payment}`),
  });
  return (
    <>
      <Heading
        title="Batches"
        subtitle="Explore accepted instructions, provider routing and individual outcomes."
      />
      <TextField
        select
        label="Batch status"
        value={status}
        onChange={(e) => {
          setStatus(e.target.value);
          setPage(1);
        }}
        sx={{ minWidth: 220, mb: 3 }}
      >
        {[
          "",
          "Accepted",
          "Processing",
          "ActionRequired",
          "SubmissionCompleted",
          "SubmissionCompletedWithExceptions",
        ].map((s) => (
          <MenuItem key={s} value={s}>
            {s || "All statuses"}
          </MenuItem>
        ))}
      </TextField>
      <ErrorNotice error={q.error} />
      {q.isPending ? (
        <CircularProgress />
      ) : (
        <Card>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Client reference</TableCell>
                <TableCell>Received</TableCell>
                <TableCell>Payments</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Currency totals</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {q.data?.items.map((b) => (
                <TableRow
                  key={b.id}
                  hover
                  onClick={() => {
                    setSelected(b);
                    setPaymentPage(1);
                    setPayment(null);
                  }}
                  sx={{ cursor: "pointer" }}
                >
                  <TableCell>
                    <Button>{b.clientReference}</Button>
                  </TableCell>
                  <TableCell>
                    {new Date(b.receivedAt).toLocaleString()}
                  </TableCell>
                  <TableCell>{b.paymentCount}</TableCell>
                  <TableCell>
                    <Status value={b.status} />
                  </TableCell>
                  <TableCell>{b.totals.map(money).join(" · ")}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          {q.data?.total === 0 && (
            <Typography p={3}>
              No batches yet. Submit the demo through Swagger or the demo
              script.
            </Typography>
          )}
          <Pager page={page} total={q.data?.total ?? 0} setPage={setPage} />
        </Card>
      )}
      <Drawer
        anchor="right"
        open={!!selected}
        onClose={() => setSelected(null)}
      >
        <Box p={3} width={{ xs: 340, md: 1000 }}>
          <Stack direction="row" justifyContent="space-between">
            <Typography variant="h6">{selected?.clientReference}</Typography>
            <Button onClick={() => setSelected(null)}>Close</Button>
          </Stack>
          <ErrorNotice error={p.error} />
          {p.isPending ? (
            <CircularProgress />
          ) : (
            <>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Reference</TableCell>
                    <TableCell>Payer company</TableCell>
                    <TableCell>Payee</TableCell>
                    <TableCell>Purpose</TableCell>
                    <TableCell>Amount</TableCell>
                    <TableCell>Provider</TableCell>
                    <TableCell>Status</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {p.data?.items.map((x) => (
                    <TableRow
                      key={x.id}
                      hover
                      onClick={() => setPayment(x.id)}
                      sx={{ cursor: "pointer" }}
                    >
                      <TableCell>{x.clientReference}</TableCell>
                      <TableCell>
                        {x.parties?.payer.legalName ??
                          "Original details not supplied"}
                      </TableCell>
                      <TableCell>{x.beneficiary}</TableCell>
                      <TableCell>
                        {x.purpose.replace(/([a-z])([A-Z])/g, "$1 $2")}
                      </TableCell>
                      <TableCell>{money(x)}</TableCell>
                      <TableCell>{x.provider}</TableCell>
                      <TableCell>
                        <Status value={x.status} />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              <Pager
                page={paymentPage}
                total={p.data?.total ?? 0}
                setPage={setPaymentPage}
              />
            </>
          )}
          <ErrorNotice error={d.error} />
          {d.data && (
            <Box mt={3}>
              <PaymentEvidence detail={d.data} />
            </Box>
          )}
        </Box>
      </Drawer>
    </>
  );
}
