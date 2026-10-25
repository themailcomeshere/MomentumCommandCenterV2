using System.Reflection;

namespace MomentumCommandCenterV2.Infrastructure.MarketData;

// =========================================================
/// CLASS: SchwabExclusionList
/// PURPOSE:
///   Provides one shared, read-only view of the user-maintained
///   Schwab exclusion symbols embedded with the Infrastructure assembly.
/// RESPONSIBILITIES:
///   - Load and validate the canonical symbol resource.
///   - Check symbols consistently without changing their displayed spelling.
///   - Expose the source symbols for audit/reporting and tests.
/// LAYER: Infrastructure
// =========================================================
/// <summary>
/// Provides the current user-supplied Schwab exclusion list.
/// The list is a manual workflow exclusion, not a live broker tradability check.
/// </summary>
public sealed class SchwabExclusionList
{
    private const string ResourceName =
        "MomentumCommandCenterV2.Infrastructure.Resources.schwab-exclusions.txt";

    private readonly HashSet<string> _symbols;

    /// <summary>
    /// Loads the embedded exclusion list. Throws if the resource is absent,
    /// empty, or contains duplicate/blank symbols, so failures cannot silently
    /// disable exclusion checks.
    /// </summary>
    public SchwabExclusionList()
    {
        using var stream = typeof(SchwabExclusionList).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Required exclusion resource '{ResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        var symbols = reader.ReadToEnd()
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(symbol => symbol.Trim())
            .ToArray();

        if (symbols.Length == 0 || symbols.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("The Schwab exclusion list must contain at least one nonblank symbol.");
        }

        if (symbols.Distinct(StringComparer.OrdinalIgnoreCase).Count() != symbols.Length)
        {
            throw new InvalidOperationException("The Schwab exclusion list contains duplicate symbols.");
        }

        _symbols = new HashSet<string>(symbols, StringComparer.OrdinalIgnoreCase);
        Symbols = Array.AsReadOnly(symbols);
    }

    /// <summary>Original source symbols, preserving user-provided spelling and order.</summary>
    public IReadOnlyList<string> Symbols { get; }

    /// <summary>
    /// Returns true when the trimmed input matches an excluded symbol
    /// case-insensitively. Null, empty, and whitespace inputs return false.
    /// </summary>
    public bool IsExcluded(string? symbol) =>
        !string.IsNullOrWhiteSpace(symbol) && _symbols.Contains(symbol.Trim());
}
