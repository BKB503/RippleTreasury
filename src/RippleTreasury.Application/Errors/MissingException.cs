namespace RippleTreasury.Application;

/// <summary>Signals that a requested application resource could not be found.</summary>
public class MissingException() : Exception("Resource not found.");
