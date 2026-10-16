import { Box, Chip, Divider, Stack, Typography } from "@mui/material";
import { money } from "../shared/money";
import type { Detail } from "../features/payments/types";
import { Status } from "./Status";

function utc(value: string) {
  return new Date(value).toLocaleString("en-GB", { timeZone: "UTC" }) + " UTC";
}
export function PaymentEvidence({ detail }: { detail: Detail }) {
  const payment = detail.payment;
  const history = detail.history;
  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={2}>
        <Status value={payment.status} />
        <Chip size="small" label={payment.provider} />
      </Stack>
      <Typography>
        {money(payment)} | {payment.beneficiary} | {payment.destinationCountry}
      </Typography>
      {payment.parties ? (
        <Box>
          <Typography variant="subtitle2">{payment.purpose}</Typography>
          <Typography>
            Payer: {payment.parties.payer.legalName} |{" "}
            {payment.parties.payer.reference} | {payment.parties.payer.country}
          </Typography>
          <Typography>
            Payee: {payment.parties.payee.legalName} |{" "}
            {payment.parties.payee.reference} | {payment.parties.payee.kind}
          </Typography>
          <Typography variant="body2">
            Business reference: {payment.parties.businessReference}
          </Typography>
          <Typography variant="body2">
            Requested execution: {payment.executionDate}
          </Typography>
          {payment.parties.payer.groupReference && (
            <Typography variant="body2">
              Corporate group: {payment.parties.payer.groupReference}
            </Typography>
          )}
        </Box>
      ) : (
        <Typography variant="caption">
          Original demo instruction: company snapshots were not supplied.
        </Typography>
      )}
      {payment.treasury ? (
        <Box>
          <Typography variant="body2">Funding account: {payment.treasury.fundingAccountId}</Typography>
          <Typography variant="body2">
            Corridor: {payment.treasury.sourceCountry} to {payment.destinationCountry} / {payment.currency}
            {payment.treasury.isCrossBorder ? " (cross-border)" : " (domestic)"}
          </Typography>
          <Typography variant="body2">
            Beneficiary reference: {payment.treasury.beneficiaryReference} | {payment.treasury.accountIdentifierType}
          </Typography>
          <Typography variant="body2">Routing policy: {payment.treasury.routingPolicyVersion}</Typography>
          <Typography variant="caption">{payment.treasury.routingReason}</Typography>
        </Box>
      ) : (
        <Typography variant="caption">Legacy v1 instruction: funding/capability checks were not supplied.</Typography>
      )}
      <Typography variant="caption" sx={{ overflowWrap: "anywhere" }}>
        Correlation: {payment.correlationId}
        <br />
        Execution: {payment.executionId}
        <br />
        Version: {payment.version}
        <br />
        Bank evidence: {payment.bankReference ?? "Not confirmed"}
      </Typography>
      {history && (
        <Box>
          <Typography variant="h6">Who and when</Typography>
          <Typography variant="body2">
            Created: {utc(history.createdAt)} |{" "}
            {history.createdBy.displayName || history.createdBy.id}
          </Typography>
          <Typography variant="body2">
            Processing last updated: {utc(history.lastUpdatedAt)} |{" "}
            {history.lastUpdatedBy.displayName || history.lastUpdatedBy.id}
          </Typography>
          <Typography variant="body2">
            Reconciliation started:{" "}
            {history.lastReconciliationStartedAt
              ? utc(history.lastReconciliationStartedAt)
              : "Not run"}
          </Typography>
          <Typography variant="body2">
            Reconciliation completed:{" "}
            {history.lastReconciliationCompletedAt
              ? utc(history.lastReconciliationCompletedAt)
              : "Not completed"}{" "}
            |{" "}
            {history.lastReconciliationBy?.displayName ||
              history.lastReconciliationBy?.id ||
              "None"}
          </Typography>
        </Box>
      )}
      <Divider />
      <Typography variant="h6">Attempt timeline</Typography>
      {detail.attempts.map((attempt) => (
        <Box key={attempt.id}>
          <Typography>
            {attempt.kind} | {attempt.outcome}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            Bank: {attempt.provider ?? payment.provider} | Funding: {attempt.fundingAccountId ?? "Legacy"}
            <br />
            Started: {utc(attempt.startedAt)} | Completed:{" "}
            {attempt.completedAt ? utc(attempt.completedAt) : "Pending"}
          </Typography>
        </Box>
      ))}
      <Divider />
      <Typography variant="h6">Business audit</Typography>
      {detail.audit.map((event) => (
        <Box key={event.id}>
          <Typography variant="body2">
            {event.action || "Historical event"} | {event.before} &rarr;{" "}
            {event.after}
          </Typography>
          <Typography variant="caption" color="text.secondary" display="block">
            {utc(event.at)} | {event.actorDisplayName || event.actor} (
            {(event.actorKind === "DemoUser" ? "Operator" : event.actorKind) ||
              "Historical actor"}
            )
          </Typography>
          <Typography variant="caption" color="text.secondary" display="block">
            Actor ID: {event.actor} | Source:{" "}
            {event.source || "Historical source"} | Version: {event.version}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {event.reason}
          </Typography>
        </Box>
      ))}
    </Stack>
  );
}
