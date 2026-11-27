using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Match3.Presentation.Tests
{
    public class CompactHudSpriteImportTests
    {
        [TestCase("HudShell", 144, 224, 68, 68, 68, 68)]
        [TestCase("HudShellShadow", 384, 464, 188, 0, 188, 0)]
        [TestCase("GoalHalo", 160, 160, 0, 0, 0, 0)]
        [TestCase("GoalTrack", 68, 36, 18, 0, 18, 0)]
        [TestCase("GoalFill", 36, 28, 14, 0, 14, 0)]
        public void Sprite_PreservesSourceSizeAndApprovedUiImportSettings(
            string name, int width, int height, int left, int bottom, int right, int top)
        {
            string path = "Assets/Art/Hud/Compact/" + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(sprite, Is.Not.Null, path);

            // Check the imported result as well as the requested settings: the NPOT
            // source dimensions and pixel borders must survive Android import unchanged.
            Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(width, height)));
            Assert.That(sprite.texture.width, Is.EqualTo(width));
            Assert.That(sprite.texture.height, Is.EqualTo(height));
            Assert.That(sprite.border, Is.EqualTo(new Vector4(left, bottom, right, top)));
            Assert.That(sprite.pixelsPerUnit, Is.EqualTo(200f));
            Assert.That(sprite.pivot, Is.EqualTo(new Vector2(width / 2f, height / 2f)));
            Assert.That(sprite.texture.isReadable, Is.False);
            Assert.That(sprite.GetPhysicsShapeCount(), Is.Zero,
                "UI decoration needs no fallback physics shape.");

            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(200f));
            Assert.That(importer.spriteBorder, Is.EqualTo(sprite.border));
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.alphaSource, Is.EqualTo(TextureImporterAlphaSource.FromInput));
            Assert.That(importer.alphaIsTransparency, Is.True);
            Assert.That(importer.sRGBTexture, Is.True);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.crunchedCompression, Is.False);
            Assert.That(importer.maxTextureSize, Is.EqualTo(512));
            Assert.That(importer.isReadable, Is.False);

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
            Assert.That(settings.spriteAlignment, Is.EqualTo((int)SpriteAlignment.Center));
            Assert.That(settings.spritePivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(settings.spriteGenerateFallbackPhysicsShape, Is.False);

            var android = importer.GetPlatformTextureSettings("Android");
            Assert.That(android.overridden, Is.True);
            Assert.That(android.format, Is.EqualTo(TextureImporterFormat.RGBA32));
            Assert.That(android.maxTextureSize, Is.EqualTo(512));
            Assert.That(android.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(android.crunchedCompression, Is.False);
        }
    }
}
