﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
using Microsoft.Xna.Framework.Graphics;
using NUnit.Framework;
#if XNA
using System.Reflection;
#endif

namespace MonoGame.Tests.ContentPipeline
{
    // These tests are based on "Everything you ever wanted to know about IntermediateSerializer" by Shawn Hargreaves
    // http://blogs.msdn.com/b/shawnhar/archive/2008/08/12/everything-you-ever-wanted-to-know-about-intermediateserializer.aspx

    class IntermediateSerializerTest
    {
        class TestContentManager : ContentManager
        {
            class FakeGraphicsService : IGraphicsDeviceService
            {
                public GraphicsDevice GraphicsDevice { get; private set; }
                public event EventHandler<EventArgs> DeviceCreated;
                public event EventHandler<EventArgs> DeviceDisposing;
                public event EventHandler<EventArgs> DeviceReset;
                public event EventHandler<EventArgs> DeviceResetting;
            }

            class FakeServiceProvider : IServiceProvider
            {
                public object GetService(Type serviceType)
                {
                    if (serviceType == typeof(IGraphicsDeviceService))
                        return new FakeGraphicsService();

                    throw new NotImplementedException();
                }
            }

            private readonly MemoryStream _xnbStream;

            public TestContentManager(MemoryStream xnbStream)
                : base(new FakeServiceProvider(), "NONE")
            {
                _xnbStream = xnbStream;
            }

            protected override Stream OpenStream(string assetName)
            {
                return new MemoryStream(_xnbStream.GetBuffer(), false);
            }
        }

        private static void SerializeAndAssert<T>(string file, T value)
        {
            var filePath = Paths.Xml(file);
            var expectedXml = File.ReadAllText(filePath);

            var actualXml = Serialize(filePath, value);

            // Normalize line endings - git on build server seems to set
            // core.autocrlf to false.
            expectedXml = expectedXml.Replace(Environment.NewLine, "\n");
            actualXml = actualXml.Replace(Environment.NewLine, "\n");

            Assert.AreEqual(expectedXml, actualXml);
        }

        private static string Serialize<T>(string filePath, T value)
        {
            string referenceRelocationPath = filePath;

            // Note: Can't use StringBuilder here because it is always UTF-16,
            // while our test XML files use a UTF-8 encoding.
            var memoryStream = new MemoryStream();
            var xmlWriterSettings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true
            };
            using (var writer = XmlWriter.Create(memoryStream, xmlWriterSettings))
                IntermediateSerializer.Serialize(writer, value, referenceRelocationPath);

            memoryStream.Position = 0;
            var actualXml = new StreamReader(memoryStream).ReadToEnd();

            return actualXml;
        }

        [Test]
        public void TheBasics()
        {
            var nestedObject = new NestedClass
            {
                Name = "Shawn",
                IsEnglish = true
            };
            SerializeAndAssert("01_TheBasics.xml", new TheBasics
            {
                PublicField = 1,
                InternalField = 0,
                GetSetProperty = "Hello World",
                Nested = nestedObject,
                Nested2 = nestedObject
            });
        }

        [Test]
        public void Inheritance()
        {
            SerializeAndAssert("02_Inheritance.xml", new Inheritance
            {
                elf = 23,
                hello = "world"
            });
        }

        [Test]
        public void IncludingPrivateMembers()
        {
            SerializeAndAssert("03_IncludingPrivateMembers.xml", new IncludingPrivateMembers(23));
        }

        [Test]
        public void ExcludingPublicMembers()
        {
            SerializeAndAssert("04_ExcludingPublicMembersOutput.xml", new ExcludingPublicMembers
            {
                elf = 23
            });
        }

        [Test]
        public void RenamingXmlElements()
        {
            SerializeAndAssert("05_RenamingXmlElements.xml", new RenamingXmlElements
            {
                hello = "world",
                elf = 23
            });
        }

        [Test]
        public void NullReferences()
        {
            SerializeAndAssert("06_NullReferences.xml", new NullReferences
            {
                hello = null
            });
        }

        [Test]
        public void OptionalElements()
        {
            SerializeAndAssert("07_OptionalElements.xml", new OptionalElements
            {
                a = null,
                b = null,
                c = string.Empty
            });
        }

        [Test]
        public void AllowNull()
        {
            // [AllowNull] "has no effect when serializing to XML"
            // (http://blogs.msdn.com/b/shawnhar/archive/2008/08/12/everything-you-ever-wanted-to-know-about-intermediateserializer.aspx)

            // Except for... it does have an effect. XNA throws an exception when
            // you try to serialize a null element which has [AllowNull] specified.
            Assert.Throws<InvalidOperationException>(() =>
                Serialize("output", new AllowNull()));
        }

        [Test]
        public void Collections()
        {
            SerializeAndAssert("09_Collections.xml", new Collections
            {
                StringArray = new[] { "Hello", "World" },
                StringList = new List<string> { "This", "is", "a", "test" },
                IntArray = new[] { 1, 2, 3, 23, 42 }
            });
        }

        [Test]
        public void CollectionItemName()
        {
            SerializeAndAssert("10_CollectionItemName.xml", new CollectionItemName
            {
                StringArray = new[] { "Hello", "World" }
            });
        }

        [Test]
        public void Dictionaries()
        {
            SerializeAndAssert("11_Dictionaries.xml", new Dictionaries
            {
                TestDictionary = new Dictionary<int, bool>
                {
                    { 23, true },
                    { 42, false }
                }
            });
        }

