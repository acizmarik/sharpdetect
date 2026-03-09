// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using HandlebarsDotNet;
using SharpDetect.Reporting.Services;
using Xunit;

namespace SharpDetect.Reporting.Tests;

public class HtmlReportRendererTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("simple", "simple")]
    [InlineData("with space", "with_space")]
    [InlineData("single'quote", "single_quote")]
    [InlineData("double\"quote", "double_quote")]
    [InlineData("<script>", "_script_")]
    [InlineData("T2 (Test'<>weirdName``''\"\")", "T2__Test___weirdName_______")]
    [InlineData("already-safe_123", "already-safe_123")]
    public void Slugify_ReturnsOnlySafeCharacters(string input, string expected)
    {
        Assert.Equal(expected, HtmlReportRenderer.Slugify(input));
    }

    [Fact]
    public void SlugifyHelper_InTemplate_SingleQuoteDoesNotBreakOnclickHandler()
    {
        var environment = CreateEnvironmentWithSlugifyHelper();
        var template = environment.Compile("""<button onclick="openTab('{{slugify name}}')">{{name}}</button>""");
        var result = template(new { name = "Thread'1" });

        Assert.DoesNotContain("openTab('Thread'1')", result);
        Assert.Contains("openTab('Thread_1')", result);
    }

    [Fact]
    public void SlugifyHelper_InTemplate_AngleBracketsDoNotBreakIdAttribute()
    {
        var environment = CreateEnvironmentWithSlugifyHelper();
        var template = environment.Compile("<div id=\"{{slugify name}}\"></div>");
        var result = template(new { name = "<inject>" });

        Assert.Contains("id=\"_inject_\"", result);
    }

    [Fact]
    public void SlugifyHelper_DisplayText_IsHtmlEncoded()
    {
        var environment = CreateEnvironmentWithSlugifyHelper();
        var template = environment.Compile("<span>{{name}}</span>");
        var result = template(new { name = "<b>bold</b>" });

        Assert.Contains("&lt;b&gt;bold&lt;/b&gt;", result);
        Assert.DoesNotContain("<b>bold</b>", result);
    }
    
    private static IHandlebars CreateEnvironmentWithSlugifyHelper()
    {
        var environment = Handlebars.CreateSharedEnvironment();
        environment.RegisterHelper("slugify", (output, _, arguments) =>
        {
            var value = arguments[0]?.ToString() ?? string.Empty;
            output.WriteSafeString(HtmlReportRenderer.Slugify(value));
        });
        return environment;
    }
}






