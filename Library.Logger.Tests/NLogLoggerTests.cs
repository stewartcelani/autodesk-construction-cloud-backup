using System;
using Xunit;
using FluentAssertions;
using NSubstitute;

namespace Library.Logger.Tests;

public class NLogLoggerTests
{
    private readonly ILogger _sut;
    
    private const LogLevel DefaultLogLevel = LogLevel.Info;
    private const bool DefaultLogToConsole = true;
    
    public NLogLoggerTests()
    {
        _sut = Substitute.For<NLogLogger>(new NLogLoggerConfiguration()
        {
            LogLevel = LogLevel.Trace,
            LogToConsole = true
        });
    }
    
    [Fact]
    public void Invoking_WithNoParameters_Should_HaveAppropriateConfigurationSet()
    {
        // Act
        ILogger sut = new NLogLogger();
        
        // Assert   
        sut.Config.LogLevel.Should().Be(DefaultLogLevel);
        sut.Config.LogToConsole.Should().Be(DefaultLogToConsole);
    }
    
    [Fact]
    public void Invoking_WithDefaultConfiguration_Should_HaveAppropriateConfigurationSet()
    {
        // Arrange
        ILoggerConfiguration config = new NLogLoggerConfiguration();

        // Act
        ILogger sut = new NLogLogger(config);
        
        // Assert   
        sut.Config.LogLevel.Should().Be(DefaultLogLevel);
        sut.Config.LogToConsole.Should().Be(DefaultLogToConsole);
    }
    
    [Fact]
    public void Invoking_WithCustomConfiguration_Should_HaveAppropriateConfigurationSet()
    {
        // Arrange
        const LogLevel logLevel = LogLevel.Error;
        const bool logToConsole = false;
        ILoggerConfiguration config = new NLogLoggerConfiguration()
        {
            LogLevel = logLevel,
            LogToConsole = logToConsole
        };
        
        // Act
        ILogger sut = new NLogLogger(config);
        
        // Assert   
        sut.Config.LogLevel.Should().Be(logLevel);
        sut.Config.LogToConsole.Should().Be(logToConsole);
    }
    
    [Fact]
    public void Trace_Should_LogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";

        // Act
        Action act = () =>
        {
            _sut.Trace(message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sut.Received(1).Trace(message);
    }

    [Fact]
    public void Trace_WithException_Should_LogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";
        var ex = new Exception();

        // Act
        Action act = () =>
        {
            _sut.Trace(ex, message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sut.Received(1).Trace(ex, message);
    }
    
    [Fact]
    public void Debug_Should_LogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";

        // Act
        Action act = () =>
        {
            _sut.Debug(message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sut.Received(1).Debug(message);
    }
    
    [Fact]
    public void Debug_WithException_Should_LogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";
        var ex = new Exception();

        // Act
        Action act = () =>
        {
            _sut.Debug(ex, message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sut.Received(1).Debug(ex, message);
    }
    
    [Fact]
    public void Info_Should_LogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";

        // Act
        Action act = () =>
        {
            _sut.Info(message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sut.Received(1).Info(message);
    }
    
    [Fact]
    public void Info_WithException_Should_LogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";
        var ex = new Exception();

        // Act
        Action act = () =>
        {
            _sut.Info(ex, message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sut.Received(1).Info(ex, message);
    }
    
    [Fact]
    public void Warn_Should_LogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";

        // Act
        Action act = () =>
        {
            _sut.Warn(message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sut.Received(1).Warn(message);
    }
    
    [Fact]
    public void Warn_WithException_Should_LogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";
        var ex = new Exception();

        // Act
        Action act = () =>
        {
            _sut.Warn(ex, message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sut.Received(1).Warn(ex, message);
    }
    
    [Fact]
    public void Error_Should_LogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";

        // Act
        Action act = () =>
        {
            _sut.Error(message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sut.Received(1).Error(message);
    }
    
    [Fact]
    public void Error_WithException_Should_LogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";
        var ex = new Exception();

        // Act
        Action act = () =>
        {
            _sut.Error(ex, message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sut.Received(1).Error(ex, message);
    }
    
    [Fact]
    public void Fatal_Should_LogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";

        // Act
        Action act = () =>
        {
            _sut.Fatal(message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sut.Received(1).Fatal(message);
    }
    
    [Fact]
    public void Fatal_WithException_Should_LogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";
        var ex = new Exception();

        // Act
        Action act = () =>
        {
            _sut.Fatal(ex, message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sut.Received(1).Fatal(ex, message);
    }
}