using System;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Library.Logger.UnitTests;

public class NLogLoggerUnitTests
{
    private const LogLevel DefaultLogLevel = LogLevel.Info;
    private const bool DefaultLogToConsole = true;
    private readonly ILogger _sut; // workaround for Rider coverage not picking up test for logging methods with mock
    private readonly ILogger _sutMock;

    public NLogLoggerUnitTests()
    {
        var config = new NLogLoggerConfiguration
        {
            LogLevel = LogLevel.Trace,
            LogToConsole = true
        };
        _sutMock = Substitute.For<NLogLogger>(config);
        _sut = new NLogLogger(config);
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
        ILoggerConfiguration config = new NLogLoggerConfiguration
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
    public void Invoking_ILogLevel_Should_BeProperlyMappedToNLogLogLevel()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";
        const bool logToConsole = true;

        // Act
        ILogger off = new NLogLogger(new NLogLoggerConfiguration
        {
            LogLevel = LogLevel.Off,
            LogToConsole = logToConsole
        });
        ILogger trace = new NLogLogger(new NLogLoggerConfiguration
        {
            LogLevel = LogLevel.Trace,
            LogToConsole = logToConsole
        });
        ILogger debug = new NLogLogger(new NLogLoggerConfiguration
        {
            LogLevel = LogLevel.Debug,
            LogToConsole = logToConsole
        });
        ILogger info = new NLogLogger(new NLogLoggerConfiguration
        {
            LogLevel = LogLevel.Info,
            LogToConsole = logToConsole
        });
        ILogger warn = new NLogLogger(new NLogLoggerConfiguration
        {
            LogLevel = LogLevel.Warn,
            LogToConsole = logToConsole
        });
        ILogger error = new NLogLogger(new NLogLoggerConfiguration
        {
            LogLevel = LogLevel.Error,
            LogToConsole = logToConsole
        });
        ILogger fatal = new NLogLogger(new NLogLoggerConfiguration
        {
            LogLevel = LogLevel.Fatal,
            LogToConsole = logToConsole
        });
        Action act = () =>
        {
            off.Trace(message);
            trace.Trace(message);
            debug.Debug(message);
            info.Info(message);
            warn.Warn(message);
            error.Error(message);
            fatal.Fatal(message);
        };

        // Assert   
        act.Should().NotThrow();
    }

    [Fact]
    public void Invoking_ILogLevel_Should_ThrowIfNotSupportedByNLog()
    {
        // Arrange
        ILogger sut;

        // Act
        Action invoking = () =>
        {
            sut = new NLogLogger(new NLogLoggerConfiguration
            {
                LogLevel = LogLevel.Silly,
                LogToConsole = true
            });
        };

        // Assert   
        invoking.Should().Throw<ArgumentOutOfRangeException>();
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
            _sutMock.Trace(message);
        };


        // Assert
        act.Should().NotThrow();
        _sutMock.Received(1).Trace(message);
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
            _sutMock.Trace(ex, message);
        };

        // Assert
        act.Should().NotThrow();
        _sutMock.Received(1).Trace(ex, message);
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
            _sutMock.Debug(message);
        };

        // Assert
        act.Should().NotThrow();
        _sutMock.Received(1).Debug(message);
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
            _sutMock.Debug(ex, message);
        };

        // Assert
        act.Should().NotThrow();
        _sutMock.Received(1).Debug(ex, message);
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
            _sutMock.Info(message);
        };

        // Assert
        act.Should().NotThrow();
        _sutMock.Received(1).Info(message);
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
            _sutMock.Info(ex, message);
        };

        // Assert
        act.Should().NotThrow();
        _sutMock.Received(1).Info(ex, message);
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
            _sutMock.Warn(message);
        };

        // Assert
        act.Should().NotThrow();
        _sutMock.Received(1).Warn(message);
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
            _sutMock.Warn(ex, message);
        };

        // Assert
        act.Should().NotThrow();
        _sutMock.Received(1).Warn(ex, message);
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
            _sutMock.Error(message);
        };

        // Assert
        act.Should().NotThrow();
        _sutMock.Received(1).Error(message);
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
            _sutMock.Error(ex, message);
        };

        // Assert
        act.Should().NotThrow();
        _sutMock.Received(1).Error(ex, message);
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
            _sutMock.Fatal(message);
        };

        // Assert
        act.Should().NotThrow();
        _sutMock.Received(1).Fatal(message);
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
            _sutMock.Fatal(ex, message);
        };

        // Assert
        act.Should().NotThrow();
        _sutMock.Received(1).Fatal(ex, message);
    }
}