using System.Text.Json;
using GridSpawner.Shared.Models;
using NUnit.Framework;

namespace GridSpawner.Tests
{
    [TestFixture]
    public class ScriptRunResultTests
    {
        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        [Test]
        public void Deserialize_FullJson_EchoOutputAndSuccessPopulated()
        {
            const string json = @"
            {
                ""echo"": ""FSM: Idle -> Launch"",
                ""output"": ""Target: X:100 Y:200 Z:300"",
                ""success"": true
            }";

            var result = JsonSerializer.Deserialize<ScriptRunResult>(json, JsonOpts);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Echo, Is.EqualTo("FSM: Idle -> Launch"));
            Assert.That(result.Output, Is.EqualTo("Target: X:100 Y:200 Z:300"));
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void Serialize_OutputAndEchoSet_BothFieldsInJson()
        {
            var result = new ScriptRunResult
            {
                Echo = "echo text",
                Output = "output text",
                Success = true
            };

            var json = JsonSerializer.Serialize(result, JsonOpts);

            Assert.That(json, Does.Contain("\"echo\""));
            Assert.That(json, Does.Contain("\"output\""));
            Assert.That(json, Does.Contain("\"echo text\""));
            Assert.That(json, Does.Contain("\"output text\""));
            Assert.That(json, Does.Contain("\"success\""));
        }

        [Test]
        public void Deserialize_OnlyEchoAndSuccess_OutputDefaultsToEmpty()
        {
            const string json = @"
            {
                ""echo"": ""legacy response"",
                ""success"": false
            }";

            var result = JsonSerializer.Deserialize<ScriptRunResult>(json, JsonOpts);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Echo, Is.EqualTo("legacy response"));
            Assert.That(result.Output, Is.EqualTo(""));
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void DefaultConstructor_HasEmptyEchoAndOutput()
        {
            var result = new ScriptRunResult();

            Assert.That(result.Echo, Is.EqualTo(""));
            Assert.That(result.Output, Is.EqualTo(""));
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void Serialize_OutputOnly_FieldInJson()
        {
            var result = new ScriptRunResult
            {
                Output = "PB echo captured"
            };

            var json = JsonSerializer.Serialize(result, JsonOpts);

            Assert.That(json, Does.Contain("\"output\""));
            Assert.That(json, Does.Contain("\"PB echo captured\""));
        }
    }
}
