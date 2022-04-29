using System;
using Xunit;
using FluentAssertions;
using Moq;
using NLog;

namespace Library.Logger.Tests;

public class NLogLoggerTests
{
    private readonly ILogger _sut;
    // private readonly ILogger _sutDefaultConstructor;
    private readonly Mock<NLogLogger> _sutMock;
    // private readonly Mock<NLogLogger> _sutMockDefaultConstructor;

    public NLogLoggerTests()
    {
        _sut = new NLogLogger(new NLogLoggerConfiguration()
        {
            LogLevel = LogLevel.Off,
            LogToConsole = true
        });
        
        _sutMock = new Mock<NLogLogger>(new NLogLoggerConfiguration()
        {
            LogLevel = LogLevel.Off,
            LogToConsole = true
        });
    }

    [Fact]
    public void DefaultConstructor_Trace_ShouldLogAppropriateMessage()
    {
        // Arrange
        var sutMock = new Mock<NLogLogger>();
        var sut = new NLogLogger();
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";
        
        // Act
        Action act = () =>
        {
            sut.Trace(message);
            sutMock.Object.Trace(message);
        };

        // Assert   
        act.Should().NotThrow();
        sutMock.Verify(x => x.Trace(message), Times.Once);
    }
    
    [Fact]
    public void Trace_ShouldLogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";

        // Act
        Action act = () =>
        {
            _sut.Trace(message);
            _sutMock.Object.Trace(message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sutMock.Verify(x => x.Trace(message), Times.Once);
    }
    
    [Fact]
    public void Trace_WithException_ShouldLogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";
        var ex = new Exception();

        // Act
        Action act = () =>
        {
            _sut.Trace(ex, message);
            _sutMock.Object.Trace(ex, message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sutMock.Verify(x => x.Trace(ex, message), Times.Once);
    }
    
    [Fact]
    public void Debug_ShouldLogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";

        // Act
        Action act = () =>
        {
            _sut.Debug(message);
            _sutMock.Object.Debug(message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sutMock.Verify(x => x.Debug(message), Times.Once);
    }
    
    [Fact]
    public void Debug_WithException_ShouldLogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";
        var ex = new Exception();

        // Act
        Action act = () =>
        {
            _sut.Debug(ex, message);
            _sutMock.Object.Debug(ex, message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sutMock.Verify(x => x.Debug(ex, message), Times.Once);
    }
    
    [Fact]
    public void Info_ShouldLogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";

        // Act
        Action act = () =>
        {
            _sut.Info(message);
            _sutMock.Object.Info(message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sutMock.Verify(x => x.Info(message), Times.Once);
    }
    
    [Fact]
    public void Info_WithException_ShouldLogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";
        var ex = new Exception();

        // Act
        Action act = () =>
        {
            _sut.Info(ex, message);
            _sutMock.Object.Info(ex, message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sutMock.Verify(x => x.Info(ex, message), Times.Once);
    }
    
    [Fact]
    public void Warn_ShouldLogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";

        // Act
        Action act = () =>
        {
            _sut.Warn(message);
            _sutMock.Object.Warn(message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sutMock.Verify(x => x.Warn(message), Times.Once);
    }
    
    [Fact]
    public void Warn_WithException_ShouldLogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";
        var ex = new Exception();

        // Act
        Action act = () =>
        {
            _sut.Warn(ex, message);
            _sutMock.Object.Warn(ex, message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sutMock.Verify(x => x.Warn(ex, message), Times.Once);
    }
    
    [Fact]
    public void Error_ShouldLogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";

        // Act
        Action act = () =>
        {
            _sut.Error(message);
            _sutMock.Object.Error(message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sutMock.Verify(x => x.Error(message), Times.Once);
    }
    
    [Fact]
    public void Error_WithException_ShouldLogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";
        var ex = new Exception();

        // Act
        Action act = () =>
        {
            _sut.Error(ex, message);
            _sutMock.Object.Error(ex, message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sutMock.Verify(x => x.Error(ex, message), Times.Once);
    }
    
    [Fact]
    public void Fatal_ShouldLogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";

        // Act
        Action act = () =>
        {
            _sut.Fatal(message);
            _sutMock.Object.Fatal(message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sutMock.Verify(x => x.Fatal(message), Times.Once);
    }
    
    [Fact]
    public void Fatal_WithException_ShouldLogAppropriateMessage()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string message = $"Test:{guid}";
        var ex = new Exception();

        // Act
        Action act = () =>
        {
            _sut.Fatal(ex, message);
            _sutMock.Object.Fatal(ex, message);
        };
        
        // Assert
        act.Should().NotThrow();
        _sutMock.Verify(x => x.Fatal(ex, message), Times.Once);
    }
}