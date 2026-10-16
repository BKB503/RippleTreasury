using Microsoft.Extensions.Logging;
using Moq;

namespace RippleTreasury.Application.Tests.TestSupport;

/// <summary>Inspects actual ILogger calls captured by Moq without implementing a logger double.</summary>
public static class LogInspection
{
    public static string[] LogMessages<T>(Mock<ILogger<T>> logger) =>
        logger.Invocations.Where(x => x.Method.Name == nameof(ILogger.Log))
            .Select(x => x.Arguments[2].ToString()!).ToArray();

    public static string[] LogScopes<T>(Mock<ILogger<T>> logger) =>
        logger.Invocations.Where(x => x.Method.Name == nameof(ILogger.BeginScope))
            .Select(x => x.Arguments[0].ToString()!).ToArray();
}
