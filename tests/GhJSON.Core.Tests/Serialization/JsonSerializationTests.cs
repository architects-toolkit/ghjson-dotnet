/*
 * GhJSON - JSON format for Grasshopper definitions
 * Copyright (C) 2024-2026 Marc Roca Musach
 */

using System.IO;
using GhJSON.Core;
using GhJSON.Core.SchemaModels;
using GhJSON.Core.Serialization;
using Xunit;

namespace GhJSON.Core.Tests.Serialization
{
    public class JsonSerializationTests
    {
        [Fact]
        public void ToJson_WithIndentedOption_FormatsJson()
        {
            var doc = GhJson.CreateDocument();
            doc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 1 });

            var options = new WriteOptions { Indented = true };
            var json = GhJson.ToJson(doc, options);

            Assert.Contains("\n", json);
            Assert.Contains("  ", json);
        }

        [Fact]
        public void ToJson_WithoutIndentation_ProducesCompactJson()
        {
            var doc = GhJson.CreateDocument();
            doc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 1 });

            var options = new WriteOptions { Indented = false };
            var json = GhJson.ToJson(doc, options);

            Assert.DoesNotContain("\n  ", json);
        }

        [Fact]
        public void ToFile_WritesJsonToFile()
        {
            var doc = GhJson.CreateDocument();
            doc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 1 });

            var tempPath = Path.GetTempFileName();
            try
            {
                GhJson.ToFile(doc, tempPath);

                Assert.True(File.Exists(tempPath));
                var content = File.ReadAllText(tempPath);
                Assert.Contains("Addition", content);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Fact]
        public void FromFile_ReadsJsonFromFile()
        {
            var doc = GhJson.CreateDocument();
            doc.Components.Add(new GhJsonComponent { Name = "Addition", Id = 1 });

            var tempPath = Path.GetTempFileName();
            try
            {
                GhJson.ToFile(doc, tempPath);
                var loadedDoc = GhJson.FromFile(tempPath);

                Assert.NotNull(loadedDoc);
                Assert.Single(loadedDoc.Components);
                Assert.Equal("Addition", loadedDoc.Components[0].Name);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Fact]
        public void FromStream_ReadsJsonFromStream()
        {
            var json = @"{""schema"":""1.0"",""components"":[{""name"":""Addition"",""id"":1}]}";
            
            using var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, leaveOpen: true))
            {
                writer.Write(json);
            }
            
            stream.Position = 0;
            var doc = GhJson.FromStream(stream);

            Assert.NotNull(doc);
            Assert.Single(doc.Components);
            Assert.Equal("Addition", doc.Components[0].Name);
        }

        [Fact]
        public void RoundTrip_PreservesData()
        {
            var doc = GhJson.CreateDocument();
            doc.Components.Add(new GhJsonComponent 
            { 
                Name = "Addition", 
                Id = 1,
                NickName = "Add",
                Library = "Maths",
                Pivot = new GhJsonPivot { X = 100, Y = 200 }
            });

            var json = GhJson.ToJson(doc);
            var loadedDoc = GhJson.FromJson(json);

            Assert.Equal(doc.Components[0].Name, loadedDoc.Components[0].Name);
            Assert.Equal(doc.Components[0].Id, loadedDoc.Components[0].Id);
            Assert.Equal(doc.Components[0].NickName, loadedDoc.Components[0].NickName);
            Assert.Equal(doc.Components[0].Library, loadedDoc.Components[0].Library);
            Assert.Equal(doc.Components[0].Pivot.X, loadedDoc.Components[0].Pivot.X);
            Assert.Equal(doc.Components[0].Pivot.Y, loadedDoc.Components[0].Pivot.Y);
        }

        [Fact]
        public void PivotConverter_HandlesStringFormat()
        {
            var json = @"{""schema"":""1.0"",""components"":[{""name"":""Addition"",""id"":1,""pivot"":""100.5,200.5""}]}";
            var doc = GhJson.FromJson(json);

            Assert.NotNull(doc.Components[0].Pivot);
            Assert.Equal(100.5, doc.Components[0].Pivot.X);
            Assert.Equal(200.5, doc.Components[0].Pivot.Y);
        }

        [Fact]
        public void PivotConverter_HandlesObjectFormat()
        {
            var json = @"{""schema"":""1.0"",""components"":[{""name"":""Addition"",""id"":1,""pivot"":{""x"":100.5,""y"":200.5}}]}";
            var doc = GhJson.FromJson(json);

            Assert.NotNull(doc.Components[0].Pivot);
            Assert.Equal(100.5, doc.Components[0].Pivot.X);
            Assert.Equal(200.5, doc.Components[0].Pivot.Y);
        }
    }
}
