// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting.Model;
using SharpDetect.Plugins.DataRace.Common;

namespace SharpDetect.Plugins.DataRace.FastTrack;

public partial class FastTrackPlugin
{
    private DataRaceReportingHelper ReportingHelper =>
        field ??= new FastTrackReportingHelper(Reporter, MetadataContext, ReportCategory, _detectedRaces, _detector);

    public Summary CreateDiagnostics() => ReportingHelper.CreateDiagnostics();

    public IEnumerable<object> CreateReportDataContext(IEnumerable<Report> reports) =>
        DataRaceReportingHelper.CreateReportDataContext(reports);

    private sealed class FastTrackReportingHelper : DataRaceReportingHelper
    {
        private readonly FastTrackDetector _detector;

        public FastTrackReportingHelper(
            SummaryBuilder reporter,
            Core.Metadata.IMetadataContext metadataContext,
            string reportCategory,
            List<DataRaceInfo> detectedRaces,
            FastTrackDetector detector)
            : base(reporter, metadataContext, reportCategory, detectedRaces)
        {
            _detector = detector;
        }

        protected override void AddStatisticsToReport(SummaryBuilder reporter)
        {
            reporter.AddCollectionProperty("Tracked Threads", _detector.GetTrackedThreadCount().ToString());
            reporter.AddCollectionProperty("Tracked Fields", _detector.GetTrackedFieldCount().ToString());
            reporter.AddCollectionProperty("Data Races", DetectedRaceCount.ToString());
        }

        protected override string GetViolationTitle(int raceCount) =>
            raceCount == 1
                ? "One data race detected"
                : $"Several ({raceCount}) data races detected";

        protected override string FormatAccessReason(DataRaceInfo race, AccessInfo access, RaceRole role)
        {
            if (role == RaceRole.Triggering)
            {
                var lastThreadName = race.LastAccess.ThreadName ?? "unknown";
                var lastAccess = race.LastAccess.AccessType;
                return $"{access.AccessType} unordered after previous {lastAccess} by {lastThreadName}";
            }
            else
            {
                var otherThreadName = race.CurrentAccess.ThreadName ?? "unknown";
                return $"{access.AccessType} conflicts with later {race.CurrentAccess.AccessType} by {otherThreadName}";
            }
        }
    }
}