        [Test]
        public void MathTypes()
        {
            SerializeAndAssert("12_MathTypes.xml", new MathTypes
            {
                Point = new Point(1, 2),
                Rectangle = new Rectangle(1, 2, 3, 4),
                Vector3 = new Vector3(1, 2, 3.1f),
                Vector4 = new Vector4(1, 2, 3, 4),
                Quaternion = new Quaternion(1, 2, 3, 4),
                Plane = new Plane(1, 2, 3, 4),
                Matrix = new Matrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Color = new Color{ A = 0xFF, R = 0x64, G = 0x95, B = 0xED },
                Vector2Array = new []{ new Vector2(0, 0), new Vector2(1, 1) },
                Vector2List = new List<Vector2>(new[] { new Vector2(1, 7), new Vector2(1, 9), new Vector2(1, 10) })
            });
        }

        [Test]
        public void PolymorphicTypes()
        {
            SerializeAndAssert("13_PolymorphicTypes.xml", new PolymorphicTypes
            {
                Hello = "World",
                Elf = 23,

                TypedArray = new[]
                {
                    new PolymorphicA { Value = true },
                    new PolymorphicB { Value = true },
                    new PolymorphicC { Value = true }
                },

                UntypedArray = new[]
                {
                    new PolymorphicA { Value = true },
                    new PolymorphicB { Value = true },
                    new PolymorphicC { Value = true }
                }
            });
        }

        [Test]
        public void Namespaces()
        {
            SerializeAndAssert("14_Namespaces.xml", new NamespaceClass
            {
                A = new NamespaceHelper { Value = true },
                B = new Vector2(0, 0),
                C = SpriteSortMode.Immediate,
                D = new Nested.ContentPipeline.ClassInsideNestedAmbiguousNamespace { Value = true },
                E = new Nested.ClassInsideNestedNamespace { Value = true },
                F = new Nested.ContentPipeline2.ClassInsideNestedUnambiguousNamespace { Value = true },
                G = new SomethingElse.ContentPipeline.ClassInsideAmbiguousNamespace { Value = true },
                H = null
            });
        }

        [Test]
        public void FlattenContent()
        {
            SerializeAndAssert("15_FlattenContent.xml", new FlattenContent
            {
                Nested = new NestedClass
                {
                    Name = "Shawn",
                    IsEnglish = true
                },
                Collection = new[] { "Hello", "World" }
            });
        }

        [Test]
        public void CircularReferences()
        {
            var resource1 = new CircularLinked();
            var resource2 = new CircularLinked();
            var resource3 = new CircularLinked();

            resource1.Next = resource2;
            resource2.Next = resource3;
            resource3.Next = resource1;

            Assert.Throws<InvalidOperationException>(() =>
                Serialize("output", new CircularReferences
                {
                    Head = resource1
                }));
        }

        [Test]
        public void SharedResources()
        {
            var resource1 = new Linked { Value = 1 };
            var resource2 = new Linked { Value = 2 };
            var resource3 = new Linked { Value = 3 };

            resource1.Next = resource2;
            resource2.Next = resource3;
            resource3.Next = resource1;

            SerializeAndAssert("16_SharedResources.xml", new SharedResources
            {
                Head = resource1
            });
        }

        [Test]
        public void ExternalReferences()
        {
            var grassExternalReference = new ExternalReference<Texture2D>(Path.GetFullPath("Assets/Xml/grass.tga"));
            SerializeAndAssert("17_ExternalReferences.xml", new ExternalReferences
            {
                Texture = grassExternalReference,
                Texture2 = grassExternalReference,
                Shader = new ExternalReference<Effect>(Path.GetFullPath("Assets/Xml/foliage.fx"))
            });
        }

        [Test]
        public void PrimitiveTypes()
        {
            SerializeAndAssert("18_PrimitiveTypes.xml", new PrimitiveTypes
            {
                Char = 'A',
                Char2 = '°',
                Char3 = 'Δ',
                Byte = 127,
                SByte = -127,
                Short = -1000,
                UShort = 1000,
                Int = -100000,
                UInt = 100000,
                Long = -10000000,
                ULong = 10000000,
                Float = 1234567.0f,
                Double = 1234567890.0,
                NullChar = null,
                NotNullChar = 'B'
            });
        }

        [Test]
        public void FontDescription()
        {
            var fontDescription = new ExtendedFontDescription
            {
                FontName = "Foo",
                Size = 30,
                Spacing = 0.75f,
                UseKerning = true,
                Style = FontDescriptionStyle.Bold,
                DefaultCharacter = '*',
                ExtendedListProperty = { "item0", "item1", "item2" }
            };

            for (var i = 32; i <= 126; i++)
                fontDescription.Characters.Add((char) i);
            fontDescription.Characters.Add(HttpUtility.HtmlDecode("&#916;")[0]);
            fontDescription.Characters.Add(HttpUtility.HtmlDecode("&#176;")[0]);

            SerializeAndAssert("19_FontDescription.xml", fontDescription);
        }

        [Test]
        public void SystemTypes()
        {
            SerializeAndAssert("20_SystemTypes.xml", new SystemTypes
            {
                TimeSpan = TimeSpan.FromSeconds(42.5f)
            });
        }
    }
}
