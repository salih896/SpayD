using UnityEditor;

// Resources/PixelFx altındaki piksel efekt sayfaları: sıkıştırma yok, mipmap yok, Point filtre.
public sealed class PixelFxImport : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains("Resources/PixelFx/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Default;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.filterMode = UnityEngine.FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.maxTextureSize = 4096;
        ti.isReadable = false;
    }
}
