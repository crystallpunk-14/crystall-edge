namespace Content.Server._CE.Statistics;

/// <summary>
/// Declares a player statistic stored in the database under a fixed key.
/// Declare instances in <see cref="CEStatistics"/>.
/// </summary>
public sealed class CEStatisticDef(string key)
{
    public readonly string Key = key;

    public override string ToString() => Key;
}

/// <summary>
/// Declares a family of player statistics whose key is built from an argument,
/// e.g. <c>rounds_finished_alive.job.CEGuard</c>.
/// </summary>
public sealed class CEStatisticDef<T>(string prefix) where T : notnull
{
    public readonly string Prefix = prefix;

    public string GetKey(T arg) => $"{Prefix}.{arg}";

    public override string ToString() => Prefix;
}
