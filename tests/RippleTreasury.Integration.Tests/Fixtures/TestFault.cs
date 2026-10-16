using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Integration.Tests;

public sealed class TestFault : ITransactionFaultInjector
{
    public string? Operation { get; set; }

    public void BeforeCommit(string operation)
    {
        if (Operation == operation)
        {
            Operation = null;
            throw new IOException("Injected pre-commit failure");
        }
    }
}
