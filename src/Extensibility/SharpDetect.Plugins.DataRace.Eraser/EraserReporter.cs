// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting.Model;
using SharpDetect.Plugins.DataRace.Common;

namespace SharpDetect.Plugins.DataRace.Eraser;

public partial class EraserPlugin
{
    private DataRaceReportingHelper ReportingHelper =>
        field ??= new EraserReportingHelper(Reporter, MetadataContext, ReportCategory, _detectedRaces, _detector);

    public Summary CreateDiagnostics() => ReportingHelper.CreateDiagnostics();

    public IEnumerable<object> CreateReportDataContext(IEnumerable<Report> reports) =>
        DataRaceReportingHelper.CreateReportDataContext(reports);

    private sealed class EraserReportingHelper : DataRaceReportingHelper
    {
        private readonly EraserDetector _detector;

        public EraserReportingHelper(
            SummaryBuilder reporter,
            Core.Metadata.IMetadataContext metadataContext,
            string reportCategory,
            List<DataRaceInfo> detectedRaces,
            EraserDetector detector)
            : base(reporter, metadataContext, reportCategory, detectedRaces)
        {
            _detector = detector;
        }

        protected override void AddStatisticsToReport(SummaryBuilder reporter)
        {
            reporter.AddCollectionProperty("Distinct Lock Sets", _detector.GetDistinctLockSetCount().ToString());
            reporter.AddCollectionProperty("Tracked Fields", _detector.GetTrackedFieldCount().ToString());
            reporter.AddCollectionProperty("(Potential) Data Races", DetectedRaceCount.ToString());
        }
        
        protected override string GetViolationTitle(int raceCount) =>
            raceCount == 1
                ? "One potential data race detected"
                : $"Several ({raceCount}) potential data races detected";

        protected override string FormatAccessReason(DataRaceInfo race, AccessInfo access, RaceRole role)
        {
            if (role == RaceRole.Triggering)
            {
                var lastThreadName = race.LastAccess.ThreadName ?? "unknown";
                var lastAccess = race.LastAccess.AccessType;
                return $"{access.AccessType} with empty lock set, unordered after {lastAccess} by {lastThreadName}";
            }
            else
            {
                var otherThreadName = race.CurrentAccess.ThreadName ?? "unknown";
                return $"{access.AccessType} conflicts with {race.CurrentAccess.AccessType} by {otherThreadName}";
            }
        }
    }
}
