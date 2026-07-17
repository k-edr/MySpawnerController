using System;
using System.IO;
using GridSpawner.Api.Application;
using NUnit.Framework;

namespace GridSpawner.Tests
{
    [TestFixture]
    public class SpawnOrchestratorTests
    {
        private string _tempRoot;
        private string _blueprintsFolder;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), $"GS_Test_{Guid.NewGuid():N}");
            _blueprintsFolder = Path.Combine(_tempRoot, "blueprints");
            Directory.CreateDirectory(_blueprintsFolder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }

        [Test]
        public void ResolveBlueprintPath_NormalName_ResolvesCorrectly()
        {
            var bpDir = Path.Combine(_blueprintsFolder, "TestGrid");
            Directory.CreateDirectory(bpDir);
            var result = SpawnOrchestrator.ResolveBlueprintPath(_blueprintsFolder, "TestGrid");
            var expected = Path.GetFullPath(Path.Combine(_blueprintsFolder, "TestGrid", "bp.sbc"));
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ResolveBlueprintPath_PathTraversal_ThrowsSecurityException()
        {
            Assert.Throws<System.Security.SecurityException>(() =>
            {
                SpawnOrchestrator.ResolveBlueprintPath(_blueprintsFolder, "..\\..\\temp\\evil");
            });
        }

        [Test]
        public void ResolveBlueprintPath_DoubleDotSlash_ThrowsSecurityException()
        {
            Assert.Throws<System.Security.SecurityException>(() =>
            {
                SpawnOrchestrator.ResolveBlueprintPath(_blueprintsFolder, "../evil");
            });
        }

        [Test]
        public void ResolveBlueprintPath_PathTraversalMixed_ThrowsSecurityException()
        {
            Assert.Throws<System.Security.SecurityException>(() =>
            {
                SpawnOrchestrator.ResolveBlueprintPath(_blueprintsFolder, "subdir/../../evil");
            });
        }
    }
}
