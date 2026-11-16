using MomentumCommandCenterV2.API.BoringOvernight;
using MomentumCommandCenterV2.API.MarketData;
using Xunit;

namespace MomentumCommandCenterV2.Tests;

/// <summary>
/// TYPE: BoringOvernightReadyTests
/// PURPOSE: Verifies the READY evaluator, catalyst rules, operator sign-off store and shadow-log outcomes against the
/// 2026-11-14 decision record. All inputs are synthetic test data.
/// WHEN CALLED: Run by xUnit during the test suite.
/// </summary>
public sealed class BoringOvernightReadyTests
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private static readonly DateOnly BuyDay = new(2026, 11, 16);   // Monday
    private static readonly DateOnly SellDay = new(2026, 11, 17);
    private static readonly DateTimeOffset InWindow = Et(BuyDay, 15, 30);

    // ---------- evaluator ----------

    /// <summary>All gates pass inside the window in shadow mode: READY (SHADOW) with limit = latest trade x (1 + target%).</summary>
    [Fact]
    public void AllGatesPassInWindowGivesShadowReadyWithLimitFromLatestTrade()
    {
        var result = BoringOvernightReadyEvaluator.Evaluate(Inputs());

        Assert.Equal("READY", result.Status);
        Assert.Equal("READY (SHADOW — not yet validated)", result.SignalLabel);
        Assert.Equal("shadow", result.EffectiveMode);
        Assert.Equal(50.39m, result.CalculatedLimitSell); // 50.00 x 1.0078 = 50.39
        Assert.Equal(50.00m, result.ReferencePrice);
        Assert.Equal("OPERATOR_ATTESTED", result.CatalystState);
        Assert.All(result.Gates, g => Assert.Equal(BoringOvernightReadyEvaluator.GatePass, g.State));
    }

    /// <summary>Live mode needs at least 10 logged shadow sessions; fewer keeps shadow.</summary>
    [Theory]
    [InlineData(9, "shadow", "READY (SHADOW — not yet validated)")]
    [InlineData(10, "live", "READY — BUY WINDOW OPEN")]
    public void LiveModeRequiresTenLoggedShadowSessions(int sessions, string expectedMode, string expectedLabel)
    {
        var result = BoringOvernightReadyEvaluator.Evaluate(Inputs() with { RequestedMode = "live", LoggedShadowSessions = sessions });
        Assert.Equal(expectedMode, result.EffectiveMode);
        Assert.Equal(expectedLabel, result.SignalLabel);
    }

    /// <summary>Outside the buy window, passing gates report OUTSIDE_BUY_WINDOW, never READY.</summary>
    [Fact]
    public void OutsideWindowIsNeverReady()
    {
        var now = Et(BuyDay, 12, 0);
        var result = BoringOvernightReadyEvaluator.Evaluate(Inputs(now) with { Attestation = Signoff() with { AttestedAtUtc = now.AddHours(-1) } });
        Assert.Equal("OUTSIDE_BUY_WINDOW", result.Status);
        Assert.Contains(result.Gates, g => g.Code == "BUY_WINDOW" && g.State == BoringOvernightReadyEvaluator.GateNotYet);
    }

    /// <summary>A snapshot older than 10 minutes, or none, is DATA?.</summary>
    [Fact]
    public void StaleOrMissingSnapshotIsData()
    {
        var stale = BoringOvernightReadyEvaluator.Evaluate(Inputs() with { Snapshot = Snapshot(InWindow.AddMinutes(-11)) });
        var missing = BoringOvernightReadyEvaluator.Evaluate(Inputs() with { Snapshot = null });
        Assert.Equal("DATA?", stale.Status);
        Assert.Equal("DATA?", missing.Status);
        Assert.NotEmpty(missing.RequiredActions);
    }

    /// <summary>Qualification WATCH (extension at 2.5 ATR) is NOT_READY and the warning is shown.</summary>
    [Fact]
    public void QualificationWarningIsNotReady()
    {
        var snapshot = Snapshot(InWindow.AddMinutes(-2));
        snapshot = snapshot with { EvaluationRequest = snapshot.EvaluationRequest with { ExtensionAtr = 2.5 } };
        var result = BoringOvernightReadyEvaluator.Evaluate(Inputs() with { Snapshot = snapshot });
        Assert.Equal("NOT_READY", result.Status);
        Assert.Contains(result.Warnings, w => w.Contains("2.0 ATR"));
    }

    /// <summary>An excluded symbol is BLOCKED; an unreadable exclusion list is DATA?.</summary>
    [Fact]
    public void ExclusionBlocksAndUnreadableListIsData()
    {
        Assert.Equal("BLOCKED", BoringOvernightReadyEvaluator.Evaluate(Inputs() with { Excluded = true }).Status);
        Assert.Equal("DATA?", BoringOvernightReadyEvaluator.Evaluate(Inputs() with { Excluded = null }).Status);
    }

    /// <summary>
    /// Operator decision 2026-11-16: without a tested target (walk-forward failed, history incomplete, evidence error) the
    /// untested +0.5% heuristic is applied to the latest trade, READY is allowed and labelled LIMIT UNTESTED.
    /// </summary>
    [Fact]
    public void UntestedSellEvidence_UsesHeuristicAndLabelsIt()
    {
        var walkForwardFail = BoringOvernightReadyEvaluator.Evaluate(Inputs() with { SellLimit = Sell("BLOCKED", null, ("WALK_FORWARD_VALIDATION", false)) });
        var historyMissing = BoringOvernightReadyEvaluator.Evaluate(Inputs() with { SellLimit = Sell("BLOCKED", null, ("HISTORY_40_OF_40", false)) });
        var evidenceError = BoringOvernightReadyEvaluator.Evaluate(Inputs() with { SellLimit = null, SellLimitError = "HttpRequestException: test" });

        foreach (var r in new[] { walkForwardFail, historyMissing, evidenceError })
        {
            Assert.Equal("READY", r.Status);
            Assert.Equal(50.25m, r.CalculatedLimitSell);            // 50.00 x 1.005
            Assert.Equal(0.5m, r.TargetReturnPct);
            Assert.Equal("UNTESTED_HEURISTIC", r.LimitBasis);
            Assert.Contains("LIMIT UNTESTED", r.SignalLabel);
            Assert.Contains(r.Warnings, w => w.Contains("UNTESTED", StringComparison.Ordinal));
        }
        Assert.Contains("WALK_FORWARD_VALIDATION", walkForwardFail.Gates.Single(g => g.Code == "SELL_LIMIT").Detail);
    }

    /// <summary>A tested target keeps its own basis and label (no UNTESTED).</summary>
    [Fact]
    public void TestedSellEvidence_UsesTestedTarget()
    {
        var r = BoringOvernightReadyEvaluator.Evaluate(Inputs());

        Assert.Equal("READY", r.Status);
        Assert.Equal(50.39m, r.CalculatedLimitSell);                // 50.00 x 1.0078
        Assert.Equal("TESTED_P50", r.LimitBasis);
        Assert.DoesNotContain("UNTESTED", r.SignalLabel);
    }

    /// <summary>Operator decision 2026-11-16: 5M permission NO / structure NOT_BULLISH are notes in READY, not blockers.</summary>
    [Fact]
    public void FiveMinuteMomentum_IsInformationalForReady()
    {
        var at = InWindow;
        var snapshot = Snapshot(at.AddMinutes(-2)) with
        {
            EvaluationRequest = Snapshot(at).EvaluationRequest with { FiveMinutePermission = false, FiveMinuteStructure = "NOT_BULLISH" }
        };
        var r = BoringOvernightReadyEvaluator.Evaluate(Inputs() with { Snapshot = snapshot });

        Assert.Equal("READY", r.Status);
        Assert.Contains(r.QualificationNotes!, n => n.Contains("5M permission NO", StringComparison.Ordinal));
        // The Full mode (other screens) keeps the original momentum rule.
        Assert.Equal("BLOCKED", BoringOvernightEvaluator.Evaluate(snapshot.EvaluationRequest with { CatalystStatus = "CLEAR" }).Status);
    }

    /// <summary>The V2 end-of-day "Exit/SELL" (from 15:55 ET) is a clock rule, not a stock signal; a real SELL still blocks.</summary>
    [Fact]
    public void EndOfDayExit_IsNoteButRealSellBlocks()
    {
        var at = Et(BuyDay, 15, 57);
        Snapshot1M(at, "Exit/SELL", out var eod);
        Snapshot1M(at, "ConfirmedWeakness/SELL", out var weak);

        Assert.Equal("READY", BoringOvernightReadyEvaluator.Evaluate(eod).Status);
        Assert.Equal("BLOCKED", BoringOvernightReadyEvaluator.Evaluate(weak).Status);
    }

    /// <summary>Late window (close to close + 60 min): Daily freshness only; trade at/below the regular close is READY.</summary>
    [Fact]
    public void LateWindow_TradeAtOrBelowClose_IsReady()
    {
        var r = BoringOvernightReadyEvaluator.Evaluate(LateInputs(49.90m, 50.00m));

        Assert.Equal("READY", r.Status);
        Assert.Equal("LATE", r.EntryWindow);
        Assert.Contains("LATE WINDOW (LMT EXT)", r.SignalLabel);
        Assert.Equal(GatePass, r.Gates.Single(g => g.Code == "LATE_ENTRY_PRICE").State);
        Assert.Contains(r.QualificationNotes!, n => n.Contains("after the regular close", StringComparison.Ordinal));
        Assert.Equal(Et(BuyDay, 17, 0), r.LateBuyDeadlineUtc);
    }

    /// <summary>Late window: an after-hours trade above the regular close is WATCH (NOT_READY), never READY.</summary>
    [Fact]
    public void LateWindow_TradeAboveClose_IsWatch()
    {
        var r = BoringOvernightReadyEvaluator.Evaluate(LateInputs(50.20m, 50.00m));

        Assert.Equal("NOT_READY", r.Status);
        Assert.Equal("WATCH — AFTER-HOURS UP 0.40%", r.SignalLabel);
    }

    /// <summary>Late window without a regular-close reference is DATA?.</summary>
    [Fact]
    public void LateWindow_NoCloseReference_IsData()
    {
        var r = BoringOvernightReadyEvaluator.Evaluate(LateInputs(49.90m, null));

        Assert.Equal("DATA?", r.Status);
        Assert.Equal("DATA?", r.Gates.Single(g => g.Code == "LATE_ENTRY_PRICE").State);
    }

    /// <summary>After close + 60 minutes the window is closed and the buy session moves to the next day.</summary>
    [Fact]
    public void AfterLateDeadline_WindowIsClosed()
    {
        var at = Et(BuyDay, 17, 5);
        var clock = MarketSessionClock.GetSnapshot(at);

        Assert.False(clock.OvernightBuyWindowOpen);
        Assert.False(clock.OvernightLateBuyWindowOpen);
        Assert.Equal(BuyDay.AddDays(1), clock.OvernightBuySessionDate);
    }

    /// <summary>A latest trade older than 2 minutes (or none) cannot be used for the limit: DATA?.</summary>
    [Fact]
    public void StaleLatestTradeIsData()
    {
        var stale = BoringOvernightReadyEvaluator.Evaluate(Inputs() with { LiveTradeTimestampUtc = InWindow.AddMinutes(-3) });
        var none = BoringOvernightReadyEvaluator.Evaluate(Inputs() with { LiveTradePrice = null });
        Assert.Equal("DATA?", stale.Status);
        Assert.Null(stale.CalculatedLimitSell);
        Assert.Equal("DATA?", none.Status);
    }

    // ---------- catalyst gate ----------

    /// <summary>Zero news needs a sign-off; with today's sign-off it passes as OPERATOR_ATTESTED, never CLEAR.</summary>
    [Fact]
    public void ZeroNewsNeedsSignoff()
    {
        var without = ReadyCatalystGate.Evaluate(Evidence(), Array.Empty<CatalystHumanReviewRecord>(), null, "TEST", BuyDay, InWindow);
        var with = ReadyCatalystGate.Evaluate(Evidence(), Array.Empty<CatalystHumanReviewRecord>(), Signoff(), "TEST", BuyDay, InWindow);
        var otherDay = ReadyCatalystGate.Evaluate(Evidence(), Array.Empty<CatalystHumanReviewRecord>(), Signoff() with { BuySessionDate = BuyDay.AddDays(-3) }, "TEST", BuyDay, InWindow);
        Assert.Equal("NEEDS_SIGNOFF", without.State);
        Assert.Contains("UNKNOWN, not CLEAR", without.Detail);
        Assert.Equal("OPERATOR_ATTESTED", with.State);
        Assert.Contains("OPERATOR-ATTESTED", with.Detail);
        Assert.Equal("NEEDS_SIGNOFF", otherDay.State);
    }

    /// <summary>A human MATERIAL_RISK label blocks even with a sign-off.</summary>
    [Fact]
    public void MaterialRiskBlocksDespiteSignoff()
    {
        var result = ReadyCatalystGate.Evaluate(Evidence(Article("a1", "MATERIAL")), new[] { Review("a1", "MATERIAL_RISK") }, Signoff(), "TEST", BuyDay, InWindow);
        Assert.Equal("BLOCKED", result.State);
    }

    /// <summary>MATERIAL_EVENT: earnings/FDA or unknown type block; analyst action / M&amp;A pass with a warning.</summary>
    [Theory]
    [InlineData("EARNINGS_OR_GUIDANCE", "BLOCKED")]
    [InlineData("FDA_OR_CLINICAL", "BLOCKED")]
    [InlineData("", "BLOCKED")]
    [InlineData("ANALYST_ACTION", "OPERATOR_ATTESTED")]
    [InlineData("M_AND_A", "OPERATOR_ATTESTED")]
    public void MaterialEventRulesFollowTheEventType(string category, string expected)
    {
        var categories = string.IsNullOrEmpty(category) ? Array.Empty<string>() : new[] { category };
        var result = ReadyCatalystGate.Evaluate(Evidence(Article("e1", "MATERIAL", categories)), new[] { Review("e1", "MATERIAL_EVENT") }, Signoff(), "TEST", BuyDay, InWindow);
        Assert.Equal(expected, result.State);
        if (expected == "OPERATOR_ATTESTED") Assert.Contains(result.Warnings, w => w.StartsWith("EVENT"));
    }

    /// <summary>Unreviewed articles: risk-phrase blocks, others need review; a NON_MATERIAL review lifts a risk-phrase article.</summary>
    [Fact]
    public void UnreviewedArticlesNeedReviewAndRiskPhraseBlocksUntilReviewed()
    {
        var unreviewedMaterial = ReadyCatalystGate.Evaluate(Evidence(Article("m1", "MATERIAL")), Array.Empty<CatalystHumanReviewRecord>(), Signoff(), "TEST", BuyDay, InWindow);
        var uncertain = ReadyCatalystGate.Evaluate(Evidence(Article("u1", "UNCLASSIFIED")), new[] { Review("u1", "UNCERTAIN") }, Signoff(), "TEST", BuyDay, InWindow);
        var unreviewedRisk = ReadyCatalystGate.Evaluate(Evidence(Article("r1", "RISK")), Array.Empty<CatalystHumanReviewRecord>(), Signoff(), "TEST", BuyDay, InWindow);
        var reviewedRisk = ReadyCatalystGate.Evaluate(Evidence(Article("r1", "RISK")), new[] { Review("r1", "NON_MATERIAL") }, Signoff(), "TEST", BuyDay, InWindow);
        Assert.Equal("NEEDS_REVIEW", unreviewedMaterial.State);
        Assert.Equal("NEEDS_REVIEW", uncertain.State);
        Assert.Equal("BLOCKED", unreviewedRisk.State);
        Assert.Equal("OPERATOR_ATTESTED", reviewedRisk.State);
    }

    /// <summary>The newest review for an article wins; truncated news coverage or missing evidence is DATA?.</summary>
    [Fact]
    public void LatestReviewWinsAndIncompleteCoverageIsData()
    {
        var reviews = new[] { Review("a1", "MATERIAL_RISK", InWindow.AddHours(-3)), Review("a1", "NON_MATERIAL", InWindow.AddHours(-1)) };
        Assert.Equal("OPERATOR_ATTESTED", ReadyCatalystGate.Evaluate(Evidence(Article("a1", "MATERIAL")), reviews, Signoff(), "TEST", BuyDay, InWindow).State);
        Assert.Equal("DATA?", ReadyCatalystGate.Evaluate(Evidence() with { CoverageComplete = false }, Array.Empty<CatalystHumanReviewRecord>(), Signoff(), "TEST", BuyDay, InWindow).State);
        Assert.Equal("DATA?", ReadyCatalystGate.Evaluate(null, Array.Empty<CatalystHumanReviewRecord>(), Signoff(), "TEST", BuyDay, InWindow).State);
    }

    // ---------- sign-off store ----------

    /// <summary>Sign-off validation: at least one known source, OTHER needs a note, only on the buy-session day; expires at the deadline.</summary>
    [Fact]
    public void SignoffValidationAndExpiry()
    {
        Assert.Throws<ArgumentException>(() => CatalystAttestationStore.CreateRecord(new("TEST", Array.Empty<string>(), null), InWindow));
        Assert.Throws<ArgumentException>(() => CatalystAttestationStore.CreateRecord(new("TEST", new[] { "TWITTER" }, null), InWindow));
        Assert.Throws<ArgumentException>(() => CatalystAttestationStore.CreateRecord(new("TEST", new[] { "OTHER" }, null), InWindow));
        Assert.Throws<ArgumentException>(() => CatalystAttestationStore.CreateRecord(new("TEST", new[] { "SEC_EDGAR" }, null), Et(new DateOnly(2026, 11, 14), 12, 0))); // Saturday
        Assert.Throws<ArgumentException>(() => CatalystAttestationStore.CreateRecord(new("TEST", new[] { "SEC_EDGAR" }, null), Et(BuyDay, 17, 5)));  // after the late deadline
        var late = CatalystAttestationStore.CreateRecord(new("TEST", new[] { "SEC_EDGAR" }, null), Et(BuyDay, 16, 5)); // late window: accepted
        Assert.Equal(Et(BuyDay, 17, 0), late.ExpiresAtUtc);

        var record = CatalystAttestationStore.CreateRecord(new("test", new[] { "sec_edgar", "SCHWAB_NEWS" }, null), InWindow);
        Assert.Equal("TEST", record.Symbol);
        Assert.Equal(BuyDay, record.BuySessionDate);
        Assert.Equal(Et(BuyDay, 17, 0), record.ExpiresAtUtc);                     // late deadline (2026-11-16 decision)
        Assert.True(CatalystAttestationStore.IsValidFor(record, "TEST", BuyDay, Et(BuyDay, 16, 59)));
        Assert.False(CatalystAttestationStore.IsValidFor(record, "TEST", BuyDay, Et(BuyDay, 17, 0)));
    }

    /// <summary>The store appends and returns the valid sign-off for the session (temp file).</summary>
    [Fact]
    public async Task SignoffStoreRoundTrip()
    {
        var path = Path.Combine(Path.GetTempPath(), "mcc-attest-" + Guid.NewGuid().ToString("N") + ".jsonl");
        try
        {
            var store = new CatalystAttestationStore(path);
            await store.AppendAsync(new("TEST", new[] { "COMPANY_IR" }, "checked IR page"), InWindow, CancellationToken.None);
            var valid = await store.GetValidAsync("TEST", BuyDay, InWindow.AddMinutes(1), CancellationToken.None);
            var none = await store.GetValidAsync("TEST", BuyDay, Et(BuyDay, 17, 1), CancellationToken.None);
            Assert.NotNull(valid);
            Assert.Equal("checked IR page", valid!.Note);
            Assert.Null(none);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    // ---------- shadow summary ----------

    /// <summary>Outcomes: TOUCHED / NOT_TOUCHED from the complete Day 2 window, PENDING before it ends or before capture,
    /// INCOMPLETE for an incomplete window, NOT_SIGNALLED without READY; the limit of the first READY is used.</summary>
    [Fact]
    public void ShadowOutcomes()
    {
        var entries = new[]
        {
            Entry("AAA", "NOT_READY", null, InWindow.AddMinutes(-10)),
            Entry("AAA", "READY", 10.10m, InWindow.AddMinutes(-5)),
            Entry("AAA", "READY", 10.50m, InWindow),
            Entry("BBB", "READY", 20.20m, InWindow),
            Entry("CCC", "READY", 30.30m, InWindow),
            Entry("DDD", "READY", 40.40m, InWindow),
            Entry("EEE", "NOT_READY", null, InWindow),
        };
        var windows = new Dictionary<(string, DateOnly), BoringOvernightSellWindowAudit>
        {
            [("AAA", SellDay)] = Window(10.20m, true),
            [("BBB", SellDay)] = Window(20.00m, true),
            [("CCC", SellDay)] = Window(31.00m, false),
        };
        var afterDay2 = Et(SellDay, 20, 30);
        var rows = ReadyShadowSummary.Build(entries, windows, afterDay2).ToDictionary(r => r.Symbol);

        Assert.Equal("TOUCHED", rows["AAA"].Outcome);          // first READY limit 10.10 <= high 10.20
        Assert.Equal(10.10m, rows["AAA"].LimitAtFirstReady);
        Assert.Equal(3, rows["AAA"].Evaluations);
        Assert.Equal("NOT_TOUCHED", rows["BBB"].Outcome);
        Assert.Equal("INCOMPLETE", rows["CCC"].Outcome);
        Assert.Equal("PENDING", rows["DDD"].Outcome);          // window not captured
        Assert.Equal("NOT_SIGNALLED", rows["EEE"].Outcome);

        var beforeEnd = ReadyShadowSummary.Build(entries, windows, Et(SellDay, 20, 10)).Single(r => r.Symbol == "AAA");
        Assert.Equal("PENDING", beforeEnd.Outcome);            // 20:00 ET + 16 min not reached
        Assert.Equal(1, ReadyShadowSummary.CountLoggedSessions(entries));
    }

    // ---------- helpers ----------

    private const string GatePass = BoringOvernightReadyEvaluator.GatePass;

    /// <summary>Inputs in the final hour with a given 1M state on the snapshot.</summary>
    private static void Snapshot1M(DateTimeOffset at, string oneMinuteState, out ReadyInputs inputs)
    {
        var snapshot = Snapshot(at.AddMinutes(-2)) with
        {
            EvaluationRequest = Snapshot(at).EvaluationRequest with { OneMinuteState = oneMinuteState }
        };
        inputs = Inputs(at) with { Snapshot = snapshot };
    }

    /// <summary>
    /// Inputs at 16:30 ET (late window): intraday data stale/missing after the close, Daily data fresh, a sign-off valid
    /// until 17:00 ET, and the given after-hours trade and regular-close reference.
    /// </summary>
    private static ReadyInputs LateInputs(decimal trade, decimal? regularClose)
    {
        var at = Et(BuyDay, 16, 30);
        var snapshot = Snapshot(at.AddMinutes(-2)) with
        {
            EvaluationRequest = Snapshot(at).EvaluationRequest with
            {
                DataFresh = false, DailyDataFresh = true, FiveMinutePermission = null, FiveMinuteStructure = null,
                OneMinuteState = null, ExtensionAtr = null, CurrentVolumeVsAverage = null
            }
        };
        return Inputs(at) with
        {
            Snapshot = snapshot,
            Attestation = Signoff() with { ExpiresAtUtc = Et(BuyDay, 17, 0) },
            LiveTradePrice = trade,
            LiveTradeTimestampUtc = at.AddSeconds(-30),
            RegularCloseReference = regularClose,
            RegularCloseReferenceSource = regularClose is null ? null : "test"
        };
    }

    private static ReadyInputs Inputs(DateTimeOffset? now = null)
    {
        var at = now ?? InWindow;
        return new ReadyInputs("TEST", at, MarketSessionClock.GetSnapshot(at), Snapshot(at.AddMinutes(-2)), false,
            Array.Empty<CatalystHumanReviewRecord>(), Signoff(), Sell("PROVISIONAL", 0.78m), null, "shadow", 0,
            50.00m, at.AddSeconds(-30), "iex");
    }

    private static BoringOvernightCandidateSnapshot Snapshot(DateTimeOffset asOf) => new(
        "snap-1", "TEST", asOf, asOf, "test", "Alpaca", "iex", "test",
        new Dictionary<string, DateTimeOffset>(),
        new BoringOvernightEvaluationRequest("TEST", true, 50, 45, 1_000_000, 70, 0.3, 0.4, 0.2, 0.5, false, "UNKNOWN",
            true, "BULLISH", "HOLD", 1.0, 1.2, 2.0, 2.0),
        null, null, Evidence());

    private static BoringOvernightCatalystEvidence Evidence(params BoringOvernightCatalystArticleEvidence[] articles) =>
        new("Alpaca News", articles.Length == 0 ? "UNKNOWN" : "MATERIAL_EVENT_REVIEW", InWindow.AddHours(-24), InWindow, InWindow,
            true, 1, articles.Length, "MCC-CATALYST-TRIAGE-1.0", articles);

    private static BoringOvernightCatalystArticleEvidence Article(string id, string severity, IReadOnlyList<string>? categories = null) =>
        new(id, "Headline " + id, InWindow.AddHours(-2), "https://example.test/" + id, severity, categories ?? Array.Empty<string>(), "test");

    private static CatalystHumanReviewRecord Review(string articleId, string label, DateTimeOffset? at = null) =>
        new(Guid.NewGuid().ToString("N"), "TEST", articleId, "Headline " + articleId, "https://example.test/" + articleId,
            InWindow.AddHours(-2), "Alpaca News", label, "MCC-CATALYST-TRIAGE-1.0", "synthetic test review note text", at ?? InWindow.AddHours(-1));

    private static CatalystAttestation Signoff() =>
        new("att-1", "TEST", BuyDay, new[] { "SEC_EDGAR" }, null, Et(BuyDay, 15, 0), Et(BuyDay, 16, 0));

    private static ReadySellEvidence Sell(string status, decimal? targetPct, params (string Code, bool Passed)[] failing)
    {
        var gates = new List<SellLimitEvidenceGate>
        {
            new("SIP_REQUIRED", true, "test"), new("HISTORY_40_OF_40", true, "test"),
            new("REFERENCE_CLOSE", true, "test"), new("WALK_FORWARD_VALIDATION", true, "test"),
            new("ATR_PARITY", false, "no TOS data", Required: false)
        };
        foreach (var (code, passed) in failing)
        {
            gates.RemoveAll(g => g.Code == code);
            gates.Add(new(code, passed, "test"));
        }
        return new ReadySellEvidence(status, 50, targetPct, gates);
    }

    private static ReadyShadowEntry Entry(string symbol, string status, decimal? limit, DateTimeOffset at) =>
        new(Guid.NewGuid().ToString("N"), symbol, BuyDay, SellDay, at, status, "shadow", limit, limit is null ? null : 10m, 1m, Array.Empty<string>());

    private static BoringOvernightSellWindowAudit Window(decimal high, bool complete) =>
        new(BuyDay, SellDay, Et(BuyDay, 15, 59), 10m, Et(SellDay, 7, 0), Et(SellDay, 20, 0), 780, 400,
            complete ? 0 : 5, Array.Empty<DateTimeOffset>(), null, null, high, complete);

    private static DateTimeOffset Et(DateOnly date, int hour, int minute)
    {
        var local = date.ToDateTime(new TimeOnly(hour, minute), DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Eastern.GetUtcOffset(local)).ToUniversalTime();
    }
}
