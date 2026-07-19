using NUnit.Framework;

namespace GridSpawner.Tests
{
    [TestFixture]
    public class BlockInfoExtractorTests
    {
        [Test]
        public void FatBlockInfoExtractor_GetName_RequiresGameEngine()
        {
            // FatBlockInfoExtractor.GetName() requires IMySlimBlock.FatBlock
            // which can only be created by the game engine.
            // Tested implicitly via integration tests (GetBlockStates → GridDtoMapper → extractors).
            Assert.Ignore("Tested via integration tests");
        }

        [Test]
        public void SlimBlockInfoExtractor_GetName_RequiresGameEngine()
        {
            // SlimBlockInfoExtractor.GetName() requires IMySlimBlock.BlockDefinition
            // which can only be created by the game engine.
            // Tested implicitly via integration tests (GetBlockStates → GridDtoMapper → extractors).
            Assert.Ignore("Tested via integration tests");
        }
    }
}
