// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Xunit;

namespace SharpDetect.Plugins.DataRace.Common.Tests;

public class DataRaceStackTraceResolverTests
{
    [Theory]
    [InlineData("System.Void Program/<>c::<<Main>$>b__0_0()", "Program.<>c.<<Main>$>b__0_0()")]
    [InlineData("System.Int32 Foo.Bar::Baz(System.String)", "Foo.Bar.Baz(System.String)")]
    [InlineData("System.Void Test::Method()", "Test.Method()")]
    public void GetDisplayMethodName_MetadataName_IsPrettified(string metadataName, string expected)
    {
        Assert.Equal(expected, DataRaceStackTraceResolver.GetDisplayMethodName(metadataName));
    }

    [Theory]
    [InlineData("<unresolved-method>(100663297)")]
    [InlineData("<unable-to-resolve-method>")]
    public void GetDisplayMethodName_UnresolvedPlaceholder_IsUnchanged(string placeholder)
    {
        Assert.Equal(placeholder, DataRaceStackTraceResolver.GetDisplayMethodName(placeholder));
    }

    [Theory]
    [InlineData("/usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.9/System.Private.CoreLib.dll", true)]
    [InlineData("/app/bin/MyApp.dll", false)]
    public void IsSystemModule_ClassifiesByFileName(string modulePath, bool expected)
    {
        Assert.Equal(expected, DataRaceStackTraceResolver.IsSystemModule(modulePath));
    }
}
