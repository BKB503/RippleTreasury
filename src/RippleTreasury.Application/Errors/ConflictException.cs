namespace RippleTreasury.Application;

/// <summary>Signals conflicting client intent or command reuse without changing existing financial state.</summary>
public class ConflictException(string message) : Exception(message);
