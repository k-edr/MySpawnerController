using GridSpawner.Api.Application;
using NUnit.Framework;

namespace GridSpawner.Tests
{
    [TestFixture]
    public class SpawnResultTests
    {
        [Test]
        public void Ok_ReturnsStatusCode200()
        {
            var result = SpawnResult.Ok(new { test = 1 });
            Assert.That(result.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public void Ok_BodyIsProvidedObject()
        {
            var body = new { test = 42 };
            var result = SpawnResult.Ok(body);
            Assert.That(result.Body, Is.SameAs(body));
        }

        [Test]
        public void BadRequest_ReturnsStatusCode400()
        {
            var result = SpawnResult.BadRequest("something wrong");
            Assert.That(result.StatusCode, Is.EqualTo(400));
        }

        [Test]
        public void BadRequest_BodyHasErrorMessage()
        {
            var result = SpawnResult.BadRequest("field X is required");
            Assert.That(result.Body, Is.Not.Null);
            Assert.That(
                result.Body.GetType().GetProperty("Error")?.GetValue(result.Body),
                Is.EqualTo("field X is required"));
        }

        [Test]
        public void NotFound_ReturnsStatusCode404()
        {
            var result = SpawnResult.NotFound("grid not found");
            Assert.That(result.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public void NotFound_BodyHasErrorMessage()
        {
            var result = SpawnResult.NotFound("missing blueprint");
            Assert.That(
                result.Body.GetType().GetProperty("Error")?.GetValue(result.Body),
                Is.EqualTo("missing blueprint"));
        }

        [Test]
        public void NotReady_ReturnsStatusCode503()
        {
            var result = SpawnResult.NotReady();
            Assert.That(result.StatusCode, Is.EqualTo(503));
        }

        [Test]
        public void NotReady_BodyHasSessionNotReadyMessage()
        {
            var result = SpawnResult.NotReady();
            Assert.That(
                result.Body.GetType().GetProperty("Error")?.GetValue(result.Body),
                Is.EqualTo("Session not ready"));
        }

        [Test]
        public void Error_ReturnsStatusCode500()
        {
            var result = SpawnResult.Error("internal failure");
            Assert.That(result.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public void Error_BodyHasProvidedMessage()
        {
            var result = SpawnResult.Error("disk full");
            Assert.That(
                result.Body.GetType().GetProperty("Error")?.GetValue(result.Body),
                Is.EqualTo("disk full"));
        }
    }
}
