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
        public void Deserialize_FullJsonWithAllDirections_AllFieldsPopulated()
        {
            const string json = @"
            {
                ""id"": 123,
                ""name"": ""TestGrid"",
                ""position"": { ""x"": 100, ""y"": 200, ""z"": 300 },
                ""velocity"": { ""x"": 50, ""y"": 0, ""z"": -10 },
                ""forward"":  { ""x"": 1, ""y"": 0, ""z"": 0 },
                ""backward"": { ""x"": -1, ""y"": 0, ""z"": 0 },
                ""up"":       { ""x"": 0, ""y"": 1, ""z"": 0 },
                ""down"":     { ""x"": 0, ""y"": -1, ""z"": 0 },
                ""left"":     { ""x"": 0, ""y"": 0, ""z"": -1 },
                ""right"":    { ""x"": 0, ""y"": 0, ""z"": 1 }
            }";

            var dto = JsonSerializer.Deserialize<GridDto>(json, JsonOpts);

            Assert.That(dto, Is.Not.Null);
            Assert.That(dto.Id, Is.EqualTo(123));
            Assert.That(dto.Name, Is.EqualTo("TestGrid"));
            Assert.That(dto.Forward.X, Is.EqualTo(1));
            Assert.That(dto.Forward.Y, Is.EqualTo(0));
            Assert.That(dto.Forward.Z, Is.EqualTo(0));
            Assert.That(dto.Backward.X, Is.EqualTo(-1));
            Assert.That(dto.Backward.Y, Is.EqualTo(0));
            Assert.That(dto.Backward.Z, Is.EqualTo(0));
            Assert.That(dto.Up.X, Is.EqualTo(0));
            Assert.That(dto.Up.Y, Is.EqualTo(1));
            Assert.That(dto.Up.Z, Is.EqualTo(0));
            Assert.That(dto.Down.X, Is.EqualTo(0));
            Assert.That(dto.Down.Y, Is.EqualTo(-1));
            Assert.That(dto.Down.Z, Is.EqualTo(0));
            Assert.That(dto.Left.X, Is.EqualTo(0));
            Assert.That(dto.Left.Y, Is.EqualTo(0));
            Assert.That(dto.Left.Z, Is.EqualTo(-1));
            Assert.That(dto.Right.X, Is.EqualTo(0));
            Assert.That(dto.Right.Y, Is.EqualTo(0));
            Assert.That(dto.Right.Z, Is.EqualTo(1));
        }

        [Test]
        public void Deserialize_DirectionsOmitted_FieldsAreNull()
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
            Assert.That(dto.Backward, Is.Null);
            Assert.That(dto.Up, Is.Null);
            Assert.That(dto.Down, Is.Null);
            Assert.That(dto.Left, Is.Null);
            Assert.That(dto.Right, Is.Null);
        }

        [Test]
        public void Serialize_AllDirectionsSet_AppearInJson()
        {
            var dto = new GridDto
            {
                Id = 42,
                Name = "OrientedGrid",
                Position = new Vector3Dto { X = 0, Y = 0, Z = 0 },
                Forward = new Vector3Dto { X = 0, Y = 0, Z = 1 },
                Backward = new Vector3Dto { X = 0, Y = 0, Z = -1 },
                Up = new Vector3Dto { X = 0, Y = 1, Z = 0 },
                Down = new Vector3Dto { X = 0, Y = -1, Z = 0 },
                Left = new Vector3Dto { X = -1, Y = 0, Z = 0 },
                Right = new Vector3Dto { X = 1, Y = 0, Z = 0 }
            };

            var json = JsonSerializer.Serialize(dto, JsonOpts);

            Assert.That(json, Does.Contain("\"forward\""));
            Assert.That(json, Does.Contain("\"backward\""));
            Assert.That(json, Does.Contain("\"up\""));
            Assert.That(json, Does.Contain("\"down\""));
            Assert.That(json, Does.Contain("\"left\""));
            Assert.That(json, Does.Contain("\"right\""));
        }

        [Test]
        public void Serialize_DirectionsNotSet_SerializedAsNull()
        {
            var dto = new GridDto
            {
                Id = 1,
                Name = "Grid",
                Position = new Vector3Dto { X = 0, Y = 0, Z = 0 }
            };

            var json = JsonSerializer.Serialize(dto, JsonOpts);

            Assert.That(json, Does.Contain("\"forward\":null"));
            Assert.That(json, Does.Contain("\"backward\":null"));
            Assert.That(json, Does.Contain("\"up\":null"));
            Assert.That(json, Does.Contain("\"down\":null"));
            Assert.That(json, Does.Contain("\"left\":null"));
            Assert.That(json, Does.Contain("\"right\":null"));
        }

        [Test]
        public void RoundTrip_AllDirections_SurviveSerializeDeserialize()
        {
            var original = new GridDto
            {
                Id = 7,
                Name = "RoundTrip",
                Position = new Vector3Dto { X = 1, Y = 2, Z = 3 },
                Forward = new Vector3Dto { X = 0, Y = 0, Z = 1 },
                Backward = new Vector3Dto { X = 0, Y = 0, Z = -1 },
                Up = new Vector3Dto { X = 0, Y = 1, Z = 0 },
                Down = new Vector3Dto { X = 0, Y = -1, Z = 0 },
                Left = new Vector3Dto { X = -1, Y = 0, Z = 0 },
                Right = new Vector3Dto { X = 1, Y = 0, Z = 0 }
            };

            var json = JsonSerializer.Serialize(original, JsonOpts);
            var deserialized = JsonSerializer.Deserialize<GridDto>(json, JsonOpts);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Forward.Z, Is.EqualTo(1));
            Assert.That(deserialized.Backward.Z, Is.EqualTo(-1));
            Assert.That(deserialized.Up.Y, Is.EqualTo(1));
            Assert.That(deserialized.Down.Y, Is.EqualTo(-1));
            Assert.That(deserialized.Left.X, Is.EqualTo(-1));
            Assert.That(deserialized.Right.X, Is.EqualTo(1));
        }
    }
}
