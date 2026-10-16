using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Defines application activity sources and business metrics independently of any telemetry exporter.</summary>
public static class Telemetry
{
    public const string Name = "RippleTreasury";

    public static readonly ActivitySource Activities = new(Name);
    public static readonly Meter Meter = new(Name);

    public static readonly Counter<long> AcceptedBatches = Meter.CreateCounter<long>("batches_accepted_total");
    public static readonly Counter<long> Replayed = Meter.CreateCounter<long>("idempotency_replayed_total");

    public static readonly Counter<long> Outcomes = Meter.CreateCounter<long>("payment_outcomes_total");
    public static readonly Histogram<double> BankLatency = Meter.CreateHistogram<double>("bank_latency_ms");
}
