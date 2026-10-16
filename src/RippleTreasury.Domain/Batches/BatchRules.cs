namespace RippleTreasury.Domain;

public static class BatchRules
{
    public const int MaximumPayments = 500;

    /// <summary>Validates a client batch reference before intake.</summary>
    public static void ValidateReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 100)
            throw new DomainException("Reference required, maximum 100 characters.");
    }

    /// <summary>Rejects duplicate client payment references inside an atomically accepted batch.</summary>
    public static void EnsureUniquePaymentReference(HashSet<string> references, string reference)
    {
        ValidateReference(reference);
        if (!references.Add(reference))
            throw new DomainException("Payment references must be unique within the batch.");
    }
}
