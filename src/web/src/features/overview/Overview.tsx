import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Box,
  Card,
  CardContent,
  CircularProgress,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useApi, money } from "../../shared/api";
import type { Summary } from "../../shared/api";
import { ErrorNotice, Heading, Status } from "../../components";
export default function Overview() {
  const api = useApi();
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const params = new URLSearchParams();
  if (from) params.set("from", from + "T00:00:00Z");
  if (to) params.set("to", to + "T23:59:59.9999999Z");
  const q = useQuery({
    queryKey: ["summary", from, to],
    queryFn: () => api<Summary>("/operations/summary?" + params),
  });
  const data = q.data;
  const exceptions =
    (data?.statusCounts.OutcomeUnknown ?? 0) +
    (data?.statusCounts.ReconciliationRequired ?? 0);
  return (
    <>
      <Heading
        title="Dashboard & Reports"
        subtitle="A currency-aware view of outbound instructions and bank acknowledgements."
      />
      <Stack direction="row" spacing={2} mb={3}>
        <TextField
          type="date"
          label="Received from (UTC)"
          value={from}
          onChange={(e) => setFrom(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField
          type="date"
          label="Received to (UTC)"
          value={to}
          onChange={(e) => setTo(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
      </Stack>
      <ErrorNotice error={q.error} />
      {q.isPending ? (
        <CircularProgress />
      ) : (
        <>
          <Box
            display="grid"
            gridTemplateColumns="repeat(auto-fit,minmax(180px,1fr))"
            gap={2}
            mb={3}
          >
            {[
              ["Instructions", data?.total ?? 0],
              ["Bank accepted", data?.statusCounts.Accepted ?? 0],
              ["Rejected", data?.statusCounts.Rejected ?? 0],
              ["Needs reconciliation", exceptions],
            ].map(([label, count]) => (
              <Card key={label}>
                <CardContent>
                  <Typography color="text.secondary">{label}</Typography>
                  <Typography variant="h3" mt={1}>
                    {count}
                  </Typography>
                </CardContent>
              </Card>
            ))}
          </Box>
          <Box
            display="grid"
            gridTemplateColumns="repeat(auto-fit,minmax(280px,1fr))"
            gap={3}
          >
            <Card>
              <CardContent>
                <Typography variant="h6" mb={2}>
                  Instruction totals by currency
                </Typography>
                {data?.totals.length ? (
                  data.totals.map((t) => (
                    <Stack
                      key={t.currency}
                      direction="row"
                      justifyContent="space-between"
                      py={1}
                    >
                      <Typography>{t.currency}</Typography>
                      <Typography fontWeight={600}>{money(t)}</Typography>
                    </Stack>
                  ))
                ) : (
                  <Typography color="text.secondary">
                    No instructions in this period.
                  </Typography>
                )}
                <Typography variant="caption" color="text.secondary">
                  Intended amounts across all statuses. Accepted does not mean
                  settled.
                </Typography>
              </CardContent>
            </Card>
            <Card>
              <CardContent>
                <Typography variant="h6" mb={2}>
                  Processing status
                </Typography>
                {Object.entries(data?.statusCounts ?? {}).map(([s, n]) => (
                  <Stack
                    key={s}
                    direction="row"
                    justifyContent="space-between"
                    py={1}
                  >
                    <Status value={s} />
                    <Typography>{n}</Typography>
                  </Stack>
                ))}
              </CardContent>
            </Card>
          </Box>
        </>
      )}
    </>
  );
}
