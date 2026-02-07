using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceDb.Services;
using Xunit;

namespace SpaceDb.Tests.Services
{
    /// <summary>
    /// Basic tests for WorkflowLogService interface
    /// </summary>
    public class WorkflowLogServiceTests
    {
        private readonly Mock<ILogger<WorkflowLogService>> _loggerMock;

        public WorkflowLogServiceTests()
        {
            _loggerMock = new Mock<ILogger<WorkflowLogService>>();
        }

        [Fact]
        public void WorkflowLogSeverity_ShouldHaveCorrectValues()
        {
            // Assert
            ((int)WorkflowLogSeverity.Info).Should().Be(1);
            ((int)WorkflowLogSeverity.Warning).Should().Be(2);
            ((int)WorkflowLogSeverity.Error).Should().Be(3);
        }

        [Fact]
        public void WorkflowLogEntry_ShouldHaveRequiredProperties()
        {
            // Arrange & Act
            var entry = new WorkflowLogEntry
            {
                Id = 1,
                WorkflowId = 100,
                Message = "Test message",
                Time = DateTime.UtcNow,
                Severity = WorkflowLogSeverity.Info
            };

            // Assert
            entry.Id.Should().Be(1);
            entry.WorkflowId.Should().Be(100);
            entry.Message.Should().Be("Test message");
            entry.Severity.Should().Be(WorkflowLogSeverity.Info);
        }
    }
}
