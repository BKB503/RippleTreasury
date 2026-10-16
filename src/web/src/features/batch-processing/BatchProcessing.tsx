import { useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert,
  Box,
  Button,
  Card,
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
import { useApi } from "../../shared/api";
import type { Batch } from "../batches/types";
import { ErrorNotice, Heading, Status } from "../../components";
import { companies, generateBatch, randomSeed } from "./generateBatch";
import { purposeNames } from "./types";
import type { Draft, Receipt, FundingAccount } from "./types";

export default function BatchProcessing() {
  const api = useApi();
  const queries = useQueryClient();
  const funding = useQuery({
    queryKey: ["funding-accounts"],
    queryFn: () => api<FundingAccount[]>("/funding-accounts"),
  });
  const [count, setCount] = useState(100);
  const [seed, setSeed] = useState(randomSeed);
  const [payerReference, setPayerReference] = useState(companies[0].reference);
  const [executionDate, setExecutionDate] = useState(
    new Date().toISOString().slice(0, 10),
  );
  const [mode, setMode] = useState<"accepted" | "mixed">("mixed");
  const [draft, setDraft] = useState<Draft | null>(null);
  const submission = useMutation({
    mutationFn: (value: Draft) =>
      api<Receipt>("/payment-batches", {
        method: "POST",
        headers: { "Idempotency-Key": value.key },
        body: JSON.stringify(value.input),
      }),
    onSuccess: () => {
      void queries.invalidateQueries();
    },
  });
  const progress = useQuery({
    queryKey: ["batch", submission.data?.id],
    enabled: !!submission.data,
    queryFn: () => api<Batch>(`/payment-batches/${submission.data!.id}`),
  });
  const resetDraft = () => {
    setDraft(null);
    submission.reset();
  };
  const valid =
    Number.isInteger(count) &&
    count >= 1 &&
    count <= 500 &&
    Number.isInteger(seed) &&
    seed >= 0 &&
    seed <= 4294967295 &&
    !!executionDate &&
    ["GBP", "EUR", "USD"].every((currency) => funding.data?.some((account) =>
      account.ownerReference === payerReference && account.currency === currency));

  return (
    <>
      <Heading
        title="Process batch"
        subtitle="Generate named payment instructions, review them and submit a batch for background processing."
      />
      <Alert severity="info" sx={{ mb: 3 }}>
        Review the preview before submitting. The API validates the batch and
        the worker processes each payment independently. Funding currencies must match payout currencies; no FX is performed. The requested UTC date is not a settlement guarantee.
      </Alert>
      <Card sx={{ p: 3, mb: 3 }}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={2} mb={2}>
          <TextField
            label="Payment count"
            type="number"
            value={count}
            disabled={submission.isPending}
            onChange={(event) => {
              setCount(Number(event.target.value));
              resetDraft();
            }}
            helperText="1 to 500 payments"
            slotProps={{ htmlInput: { min: 1, max: 500 } }}
          />
          <TextField
            select
            label="Payer company"
            value={payerReference}
            disabled={submission.isPending}
            sx={{ minWidth: 260 }}
            onChange={(event) => {
              setPayerReference(event.target.value);
              resetDraft();
            }}
          >
            {companies.map((company) => (
              <MenuItem key={company.reference} value={company.reference}>
                {company.legalName}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            label="Outcome scenario"
            value={mode}
            disabled={submission.isPending}
            sx={{ minWidth: 200 }}
            onChange={(event) => {
              setMode(event.target.value as "accepted" | "mixed");
              resetDraft();
            }}
          >
            <MenuItem value="mixed">Mixed outcomes</MenuItem>
            <MenuItem value="accepted">All accepted</MenuItem>
          </TextField>
        </Stack>
        <Stack direction={{ xs: "column", md: "row" }} spacing={2} mb={3}>
          <TextField
            label="Requested execution date"
            type="date"
            value={executionDate}
            disabled={submission.isPending}
            onChange={(event) => {
              setExecutionDate(event.target.value);
              resetDraft();
            }}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            label="Random seed"
            type="number"
            value={seed}
            disabled={submission.isPending}
            onChange={(event) => {
              setSeed(Number(event.target.value));
              resetDraft();
            }}
            helperText="Repeatable fixture values"
          />
          <Button
            disabled={submission.isPending}
            onClick={() => {
              setSeed(randomSeed());
              resetDraft();
            }}
          >
            New random seed
          </Button>
        </Stack>
        <Stack direction="row" spacing={2}>
          <Button
            variant="outlined"
            disabled={!valid || submission.isPending}
            onClick={() => {
              setDraft(
                generateBatch({
                  fundingAccounts: funding.data!,
                  count,
                  seed,
                  payerReference,
                  executionDate,
                  mode,
                }),
              );
              submission.reset();
            }}
          >
            Generate preview
          </Button>
          <Button
            variant="contained"
            disabled={!draft || submission.isPending || submission.isSuccess}
            onClick={() => submission.mutate(draft!)}
          >
            {submission.isPending
              ? "Submitting..."
              : submission.isError
                ? "Retry same submission"
                : "Submit batch"}
          </Button>
        </Stack>
      </Card>
      <ErrorNotice error={funding.error} />
      <ErrorNotice error={submission.error} />
      {submission.isError && (
        <Alert severity="warning" sx={{ my: 2 }}>
          Retry same submission keeps the original request key and instructions.
          Check Batches before creating a replacement if the response was lost.
        </Alert>
      )}
      {submission.data && (
        <Card sx={{ p: 3, mb: 3 }}>
          <Alert severity="success" sx={{ mb: 2 }}>
            Batch accepted and queued. Bank acceptance is not settlement.
          </Alert>
          <Typography variant="body2" sx={{ overflowWrap: "anywhere" }}>
            Batch ID: {submission.data.id}
          </Typography>
          <Stack direction="row" spacing={2} alignItems="center" mt={2}>
            {progress.data && <Status value={progress.data.status} />}
            <Button component={Link} to="/batches">
              Open Batches
            </Button>
            <Button component={Link} to="/reconciliation">
              Open Reconciliation
            </Button>
          </Stack>
          <ErrorNotice error={progress.error} />
        </Card>
      )}
      {draft && (
        <Card>
          <Box p={2}>
            <Typography variant="h6">Payment preview</Typography>
            <Typography variant="body2" color="text.secondary">
              {draft.input.payments.length} instructions | GBP/EUR/USD | GB/DE/US | Bank A/B |
              Seed {draft.seed}. Showing the first 10.
            </Typography>
          </Box>
          <Box sx={{ overflowX: "auto" }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  {[
                    "Payer company",
                    "Payee",
                    "Purpose",
                    "Amount",
                    "Funding account",
                    "Bank",
                    "Country",
                    "Execution date",
                  ].map((label) => (
                    <TableCell key={label}>{label}</TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {draft.input.payments.slice(0, 10).map((payment) => (
                  <TableRow key={payment.clientReference}>
                    <TableCell>{payment.parties.payer.legalName}</TableCell>
                    <TableCell>{payment.beneficiary}</TableCell>
                    <TableCell>
                      {purposeNames[payment.purpose]}
                      <Typography variant="caption" display="block">
                        {payment.parties.businessReference}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      {payment.currency} {payment.amount.toFixed(2)}
                    </TableCell>
                    <TableCell>{payment.treasury.fundingAccountId}</TableCell>
                    <TableCell>
                      {payment.provider === "bank-a" ? "Bank A" : "Bank B"}
                    </TableCell>
                    <TableCell>{payment.destinationCountry}</TableCell>
                    <TableCell>{payment.executionDate}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Box>
        </Card>
      )}
    </>
  );
}
