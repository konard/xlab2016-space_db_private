using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceDb.Services;
using Xunit;

namespace SpaceDb.Tests.Services
{
    public class LinksServiceTests : IDisposable
    {
        private readonly LinksService _linksService;
        private readonly string _testDbPath;
        private readonly Mock<ILogger<LinksService>> _mockLogger;

        public LinksServiceTests()
        {
            _mockLogger = new Mock<ILogger<LinksService>>();
            _testDbPath = Path.Combine(Path.GetTempPath(), $"linksdb_test_{Guid.NewGuid()}");
            _linksService = new LinksService(_testDbPath, _mockLogger.Object);
        }

        public void Dispose()
        {
            _linksService?.Dispose();

            // Clean up test database
            try
            {
                if (Directory.Exists(_testDbPath))
                {
                    Directory.Delete(_testDbPath, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        [Fact]
        public async Task CreateLinkAsync_ShouldCreateLinkSuccessfully()
        {
            // Arrange
            ulong source = 100;
            ulong target = 200;

            // Act
            var linkId = await _linksService.CreateLinkAsync(source, target);

            // Assert
            linkId.Should().BeGreaterThan(0);

            var retrievedLink = await _linksService.GetLinkAsync(linkId);
            retrievedLink.Should().NotBeNull();
            retrievedLink.Value.source.Should().Be(source);
            retrievedLink.Value.target.Should().Be(target);
        }

        [Fact]
        public async Task GetLinkAsync_NonExistentLink_ShouldReturnNull()
        {
            // Arrange
            ulong nonExistentLinkId = 999999;

            // Act
            var result = await _linksService.GetLinkAsync(nonExistentLinkId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateLinkAsync_ShouldUpdateLinkSuccessfully()
        {
            // Arrange
            var linkId = await _linksService.CreateLinkAsync(100, 200);
            ulong newSource = 300;
            ulong newTarget = 400;

            // Act
            var updatedId = await _linksService.UpdateLinkAsync(linkId, newSource, newTarget);

            // Assert
            var retrievedLink = await _linksService.GetLinkAsync(updatedId);
            retrievedLink.Should().NotBeNull();
            retrievedLink.Value.source.Should().Be(newSource);
            retrievedLink.Value.target.Should().Be(newTarget);
        }

        [Fact]
        public async Task DeleteLinkAsync_ShouldDeleteLinkSuccessfully()
        {
            // Arrange
            var linkId = await _linksService.CreateLinkAsync(100, 200);

            // Act
            await _linksService.DeleteLinkAsync(linkId);

            // Assert
            var retrievedLink = await _linksService.GetLinkAsync(linkId);
            retrievedLink.Should().BeNull();
        }

        [Fact]
        public async Task SearchBySourceAsync_ShouldReturnMatchingLinks()
        {
            // Arrange
            ulong source = 100;
            await _linksService.CreateLinkAsync(source, 200);
            await _linksService.CreateLinkAsync(source, 300);
            await _linksService.CreateLinkAsync(999, 400); // Different source

            // Act
            var results = await _linksService.SearchBySourceAsync(source);

            // Assert
            results.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task SearchByTargetAsync_ShouldReturnMatchingLinks()
        {
            // Arrange
            ulong target = 200;
            await _linksService.CreateLinkAsync(100, target);
            await _linksService.CreateLinkAsync(300, target);
            await _linksService.CreateLinkAsync(400, 999); // Different target

            // Act
            var results = await _linksService.SearchByTargetAsync(target);

            // Assert
            results.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task SearchBySourceAndTargetAsync_ShouldReturnMatchingLinks()
        {
            // Arrange
            ulong source = 100;
            ulong target = 200;
            await _linksService.CreateLinkAsync(source, target);
            // Note: Platform.Data.Doublets may consolidate duplicate links (source, target)
            // so we create a different combination that still matches our search
            await _linksService.CreateLinkAsync(source, 999); // Different target
            await _linksService.CreateLinkAsync(999, target); // Different source

            // Act
            var results = await _linksService.SearchBySourceAndTargetAsync(source, target);

            // Assert
            // Should find at least one matching link
            results.Should().HaveCountGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task CountLinksAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            var initialCount = await _linksService.CountLinksAsync();

            await _linksService.CreateLinkAsync(100, 200);
            await _linksService.CreateLinkAsync(300, 400);

            // Act
            var finalCount = await _linksService.CountLinksAsync();

            // Assert
            finalCount.Should().Be(initialCount + 2);
        }

        [Fact]
        public async Task StoreResourceHierarchyAsync_ShouldCreateCompleteHierarchy()
        {
            // Arrange
            long resourceId = 1000;
            var blockIds = new List<long> { 2000, 2001 };
            var fragmentIdsByBlock = new Dictionary<long, IEnumerable<long>>
            {
                { 2000, new List<long> { 3000, 3001, 3002 } },
                { 2001, new List<long> { 3003, 3004 } }
            };

            // Act
            var resourceLinkId = await _linksService.StoreResourceHierarchyAsync(
                resourceId,
                blockIds,
                fragmentIdsByBlock);

            // Assert
            resourceLinkId.Should().BeGreaterThan(0);

            // Verify we can retrieve the hierarchy
            var hierarchy = await _linksService.GetResourceHierarchyAsync(resourceLinkId);
            hierarchy.Should().NotBeNull();
            hierarchy!.ResourceId.Should().Be(resourceId);
            hierarchy.Blocks.Should().HaveCount(2);

            // Verify blocks and fragments
            var block1 = hierarchy.Blocks.FirstOrDefault(b => b.BlockId == 2000);
            block1.Should().NotBeNull();
            block1!.FragmentIds.Should().HaveCount(3);
            block1.FragmentIds.Should().Contain(new[] { 3000L, 3001L, 3002L });

            var block2 = hierarchy.Blocks.FirstOrDefault(b => b.BlockId == 2001);
            block2.Should().NotBeNull();
            block2!.FragmentIds.Should().HaveCount(2);
            block2.FragmentIds.Should().Contain(new[] { 3003L, 3004L });
        }

        [Fact]
        public async Task StoreResourceHierarchyAsync_EmptyBlocks_ShouldStoreResourceOnly()
        {
            // Arrange
            long resourceId = 1000;
            var blockIds = new List<long>();
            var fragmentIdsByBlock = new Dictionary<long, IEnumerable<long>>();

            // Act
            var resourceLinkId = await _linksService.StoreResourceHierarchyAsync(
                resourceId,
                blockIds,
                fragmentIdsByBlock);

            // Assert
            resourceLinkId.Should().BeGreaterThan(0);

            var hierarchy = await _linksService.GetResourceHierarchyAsync(resourceLinkId);
            hierarchy.Should().NotBeNull();
            hierarchy!.ResourceId.Should().Be(resourceId);
            hierarchy.Blocks.Should().BeEmpty();
        }

        [Fact]
        public async Task StoreResourceHierarchyAsync_BlocksWithoutFragments_ShouldStoreCorrectly()
        {
            // Arrange
            long resourceId = 1000;
            var blockIds = new List<long> { 2000, 2001 };
            var fragmentIdsByBlock = new Dictionary<long, IEnumerable<long>>();

            // Act
            var resourceLinkId = await _linksService.StoreResourceHierarchyAsync(
                resourceId,
                blockIds,
                fragmentIdsByBlock);

            // Assert
            resourceLinkId.Should().BeGreaterThan(0);

            var hierarchy = await _linksService.GetResourceHierarchyAsync(resourceLinkId);
            hierarchy.Should().NotBeNull();
            hierarchy!.Blocks.Should().HaveCount(2);

            foreach (var block in hierarchy.Blocks)
            {
                block.FragmentIds.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task GetResourceHierarchyAsync_NonExistentResource_ShouldReturnNull()
        {
            // Arrange
            ulong nonExistentLinkId = 999999;

            // Act
            var hierarchy = await _linksService.GetResourceHierarchyAsync(nonExistentLinkId);

            // Assert
            hierarchy.Should().BeNull();
        }

        [Fact]
        public async Task StoreResourceHierarchyAsync_MultipleResources_ShouldStoreIndependently()
        {
            // Arrange
            long resourceId1 = 1000;
            var blockIds1 = new List<long> { 2000 };
            var fragmentIds1 = new Dictionary<long, IEnumerable<long>>
            {
                { 2000, new List<long> { 3000, 3001 } }
            };

            long resourceId2 = 1001;
            var blockIds2 = new List<long> { 2100 };
            var fragmentIds2 = new Dictionary<long, IEnumerable<long>>
            {
                { 2100, new List<long> { 3100, 3101, 3102 } }
            };

            // Act
            var linkId1 = await _linksService.StoreResourceHierarchyAsync(resourceId1, blockIds1, fragmentIds1);
            var linkId2 = await _linksService.StoreResourceHierarchyAsync(resourceId2, blockIds2, fragmentIds2);

            // Assert
            linkId1.Should().NotBe(linkId2);

            var hierarchy1 = await _linksService.GetResourceHierarchyAsync(linkId1);
            var hierarchy2 = await _linksService.GetResourceHierarchyAsync(linkId2);

            hierarchy1.Should().NotBeNull();
            hierarchy1!.ResourceId.Should().Be(resourceId1);
            hierarchy1.Blocks.Should().HaveCount(1);
            hierarchy1.Blocks[0].FragmentIds.Should().HaveCount(2);

            hierarchy2.Should().NotBeNull();
            hierarchy2!.ResourceId.Should().Be(resourceId2);
            hierarchy2.Blocks.Should().HaveCount(1);
            hierarchy2.Blocks[0].FragmentIds.Should().HaveCount(3);
        }
    }
}
