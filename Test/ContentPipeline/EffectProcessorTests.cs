using System;
using Microsoft.Xna.Framework.Content.Pipeline;
using NUnit.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using System.IO;
#if DIRECTX
using System.Collections.Generic;
using TwoMGFX;
#endif

namespace MonoGame.Tests.ContentPipeline
{
    class EffectProcessorTests
    {
        class ImporterContext : ContentImporterContext
        {
            public override string IntermediateDirectory
            {
                get { throw new NotImplementedException(); }
            }

            public override ContentBuildLogger Logger
            {
                get { throw new NotImplementedException(); }
            }

            public override string OutputDirectory
            {
                get { throw new NotImplementedException(); }
            }

            public override void AddDependency(string filename)
            {
                throw new NotImplementedException();
            }
        }

#if DIRECTX
        [TestCase("Assets/Effects/PreprocessorTest.fx")]
        public void TestPreprocessor(string effectFile)
        {
            var effectCode = File.ReadAllText(effectFile);
            var fullPath = Path.GetFullPath(effectFile);

            // Preprocess.
            var mgDependencies = new List<string>();
            var mgPreprocessed = Preprocessor.Preprocess(effectCode, fullPath, new Dictionary<string, string>
            {
                { "TEST2", "1" }
            }, mgDependencies);

            Assert.That(mgDependencies, Has.Count.EqualTo(1));
            Assert.That(Path.GetFileName(mgDependencies[0]), Is.EqualTo("include.fxh"));

            Assert.That(mgPreprocessed, Is.Not.StringContaining("Foo"));
            Assert.That(mgPreprocessed, Is.StringContaining("Bar"));
            Assert.That(mgPreprocessed, Is.Not.StringContaining("Baz"));

            Assert.That(mgPreprocessed, Is.StringContaining("FOO"));
            Assert.That(mgPreprocessed, Is.Not.StringContaining("BAR"));
        }
#endif

        [Test]
        [TestCase("Assets/Effects/ParserTest.fx")]
        public void TestParser(string effectFile)
        {
            BuildEffect(effectFile, TargetPlatform.Windows);
        }

        [Test]
        public void TestDefines()
        {
            Assert.DoesNotThrow(() => BuildEffect("Assets/Effects/DefinesTest.fx", TargetPlatform.Windows));
            Assert.Throws<InvalidContentException>(() =>
                BuildEffect("Assets/Effects/DefinesTest.fx", TargetPlatform.Windows, "INVALID_SYNTAX;ANOTHER_MACRO"));
        }

        [Test]
        [TestCase("Assets/Effects/Stock/AlphaTestEffect.fx")]
        [TestCase("Assets/Effects/Stock/BasicEffect.fx")]
        [TestCase("Assets/Effects/Stock/DualTextureEffect.fx")]
        [TestCase("Assets/Effects/Stock/EnvironmentMapEffect.fx")]
        [TestCase("Assets/Effects/Stock/SkinnedEffect.fx")]
        [TestCase("Assets/Effects/Stock/SpriteEffect.fx")]
        public void BuildStockEffect(string effectFile)
        {
            BuildEffect(effectFile, TargetPlatform.Windows);
        }

        private void BuildEffect(string effectFile, TargetPlatform targetPlatform, string defines = null)
        {
            var importerContext = new ImporterContext();
            var importer = new EffectImporter();
            var input = importer.Import(effectFile, importerContext);

            Assert.NotNull(input);

            var processorContext = new TestProcessorContext(targetPlatform, Path.ChangeExtension(effectFile, ".xnb"));
            var processor = new EffectProcessor { Defines = defines };
            var output = processor.Process(input, processorContext);

            Assert.NotNull(output);

            // TODO: Should we test the writer?
        }
    }
}