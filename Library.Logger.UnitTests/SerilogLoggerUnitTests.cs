using System;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Library.Logger.UnitTests;

public class SerilogLoggerUnitTests
{
    private const LogLevel DefaultLogLevel = LogLevel.Info;
    private const bool DefaultLogToConsole = true;
    private readonly ILogger _sut;
    private readonly ILogger _sutMock;

    public SerilogLoggerUnitTests()
    {
        var config = new SerilogLoggerConfiguration
        {
            LogLevel = LogLevel.Trace,
            LogToConsole = true
        };
        _sutMock = Substitute.For<SerilogLogger>(config);
        _sut = new SerilogLogger(config);
    }

    [Fact]
    public void Invoking_WithNoParameters_Should_HaveAppropriateConfigurationSet()
    {
        // Act
        ILogger sut = new SerilogLogger();

        // Assert
        sut.Config.LogLevel.Should().Be(DefaultLogLevel);
        sut.Config.LogToConsole.Should().Be(DefaultLogToConsole);
    }

    [Fact]
    public void Invoking_WithParameters_Should_HaveSpecifiedConfigurationSet()
    {
        // Arrange
        var config = new SerilogLoggerConfiguration
        {
            LogLevel = LogLevel.Trace,
            LogToConsole = false
        };

        // Act
        ILogger sut = new SerilogLogger(config);

        // Assert
        sut.Config.LogLevel.Should().Be(LogLevel.Trace);
        sut.Config.LogToConsole.Should().Be(false);
    }

    [Fact]
    public void Invoking_Trace_Should_CallLog()
    {
        // Act
        _sutMock.Trace("test");

        // Assert
        _sutMock.Received(1).Trace("test");
    }

    [Fact]
    public void Invoking_TraceWithException_Should_CallLog()
    {
        // Arrange
        var ex = new Exception("test");

        // Act
        _sutMock.Trace(ex, "test");

        // Assert
        _sutMock.Received(1).Trace(ex, "test");
    }

    [Fact]
    public void Invoking_Debug_Should_CallLog()
    {
        // Act
        _sutMock.Debug("test");

        // Assert
        _sutMock.Received(1).Debug("test");
    }

    [Fact]
    public void Invoking_DebugWithException_Should_CallLog()
    {
        // Arrange
        var ex = new Exception("test");

        // Act
        _sutMock.Debug(ex, "test");

        // Assert
        _sutMock.Received(1).Debug(ex, "test");
    }

    [Fact]
    public void Invoking_Info_Should_CallLog()
    {
        // Act
        _sutMock.Info("test");

        // Assert
        _sutMock.Received(1).Info("test");
    }

    [Fact]
    public void Invoking_InfoWithException_Should_CallLog()
    {
        // Arrange
        var ex = new Exception("test");

        // Act
        _sutMock.Info(ex, "test");

        // Assert
        _sutMock.Received(1).Info(ex, "test");
    }

    [Fact]
    public void Invoking_Warn_Should_CallLog()
    {
        // Act
        _sutMock.Warn("test");

        // Assert
        _sutMock.Received(1).Warn("test");
    }

    [Fact]
    public void Invoking_WarnWithException_Should_CallLog()
    {
        // Arrange
        var ex = new Exception("test");

        // Act
        _sutMock.Warn(ex, "test");

        // Assert
        _sutMock.Received(1).Warn(ex, "test");
    }

    [Fact]
    public void Invoking_Error_Should_CallLog()
    {
        // Act
        _sutMock.Error("test");

        // Assert
        _sutMock.Received(1).Error("test");
    }

    [Fact]
    public void Invoking_ErrorWithException_Should_CallLog()
    {
        // Arrange
        var ex = new Exception("test");

        // Act
        _sutMock.Error(ex, "test");

        // Assert
        _sutMock.Received(1).Error(ex, "test");
    }

    [Fact]
    public void Invoking_Fatal_Should_CallLog()
    {
        // Act
        _sutMock.Fatal("test");

        // Assert
        _sutMock.Received(1).Fatal("test");
    }

    [Fact]
    public void Invoking_FatalWithException_Should_CallLog()
    {
        // Arrange
        var ex = new Exception("test");

        // Act
        _sutMock.Fatal(ex, "test");

        // Assert
        _sutMock.Received(1).Fatal(ex, "test");
    }
}