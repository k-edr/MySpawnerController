using System.Text.Json;
using GridSpawner.Shared.Models;
using NUnit.Framework;

namespace GridSpawner.Tests
{
    [TestFixture]
    public class GridDtoSerializationTests
    {
        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        [Test]
        public void Deserialize_FullJsonWithForwardAndUp_AllFieldsPopulated()
        {
            const string json = @"
            {
                ""id"": 123,
                ""name"": ""TestGrid"",
                ""position"": { ""x"": 100, ""y"": 200, ""z"": 300 },
                ""velocity"": { ""x"": 50, ""y"": 0, ""z"": -10 },
                ""forward"": { ""x"": 0.7, ""y"": 0.2, ""z"": 0.7 },
                ""up"": { ""x"": 0, ""y"": 1, ""z"": 0 }
            }";

            var dto = JsonSerializer.Deserialize<GridDto>(json, JsonOpts);

            Assert.That(dto, Is.Not.Null);
            Assert.That(dto.Id, Is.EqualTo(123));
            Assert.That(dto.Name, Is.EqualTo("TestGrid"));
            Assert.That(dto.Position.X, Is.EqualTo(100));
            Assert.That(dto.Position.Y, Is.EqualTo(200));
            Assert.That(dto.Position.Z, Is.EqualTo(300));
            Assert.That(dto.Forward.X, Is.EqualTo(0.7));
            Assert.That(dto.Forward.Y, Is.EqualTo(0.2));
            Assert.That(dto.Forward.Z, Is.EqualTo(0.7));
            Assert.That(dto.Up.X, Is.EqualTo(0));
            Assert.That(dto.Up.Y, Is.EqualTo(1));
            Assert.That(dto.Up.Z, Is.EqualTo(0));
        }

        [Test]
        public void Deserialize_ForwardAndUpOmitted_FieldsAreNull()
        {
            const string json = @"
            {
                ""id"": 1,
                ""name"": ""Grid"",
                ""position"": { ""x"": 0, ""y"": 0, ""z"": 0 }
            }";

            var dto = JsonSerializer.Deserialize<GridDto>(json, JsonOpts);

            Assert.That(dto, Is.Not.Null);
            Assert.That(dto.Forward, Is.Null);
            Assert.That(dto.Up, Is.Null);
        }

        [Test]
        public void Serialize_ForwardAndUpSet_FieldsAppearInJson()
        {
            var dto = new GridDto
            {
                Id = 42,
                Name = "OrientedGrid",
                Position = new Vector3Dto { X = 10, Y = 20, Z = 30 },
                Velocity = new Vector3Dto { X = 1, Y = 2, Z = 3 },
                Forward = new Vector3Dto { X = 0.866, Y = 0, Z = -0.5 },
                Up = new Vector3Dto { X = 0, Y = 1, Z = 0 }
            };

            var json = JsonSerializer.Serialize(dto, JsonOpts);

            Assert.That(json, Does.Contain("\"forward\""));
            Assert.That(json, Does.Contain("\"up\""));
            Assert.That(json, Does.Contain("\"forward\":{\"x\":0.865"));
            Assert.That(json, Does.Contain("-0.5"));
        }

        [Test]
        public void Serialize_ForwardAndUpNotSet_FieldsSerializedAsNull()
        {
            var dto = new GridDto
            {
                Id = 1,
                Name = "Grid",
                Position = new Vector3Dto { X = 0, Y = 0, Z = 0 }
            };

            var json = JsonSerializer.Serialize(dto, JsonOpts);

            Assert.That(json, Does.Contain("\"forward\":null"));
            Assert.That(json, Does.Contain("\"up\":null"));
        }

        [Test]
        public void RoundTrip_ForwardAndUp_SurviveSerializeDeserialize()
        {
            var original = new GridDto
            {
                Id = 7,
                Name = "RoundTrip",
                Position = new Vector3Dto { X = 1, Y = 2, Z = 3 },
                Forward = new Vector3Dto { X = 0.707, Y = 0, Z = 0.707 },
                Up = new Vector3Dto { X = 0, Y = 1, Z = 0 }
            };

            var json = JsonSerializer.Serialize(original, JsonOpts);
            var deserialized = JsonSerializer.Deserialize<GridDto>(json, JsonOpts);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Forward.X, Is.EqualTo(0.707));
            Assert.That(deserialized.Forward.Y, Is.EqualTo(0));
            Assert.That(deserialized.Forward.Z, Is.EqualTo(0.707));
            Assert.That(deserialized.Up.X, Is.EqualTo(0));
            Assert.That(deserialized.Up.Y, Is.EqualTo(1));
            Assert.That(deserialized.Up.Z, Is.EqualTo(0));
        }
    }
}
