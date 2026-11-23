using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Match3.Presentation.Tests
{
    // Guards the Phase 18.2 Fredoka font assets: their own character tables must hold every HUD character,
    // so the LiberationSans fallback can never hide a missing Fredoka glyph.
    public class FredokaFontAssetTests
    {
        private const string Folder = "Assets/Fonts/Fredoka/";
        private const string LiberationSansPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        // Text the approved HUD and gameplay controls draw, including every goal colour word.
        private static readonly string[] RequiredText =
        {
            "0123456789 / LEVEL MOVES LEFT SCORE COLLECT ·",
            "RED GREEN BLUE YELLOW PURPLE ORANGE",
            "to go Goal complete! Menu Restart",
        };

        [TestCase("Fredoka-Medium SDF", "Medium")]
        [TestCase("Fredoka-SemiBold SDF", "SemiBold")]
        public void FontAsset_IsStaticSdfaaAtlasWithApprovedSettings(string assetName, string style)
        {
            var font = Load(assetName);
            Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static));
            Assert.That(font.atlasRenderMode, Is.EqualTo(GlyphRenderMode.SDFAA));
            Assert.That(font.faceInfo.pointSize, Is.EqualTo(90));
            Assert.That(font.atlasPadding, Is.EqualTo(9));
            Assert.That(font.atlasWidth, Is.EqualTo(1024));
            Assert.That(font.atlasHeight, Is.EqualTo(1024));
            Assert.That(font.isMultiAtlasTexturesEnabled, Is.False);
            Assert.That(font.atlasTextures.Length, Is.EqualTo(1));
            Assert.That(font.atlasTextures[0].isReadable, Is.False, "A Static atlas needs no CPU copy, like LiberationSans SDF.");
            Assert.That(font.faceInfo.familyName, Is.EqualTo("Fredoka"));
            Assert.That(font.faceInfo.styleName, Is.EqualTo(style));
            Assert.That(font.material.shader, Is.SameAs(Liberation().material.shader));
        }

        [TestCase("Fredoka-Medium SDF")]
        [TestCase("Fredoka-SemiBold SDF")]
        public void OwnCharacterTable_HoldsExactlyAsciiAndMiddleDot_EachWithARasterisedGlyph(string assetName)
        {
            var font = Load(assetName);
            var expected = new System.Collections.Generic.List<uint>();
            for (uint c = 32; c <= 126; c++) expected.Add(c);
            expected.Add(0xB7);

            Assert.That(font.characterTable.Count, Is.EqualTo(expected.Count));
            foreach (uint unicode in expected)
            {
                Assert.That(font.characterLookupTable.TryGetValue(unicode, out TMP_Character character), Is.True,
                    $"U+{unicode:X4} is missing from {assetName}'s own table.");
                if (unicode != ' ')
                {
                    Assert.That(character.glyph.glyphRect.width, Is.GreaterThan(0), $"U+{unicode:X4} has no atlas pixels.");
                    Assert.That(character.glyph.glyphRect.height, Is.GreaterThan(0), $"U+{unicode:X4} has no atlas pixels.");
                }
            }
        }

        [TestCase("Fredoka-Medium SDF")]
        [TestCase("Fredoka-SemiBold SDF")]
        public void RequiredHudText_IsCoveredWithoutFallback(string assetName)
        {
            var font = Load(assetName);
            foreach (string text in RequiredText)
            {
                foreach (char c in text)
                {
                    Assert.That(font.characterLookupTable.ContainsKey(c), Is.True,
                        $"'{c}' in \"{text}\" would come from a fallback.");
                }
            }
        }

        [TestCase("Fredoka-Medium SDF")]
        [TestCase("Fredoka-SemiBold SDF")]
        public void Fallback_IsOnlyTheExistingStaticLiberationSans(string assetName)
        {
            var font = Load(assetName);
            Assert.That(font.fallbackFontAssetTable, Is.EqualTo(new[] { Liberation() }));
            Assert.That(Liberation().atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static));
        }

        [Test]
        public void Licence_SitsBesideTheFontFiles()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<Font>(Folder + "Fredoka-Medium.ttf"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Font>(Folder + "Fredoka-SemiBold.ttf"), Is.Not.Null);
            var licence = AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + "OFL.txt");
            Assert.That(licence, Is.Not.Null);
            Assert.That(licence.text, Does.Contain("Copyright 2016 The Fredoka Project Authors"));
            Assert.That(licence.text, Does.Contain("SIL OPEN FONT LICENSE Version 1.1"));
        }

        private static TMP_FontAsset Load(string assetName)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder + assetName + ".asset");
            Assert.That(font, Is.Not.Null, assetName);
            return font;
        }

        private static TMP_FontAsset Liberation()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationSansPath);
        }
    }
}
