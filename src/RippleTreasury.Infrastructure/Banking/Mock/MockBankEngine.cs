using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RippleTreasury.Application;
using RippleTreasury.Domain;

namespace RippleTreasury.Infrastructure;

public class MockBankEngine(IDbContextFactory<TreasuryDb> factory, IBankCapabilityCatalog? capabilities = null)
{

    /// <summary>Models bank-side idempotent execution independently of Treasury state, including capability validation and lost responses.</summary>
    public async Task<BankObservation> Submit(string provider, MockSubmission request, CancellationToken ct)
    {
        if (provider is not ("bank-a" or "bank-b"))
            throw new InvalidOperationException("Unknown mock provider");
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        _ = new Money(request.MinorUnits, request.Currency);
        if (!Enum.TryParse<PaymentPurpose>(request.Purpose, out var purpose) || !Enum.IsDefined(purpose))
            throw new DomainException("Invalid payment purpose.");
        InstructionRules.EnsurePartyConsistency(request.Parties, purpose, request.Beneficiary, request.DestinationCountry);
        var fingerprint = request.Fingerprint();
        var prior = await db.SimulatedBankExecutions.SingleOrDefaultAsync(x => x.Id == request.ExecutionId, ct);
        if (prior is not null)
        {
            if (prior.Provider != provider || prior.InstructionHash != fingerprint)
                throw new ConflictException("Bank execution key already used for a different immutable instruction.");
            return new(Enum.Parse<BankOutcome>(prior.Outcome), prior.Evidence);
        }
        if (request.Treasury is { } treasury)
        {
            // Defense in depth: the simulated bank independently rejects incompatible source/corridor payloads.
            var source = await db.FundingAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == treasury.FundingAccountId, ct)
                ?? throw new TreasuryValidationException("funding_account_unknown", "Unknown mock funding account.");
            source.EnsureUsable(source.TenantId, treasury.FundingOwnerReference, request.Currency);
            var beneficiary = new BeneficiaryAccount(treasury.BeneficiaryReference, treasury.AccountIdentifierType,
                treasury.AccountIdentifier, treasury.Bic);
            var accepted = new TreasuryPaymentDetails(source, beneficiary,
                new(provider, treasury.RoutingPolicyVersion, treasury.RoutingReason));
            accepted.EnsureConsistent(new(request.MinorUnits, request.Currency), request.Parties);
            if (source.Country != treasury.SourceCountry || source.CurrencyCode != treasury.FundingCurrency ||
                treasury.FundingBankProviderCode != provider ||
                !(capabilities ?? new DemoBankCapabilityCatalog()).GetCapabilities().Any(x => x.Provider == provider &&
                    x.Supports(source, new(treasury.SourceCountry, request.DestinationCountry, request.Currency))))
                throw new TreasuryValidationException("routing_unsupported", "Mock bank cannot execute this funding account and corridor.");
        }
        if (request.Scenario == "rate-limit" && provider != "bank-b")
            throw new TreasuryValidationException("mock_scenario_unsupported", "The rate-limit fixture belongs to bank-b only.");
        if (request.Scenario == "rate-limit" && request.AttemptNumber == 1)
            return new(BankOutcome.ProvenNotExecuted, "Mock bank-b contract: pre-dispatch throttling; guaranteed no execution");
        if (request.Scenario == "safe-transient" && request.AttemptNumber == 1)
            return new(BankOutcome.ProvenNotExecuted, "Mock contract: rejected before dispatch; guaranteed no execution");
        if (request.Scenario == "unknown")
            return new(BankOutcome.Unknown, "Mock lookup cannot establish execution");
        var outcome = request.Scenario == "reject" ? BankOutcome.Rejected : BankOutcome.Accepted;
        var evidence = outcome == BankOutcome.Accepted ? $"{provider}-{request.ExecutionId:N}" : "Mock definitive rejection: invalid beneficiary";
        db.SimulatedBankExecutions.Add(new()
        {
            Id = request.ExecutionId,
            Provider = provider,
            InstructionHash = fingerprint,
            MinorUnits = request.MinorUnits,
            Currency = request.Currency,
            Outcome = outcome.ToString(),
            Evidence = evidence,
            FinancialExecutions = outcome == BankOutcome.Accepted ? 1 : 0
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        if (request.Scenario == "lost-response")
            return new(BankOutcome.Unknown, "Mock accepted but response lost");
        return new(outcome, evidence);
    }

    /// <summary>Returns existing bank evidence; an absent execution record is not authoritative proof of non-execution.</summary>
    public async Task<BankObservation> Lookup(string provider, Guid id, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var row = await db.SimulatedBankExecutions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.Provider == provider, ct);
        return row is null ? new(BankOutcome.Unknown, "Not found is not proof of no execution") : new(Enum.Parse<BankOutcome>(row.Outcome), row.Evidence);
    }
}
