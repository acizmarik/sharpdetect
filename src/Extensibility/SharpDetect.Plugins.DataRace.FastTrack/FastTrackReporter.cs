// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting.Model;
using SharpDetect.Plugins.DataRace.Common;

namespace SharpDetect.Plugins.DataRace.FastTrack;

public partial class FastTrackPlugin
{
    private DataRaceReportingHelper ReportingHelper =>
        field ??= new FastTrackReportingHelper(Reporter, MetadataContext, SymbolResolver, ReportCategory, _detectedRaces, _detector);

    public Summary CreateDiagnostics() => ReportingHelper.CreateDiagnostics();

    public IEnumerable<object> CreateReportDataContext(IEnumerable<Report> reports) =>
        DataRaceReportingHelper.CreateReportDataContext(reports, _pluginConfiguration.StackTraceCollectionMaxDepth);

    private sealed class FastTrackReportingHelper : DataRaceReportingHelper
    {
        private readonly FastTrackDetector _detector;

        public FastTrackReportingHelper(
            SummaryBuilder reporter,
            Core.Metadata.IMetadataContext metadataContext,
            Core.Metadata.ISymbolResolver symbolResolver,
            string reportCategory,
            List<DataRaceInfo> detectedRaces,
            FastTrackDetector detector)
            : base(reporter, metadataContext, symbolResolver, reportCategory, detectedRaces)
        {
            _detector = detector;
        }

        protected override void AddStatisticsToReport(SummaryBuilder reporter, int raceCount, int fieldCount)
        {
            reporter.AddCollectionProperty("Tracked Threads", _detector.GetTrackedThreadCount().ToString());
            reporter.AddCollectionProperty("Tracked Shadow Variables", _detector.GetShadowVariableCount().ToString());
            reporter.AddCollectionProperty("Tracked Publications", _detector.GetTrackedPublicationCount().ToString());
            reporter.AddCollectionProperty("Data Races", raceCount.ToString());
            reporter.AddCollectionProperty("Racy Fields", fieldCount.ToString());
            reporter.AddCollectionProperty("Raw Race Occurrences", RaceOccurrenceCount.ToString());
        }

        protected override string GetViolationTitle(int raceCount, int fieldCount)  
        {
            var races = raceCount == 1 ? "1 data race" : $"{raceCount} data races";
            var fields = fieldCount == 1 ? "1 field" : $"{fieldCount} fields";
            return $"{races} in {fields}";
        }

        protected override string FormatAccessReason(DataRaceInfo race, AccessInfo access, RaceRole role)
        {
            if (role == RaceRole.Later)
            {
                var earlierThreadName = DataRaceLogger.GetThreadDisplayName(race.LastAccess);
                return $"{access.AccessType} unordered after previous {race.LastAccess.AccessType} by {earlierThreadName}";
            }
            else
            {
                var laterThreadName = DataRaceLogger.GetThreadDisplayName(race.CurrentAccess);
                return $"{access.AccessType} conflicts with later {race.CurrentAccess.AccessType} by {laterThreadName}";
            }
        }
    }
}
