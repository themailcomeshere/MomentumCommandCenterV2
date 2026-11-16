using System.Text.Json;
using MomentumCommandCenterV2.API.MarketData;
using MomentumCommandCenterV2.Core.Models;
using MomentumCommandCenterV2.Core.Models.Enums;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: LiveDecisionProjectionTests
/// PURPOSE: Verifies that every V2 engine state maps to the Thinkorswim study label wording and colour tone
/// shown on the Dashboard Red Zone cards, and that EXECUTION READY is never shown without entry permission.
/// WHEN CALLED: Run by xUnit during the test suite.
/// </summary>
public sealed class LiveDecisionProjectionTests
{
    /// <summary>
    /// PURPOSE: Confirms each engine state's label and tone.
    /// WHEN CALLED: Run by xUnit once per inline data row.
    /// </summary>
    [Theory]
    [InlineData(SignalState.NoTrade, false, "WAIT", "wait")]
    [InlineData(SignalState.PrepareBuy, false, "PREPARE TO BUY", "prepare")]
    [InlineData(SignalState.Buy, true, "EXECUTION READY", "ready")]
    [InlineData(SignalState.Runner, false, "RUNNER", "runner")]
    [InlineData(SignalState.RunnerWaning, false, "RUNNER WANING", "waning")]
    [InlineData(SignalState.PrepareSell, false, "PREPARE TO SELL", "waning")]
    [InlineData(SignalState.ConfirmedWeakness, false, "SELL - CONFIRMED WEAKNESS", "sell")]
    [InlineData(SignalState.Breakdown, false, "SELL - BREAKDOWN", "sell")]
    [InlineData(SignalState.Exit, false, "GET OUT", "sell")]
    public void ToStateLabel_MapsEveryStateToStudyWording(SignalState state, bool entryAllowed, string label, string tone)
    {
        var result = LiveDecisionProjection.ToStateLabel(state, entryAllowed);

        Assert.Equal(label, result.Label);
        Assert.Equal(tone, result.Tone);
    }

    /// <summary>
    /// PURPOSE: Confirms every enum value is covered, so a new state cannot silently show as UNKNOWN.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public void ToStateLabel_CoversEveryEnumValue()
    {
        foreach (var state in Enum.GetValues<SignalState>())
            Assert.NotEqual("UNKNOWN", LiveDecisionProjection.ToStateLabel(state, true).Label);
    }

    /// <summary>
    /// PURPOSE: Confirms a Buy state without entry permission is not shown as EXECUTION READY.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public void ToStateLabel_BuyWithoutEntryPermission_IsNotExecutionReady()
    {
        var result = LiveDecisionProjection.ToStateLabel(SignalState.Buy, entryAllowed: false);

        Assert.Equal("PREPARE TO BUY", result.Label);
        Assert.Equal("prepare", result.Tone);
    }

    /// <summary>
    /// PURPOSE: Confirms an unevaluated decision stays null (shown as DATA?) and an evaluated one carries its label.
    /// WHEN CALLED: Run by xUnit.
    /// </summary>
    [Fact]
    public void Project_NullStaysNull_AndEvaluatedDecisionCarriesLabelAndTone()
    {
        Assert.Null(LiveDecisionProjection.Project(null));

        var decision = new SignalDecision(SignalState.RunnerWaning, MomentumAction.HOLD, 4, "Test reason.",
            LongPermission: true, EntryAllowed: false, RunnerAllowed: true, ExitWarning: true, HardExit: false);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(LiveDecisionProjection.Project(decision)));

        Assert.Equal("RunnerWaning", json.RootElement.GetProperty("state").GetString());
        Assert.Equal("RUNNER WANING", json.RootElement.GetProperty("stateLabel").GetString());
        Assert.Equal("waning", json.RootElement.GetProperty("stateTone").GetString());
        Assert.False(json.RootElement.GetProperty("entryAllowed").GetBoolean());
    }
}
