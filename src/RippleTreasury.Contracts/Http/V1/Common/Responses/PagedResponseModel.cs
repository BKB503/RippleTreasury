namespace RippleTreasury.Contracts;

/// <summary>A bounded page of response items with the requested paging information and total count.</summary>
public record PagedResponseModel<T>(
    T[] Items,
    int PageNumber,
    int PageSize,
    int Total);
