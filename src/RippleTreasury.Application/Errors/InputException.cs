namespace RippleTreasury.Application;

/// <summary>Carries field-specific request validation errors for centralized HTTP problem mapping.</summary>
public class InputException(Dictionary<string, string[]> errors, Dictionary<string, string>? validationCodes = null) : Exception("Request validation failed.")
{
    public Dictionary<string, string> ValidationCodes { get; } = validationCodes ?? [];
    public Dictionary<string, string[]> Errors { get; } = errors;
}
