// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Xunit;

namespace SharpDetect.E2ETests.Utils;

internal static class SdkVersions
{
    private static readonly string[] AllSdks = ["net8.0", "net9.0", "net10.0", "net11.0"];
    private static readonly string[] Net9AndAboveSdks = AllSdks.Where(sdk => sdk != "net8.0").ToArray();
    private static readonly string[] Net10AndAboveSdks = Net9AndAboveSdks.Where(sdk => sdk != "net9.0").ToArray();

    public static TheoryData<string> All => new(AllSdks);

    public static TheoryData<string> Net9AndAbove => new(Net9AndAboveSdks);

    public static TheoryData<string> Net10AndAbove => new(Net10AndAboveSdks);

    public static TheoryData<string> Net11AndAbove => ["net11.0"];

    public static TheoryData<string> Net10Only =>
    [
        "net10.0",
    ];

    public static TheoryData<string, string> AllWithFastTrackOnly
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var sdk in AllSdks)
                data.Add(sdk, typeof(TestFastTrackPlugin).FullName!);
            return data;
        }
    }

    public static TheoryData<string, string> Net9AndAboveWithFastTrackOnly
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var sdk in Net9AndAboveSdks)
                data.Add(sdk, typeof(TestFastTrackPlugin).FullName!);
            return data;
        }
    }

    public static TheoryData<string, string> Net10AndAboveWithFastTrackOnly
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var sdk in Net10AndAboveSdks)
                data.Add(sdk, typeof(TestFastTrackPlugin).FullName!);
            return data;
        }
    }

    public static TheoryData<string, string> BelowNet10WithFastTrackOnly
    {
        get
        {
            var data = new TheoryData<string, string>();
            data.Add("net8.0", typeof(TestFastTrackPlugin).FullName!);
            data.Add("net9.0", typeof(TestFastTrackPlugin).FullName!);
            return data;
        }
    }

    public static TheoryData<string, string> UpToNet10WithFastTrackOnly
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var sdk in AllSdks.Where(sdk => sdk is "net8.0" or "net9.0" or "net10.0"))
                data.Add(sdk, typeof(TestFastTrackPlugin).FullName!);
            return data;
        }
    }

    public static TheoryData<string, string> Net11AndAboveWithFastTrackOnly
    {
        get
        {
            var data = new TheoryData<string, string>();
            data.Add("net11.0", typeof(TestFastTrackPlugin).FullName!);
            return data;
        }
    }

    public static TheoryData<string, string> Net10WithFastTrackOnly
    {
        get
        {
            var data = new TheoryData<string, string>();
            data.Add("net10.0", typeof(TestFastTrackPlugin).FullName!);
            return data;
        }
    }
}
