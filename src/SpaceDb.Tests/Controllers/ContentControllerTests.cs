using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceDb.Controllers;
using SpaceDb.Services;
using Xunit;

namespace SpaceDb.Tests.Controllers
{
    /// <summary>
    /// Comprehensive tests for ContentController
    /// Tests various content sizes, formats, and edge cases for fragment/block splitting
    /// </summary>
    public class ContentControllerTests
    {
        private readonly Mock<IContentParserService> _contentParserServiceMock;
        private readonly Mock<ILogger<ContentController>> _loggerMock;
        private readonly ContentController _controller;

        public ContentControllerTests()
        {
            _contentParserServiceMock = new Mock<IContentParserService>();
            _loggerMock = new Mock<ILogger<ContentController>>();
            _controller = new ContentController(_contentParserServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task UploadContent_WithSmallTextContent_ShouldReturnSuccess()
        {
            // Arrange
            var request = new UploadContentRequest
            {
                Payload = "This is a small text content that should be parsed successfully.",
                ResourceId = "small_text.txt",
                ContentType = "text",
                SingularityId = 1,
                UserId = 1
            };

            var expectedResult = new ContentParseResult
            {
                ResourcePointId = 1,
                ParserType = "text",
                BlockPointIds = new List<long> { 2 },
                FragmentPointIds = new List<long> { 3 }
            };

            _contentParserServiceMock
                .Setup(x => x.ParseAndStoreAsync(
                    request.Payload,
                    request.ResourceId,
                    request.ContentType ?? "auto",
                    request.SingularityId,
                    request.UserId,
                    request.Metadata))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.UploadContent(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _contentParserServiceMock.Verify(x => x.ParseAndStoreAsync(
                request.Payload,
                request.ResourceId,
                request.ContentType ?? "auto",
                request.SingularityId,
                request.UserId,
                request.Metadata), Times.Once);
        }

        [Fact]
        public async Task UploadContent_WithEmptyPayload_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new UploadContentRequest
            {
                Payload = "",
                ResourceId = "empty.txt",
                ContentType = "text"
            };

            // Act
            var result = await _controller.UploadContent(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UploadContent_WithEmptyResourceId_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new UploadContentRequest
            {
                Payload = "Some content",
                ResourceId = "",
                ContentType = "text"
            };

            // Act
            var result = await _controller.UploadContent(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UploadContent_WhenParsingFails_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new UploadContentRequest
            {
                Payload = "Some content",
                ResourceId = "test.txt",
                ContentType = "text"
            };

            _contentParserServiceMock
                .Setup(x => x.ParseAndStoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<long?>(),
                    It.IsAny<int?>(),
                    It.IsAny<Dictionary<string, object>?>()))
                .ReturnsAsync((ContentParseResult?)null);

            // Act
            var result = await _controller.UploadContent(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetParsers_ShouldReturnAvailableParsers()
        {
            // Arrange
            var availableParsers = new List<string> { "text", "json" };
            _contentParserServiceMock
                .Setup(x => x.GetAvailableParserTypes())
                .Returns(availableParsers);

            // Act
            var result = _controller.GetParsers();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        #region OpenCyc OWL Endpoint Tests

        [Fact]
        public async Task UploadOpenCycOwl_WithValidOwl_ShouldReturnSuccess()
        {
            // Arrange
            var owlPayload = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
         xmlns:rdfs=""http://www.w3.org/2000/01/rdf-schema#""
         xmlns:owl=""http://www.w3.org/2002/07/owl#"">
    <owl:Class rdf:about=""http://sw.opencyc.org/concept/Cx-Dog"">
        <rdfs:label>Dog</rdfs:label>
        <rdfs:comment>A domesticated animal</rdfs:comment>
    </owl:Class>
</rdf:RDF>";

            var request = new UploadOwlRequest
            {
                Payload = owlPayload,
                ResourceId = "opencyc_test.owl",
                SingularityId = 1,
                UserId = 1
            };

            var expectedResult = new ContentParseResult
            {
                ResourcePointId = 100,
                ParserType = "owl",
                BlockPointIds = new List<long> { 101 },
                FragmentPointIds = new List<long> { 102, 103 }
            };

            _contentParserServiceMock
                .Setup(x => x.ParseAndStoreAsync(
                    request.Payload,
                    request.ResourceId,
                    "owl", // Should force owl content type
                    request.SingularityId,
                    request.UserId,
                    request.Metadata))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.UploadOpenCycOwl(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();

            _contentParserServiceMock.Verify(x => x.ParseAndStoreAsync(
                request.Payload,
                request.ResourceId,
                "owl", // Verify owl content type was forced
                request.SingularityId,
                request.UserId,
                request.Metadata), Times.Once);
        }

        [Fact]
        public async Task UploadOpenCycOwl_WithEmptyPayload_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new UploadOwlRequest
            {
                Payload = "",
                ResourceId = "empty.owl"
            };

            // Act
            var result = await _controller.UploadOpenCycOwl(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var response = badRequestResult?.Value as ApiResponse<ContentParseResult>;
            response?.Message.Should().Contain("payload is required");
        }

        [Fact]
        public async Task UploadOpenCycOwl_WithEmptyResourceId_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new UploadOwlRequest
            {
                Payload = "<rdf:RDF></rdf:RDF>",
                ResourceId = ""
            };

            // Act
            var result = await _controller.UploadOpenCycOwl(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var response = badRequestResult?.Value as ApiResponse<ContentParseResult>;
            response?.Message.Should().Contain("ResourceId is required");
        }

        [Fact]
        public async Task UploadOpenCycOwl_WhenParsingFails_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new UploadOwlRequest
            {
                Payload = "Invalid OWL content",
                ResourceId = "invalid.owl"
            };

            _contentParserServiceMock
                .Setup(x => x.ParseAndStoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    "owl",
                    It.IsAny<long?>(),
                    It.IsAny<int?>(),
                    It.IsAny<Dictionary<string, object>?>()))
                .ReturnsAsync((ContentParseResult?)null);

            // Act
            var result = await _controller.UploadOpenCycOwl(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var response = badRequestResult?.Value as ApiResponse<ContentParseResult>;
            response?.Message.Should().Contain("Failed to parse");
        }

        [Fact]
        public async Task UploadOpenCycOwl_WithComplexOntology_ShouldParseSuccessfully()
        {
            // Arrange
            var complexOwl = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<rdf:RDF xml:base=""http://sw.opencyc.org/concept/""
    xmlns:owl=""http://www.w3.org/2002/07/owl#""
    xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""
    xmlns:rdfs=""http://www.w3.org/2000/01/rdf-schema#""
    xmlns:skos=""http://www.w3.org/2004/02/skos/core#"">

    <owl:Ontology rdf:about=""http://sw.opencyc.org/2012/05/10/concept"">
        <rdfs:label>OpenCyc Ontology</rdfs:label>
    </owl:Ontology>

    <owl:Class rdf:about=""http://sw.opencyc.org/concept/Cx-Animal"">
        <rdfs:label>Animal</rdfs:label>
    </owl:Class>

    <owl:ObjectProperty rdf:about=""http://sw.opencyc.org/concept/Cx-hasColor"">
        <rdfs:label>has color</rdfs:label>
    </owl:ObjectProperty>
</rdf:RDF>";

            var request = new UploadOwlRequest
            {
                Payload = complexOwl,
                ResourceId = "opencyc_complex.owl",
                SingularityId = 1,
                Metadata = new Dictionary<string, object>
                {
                    ["source"] = "opencyc",
                    ["version"] = "4.0"
                }
            };

            var expectedResult = new ContentParseResult
            {
                ResourcePointId = 200,
                ParserType = "owl",
                BlockPointIds = new List<long> { 201 },
                FragmentPointIds = new List<long> { 202, 203, 204 } // ontology, class, property
            };

            _contentParserServiceMock
                .Setup(x => x.ParseAndStoreAsync(
                    request.Payload,
                    request.ResourceId,
                    "owl",
                    request.SingularityId,
                    request.UserId,
                    request.Metadata))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.UploadOpenCycOwl(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _contentParserServiceMock.Verify(x => x.ParseAndStoreAsync(
                request.Payload,
                request.ResourceId,
                "owl",
                request.SingularityId,
                request.UserId,
                request.Metadata), Times.Once);
        }

        #endregion
    }
}
