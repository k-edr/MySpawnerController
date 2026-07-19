using System;
using System.IO;
using GridSpawner.Api.Application;
using GridSpawner.Shared.Models;
using NUnit.Framework;

namespace GridSpawner.Tests
{
    [TestFixture]
    public class SpawnFromJsonTests
    {
        private string _tempRoot;
        private string _blueprintsFolder;
        private FakeSpawnService _spawnService;
        private SpawnOrchestrator _orchestrator;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), $"GS_SFJ_{Guid.NewGuid():N}");
            _blueprintsFolder = Path.Combine(_tempRoot, "blueprints");
            Directory.CreateDirectory(_blueprintsFolder);
            _spawnService = new FakeSpawnService();
            _orchestrator = new SpawnOrchestrator(_spawnService, _blueprintsFolder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }

        // ── P1: Missing / invalid blueprint ──

        [Test]
        public void SpawnFromJson_MissingBlueprintField_Returns400()
        {
            var result = _orchestrator.SpawnFromJson("{}");
            Assert.That(result.StatusCode, Is.EqualTo(400));
        }

        [Test]
        public void SpawnFromJson_BlueprintIsWhitespace_Returns400()
        {
            var result = _orchestrator.SpawnFromJson(@"{ ""blueprint"": ""   "" }");
            Assert.That(result.StatusCode, Is.EqualTo(400));
        }

        [Test]
        public void SpawnFromJson_MalformedJson_Returns400()
        {
            var result = _orchestrator.SpawnFromJson("not valid json {");
            Assert.That(result.StatusCode, Is.EqualTo(400));
        }

        // ── P1: Service readiness ──

        [Test]
        public void SpawnFromJson_ServiceNotReady_Returns503()
        {
            _spawnService.Ready = false;

            var result = _orchestrator.SpawnFromJson(@"{ ""blueprint"": ""TestGrid"" }");
            Assert.That(result.StatusCode, Is.EqualTo(503));
        }

        // ── P1: Blueprint file existence ──

        [Test]
        public void SpawnFromJson_BlueprintFileNotFound_Returns404()
        {
            _spawnService.Ready = true;
            // Blueprints folder exists but has no TestGrid subdirectory

            var result = _orchestrator.SpawnFromJson(@"{ ""blueprint"": ""TestGrid"" }");
            Assert.That(result.StatusCode, Is.EqualTo(404));
        }

        // ── P1: Valid happy-path ──

        [Test]
        public void SpawnFromJson_ValidRequest_Returns200()
        {
            SetupBlueprint("TestGrid");
            _spawnService.Ready = true;

            var result = _orchestrator.SpawnFromJson(@"{ ""blueprint"": ""TestGrid"" }");
            Assert.That(result.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public void SpawnFromJson_ValidRequest_BodyIsSpawnResponse()
        {
            SetupBlueprint("TestGrid");
            _spawnService.Ready = true;

            var result = _orchestrator.SpawnFromJson(@"{ ""blueprint"": ""TestGrid"" }");
            Assert.That(result.Body, Is.InstanceOf<SpawnResponse>());
        }

        [Test]
        public void SpawnFromJson_ValidRequest_CallsSpawnWithCorrectBlueprintArgs()
        {
            SetupBlueprint("TestGrid");
            _spawnService.Ready = true;

            _orchestrator.SpawnFromJson(@"{ ""blueprint"": ""TestGrid"" }");

            Assert.That(_spawnService.LastBlueprintName, Is.EqualTo("TestGrid"));
            Assert.That(_spawnService.LastBlueprintPath,
                Does.EndWith(Path.Combine("TestGrid", "bp.sbc")));
        }

        [Test]
        public void SpawnFromJson_SpawnReturnsNull_Returns500()
        {
            SetupBlueprint("TestGrid");
            _spawnService.Ready = true;
            _spawnService.ReturnNull = true;

            var result = _orchestrator.SpawnFromJson(@"{ ""blueprint"": ""TestGrid"" }");
            Assert.That(result.StatusCode, Is.EqualTo(500));
        }

        // ── P1: Position / displayName ──

        [Test]
        public void SpawnFromJson_PositionOmitted_UsesZeroVector()
        {
            SetupBlueprint("TestGrid");
            _spawnService.Ready = true;

            _orchestrator.SpawnFromJson(@"{ ""blueprint"": ""TestGrid"" }");

            Assert.That(_spawnService.LastX, Is.EqualTo(0));
            Assert.That(_spawnService.LastY, Is.EqualTo(0));
            Assert.That(_spawnService.LastZ, Is.EqualTo(0));
        }

        [Test]
        public void SpawnFromJson_PositionProvided_PassedToSpawn()
        {
            SetupBlueprint("TestGrid");
            _spawnService.Ready = true;

            _orchestrator.SpawnFromJson(
                @"{ ""blueprint"": ""TestGrid"", ""position"": { ""x"": 10, ""y"": 20, ""z"": -5 } }");

            Assert.That(_spawnService.LastX, Is.EqualTo(10));
            Assert.That(_spawnService.LastY, Is.EqualTo(20));
            Assert.That(_spawnService.LastZ, Is.EqualTo(-5));
        }

        [Test]
        public void SpawnFromJson_DisplayNameProvided_PassedToSpawn()
        {
            SetupBlueprint("TestGrid");
            _spawnService.Ready = true;

            _orchestrator.SpawnFromJson(
                @"{ ""blueprint"": ""TestGrid"", ""displayName"": ""MyCustomGrid"" }");

            Assert.That(_spawnService.LastDisplayName, Is.EqualTo("MyCustomGrid"));
        }

        // ── P1: Path traversal ──

        [Test]
        public void SpawnFromJson_PathTraversal_Returns400()
        {
            _spawnService.Ready = true;

            var result = _orchestrator.SpawnFromJson(
                @"{ ""blueprint"": ""../../evil"" }");
            Assert.That(result.StatusCode, Is.EqualTo(400));
        }

        [Test]
        public void SpawnFromJson_SubdirPathTraversal_Returns400()
        {
            // Tests the trailing-separator fix: "BlueprintsEvil" should not match "Blueprints"
            var evilDir = Path.Combine(
                Path.GetDirectoryName(_blueprintsFolder.TrimEnd(Path.DirectorySeparatorChar)),
                Path.GetFileName(_blueprintsFolder) + "Evil");
            Directory.CreateDirectory(evilDir);
            SetupBlueprint("sub", evilDir);
            _spawnService.Ready = true;

            var result = _orchestrator.SpawnFromJson(
                $@"{{ ""blueprint"": ""..\\{Path.GetFileName(evilDir)}\\sub"" }}");
            Assert.That(result.StatusCode, Is.EqualTo(400));
        }

        // ── Helpers ──

        private void SetupBlueprint(string name, string baseFolder = null)
        {
            var dir = Path.Combine(baseFolder ?? _blueprintsFolder, name);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "bp.sbc"), "<Definitions />");
        }
    }
}
