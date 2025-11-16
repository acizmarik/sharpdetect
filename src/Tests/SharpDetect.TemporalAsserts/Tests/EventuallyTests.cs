// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.TemporalAsserts.TemporalOperators;
using Xunit;

namespace SharpDetect.TemporalAsserts.Tests;

public class EventuallyTests
{
    private static readonly List<IEvent<int, string>> _testEvents = new()
    {
        new Event<int, string, object>(1, "A", new object()),
        new Event<int, string, object>(2, "B", new object()),
        new Event<int, string, object>(3, "C", new object()),
        new Event<int, string, object>(4, "D", new object()),
    };
    
    [Fact]
    public void Eventually_Is_Satisfied_When_Match()
    {
        // Arrange
        var eventually = new EventuallyOperator<int, string>(new AtomicPredicate<int, string>(evt => evt.Type == "A"));

        // Act
        eventually.Evaluate(_testEvents);
        
        // Assert
        Assert.Equal(AssertStatus.Satisfied, eventually.Status);
    }
    
    [Fact]
    public void Eventually_Is_Violated_When_NotMatch()
    {
        // Arrange
        var eventually = new EventuallyOperator<int, string>(new AtomicPredicate<int, string>(evt => evt.Type == "Z"));
        
        // Act
        eventually.Evaluate(_testEvents);
        
        // Assert
        Assert.Equal(AssertStatus.Violated, eventually.Status);
    }
        
    [Fact]
    public void EventuallyThen_Is_Satisfied_When_Match()
    {
        // Arrange
        var eventually = new EventuallyOperator<int, string>(new AtomicPredicate<int, string>(evt => evt.Type == "B"))
            .Then(new EventuallyOperator<int, string>(new AtomicPredicate<int, string>(evt => evt.Type == "D")));

        // Act
        eventually.Evaluate(_testEvents);
        
        // Assert
        Assert.Equal(AssertStatus.Satisfied, eventually.Status);
    }
    
    [Fact]
    public void EventuallyThen_Is_Violated_When_NoMatch()
    {
        // Arrange
        var eventually = new EventuallyOperator<int, string>(new AtomicPredicate<int, string>(evt => evt.Type == "C"))
            .Then(new EventuallyOperator<int, string>(new AtomicPredicate<int, string>(evt => evt.Type == "A")));

        // Act
        eventually.Evaluate(_testEvents);
        
        // Assert
        Assert.Equal(AssertStatus.Violated, eventually.Status);
    }
    
    [Fact]
    public void EventuallyThenThen_Is_Satisfied_When_Match()
    {
        // Arrange
        var eventually = new EventuallyOperator<int, string>(new AtomicPredicate<int, string>(evt => evt.Type == "B"))
            .Then(new EventuallyOperator<int, string>(new AtomicPredicate<int, string>(evt => evt.Type == "C")))
            .Then(new EventuallyOperator<int, string>(new AtomicPredicate<int, string>(evt => evt.Type == "D")));

        // Act
        eventually.Evaluate(_testEvents);
        
        // Assert
        Assert.Equal(AssertStatus.Satisfied, eventually.Status);
    }
}