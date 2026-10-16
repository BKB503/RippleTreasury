using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Validates the common paging limits before reading repository data.</summary>
internal static class PaginationValidator
{
    /// <summary>Rejects non-positive page numbers and page sizes outside the supported 1-to-100 range.</summary>
    public static void Validate(int page, int size)
    {
        if (page < 1 || size is < 1 or > 100)
            throw new DomainException("Page must be positive and pageSize between 1 and 100.");
    }
}
