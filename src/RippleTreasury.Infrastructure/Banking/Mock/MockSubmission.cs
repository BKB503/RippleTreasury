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

public record MockSubmission(Guid ExecutionId, string Scenario, int AttemptNumber, long MinorUnits, string Currency,
    string Beneficiary, string DestinationCountry, string Purpose, DateOnly ExecutionDate, PaymentParties? Parties = null, TreasurySubmissionDetails? Treasury = null)
{

    /// <summary>Builds the provider payload using the persisted route, account and original execution identity.</summary>
    public static MockSubmission From(WorkItem item) => new(item.Execution.ExecutionId, item.Instruction.Scenario,
        item.AttemptNumber, item.Instruction.Amount.MinorUnits, item.Instruction.Amount.Currency, item.Instruction.Beneficiary,
        item.Instruction.DestinationCountry, item.Instruction.Purpose.ToString(), item.Instruction.ExecutionDate, item.Instruction.Parties, TreasurySubmissionDetails.From(item.Instruction.Treasury));

    /// <summary>Retains the legacy hash shape and includes enhanced treasury intent only when supplied.</summary>
    public string Fingerprint()
    {
        var canonical = JsonSerializer.Serialize(new
        {
            MinorUnits,
            Currency,
            Beneficiary,
            DestinationCountry,
            Purpose,
            ExecutionDate
        });
        if (Parties is not null)
            canonical = JsonSerializer.Serialize(new
            {
                LegacyCanonical = canonical,
                Parties
            });
        if (Treasury is not null)
            canonical = JsonSerializer.Serialize(new { LegacyCanonical = canonical, Treasury });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
