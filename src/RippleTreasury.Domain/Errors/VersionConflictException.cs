namespace RippleTreasury.Domain;

public class VersionConflictException() : Exception("The resource changed; reload before retrying.");
