#if UNITY_EDITOR
using UnityEditor;

namespace GameClient.EditorTools
{
    /// <summary>
    /// Hazır CC0 modellerin içe aktarma ayarları (Assets/Resources/ThirdParty/...):
    /// KayKit karakterleri ve animasyonları "Legacy" animasyonla (çalışma anında Animation bileşeniyle oynatılır, Animator Controller gerekmez);
    /// Kenney yapı parçaları animasyonsuz. Oyun kuralı etkilenmez.
    /// </summary>
    public sealed class ThirdPartyImport : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.Contains("/ThirdParty/")) return;
            var mi = (ModelImporter)assetImporter;
            bool kayChar = assetPath.Contains("/KayKit/Characters/");
            bool kayAnim = assetPath.Contains("/KayKit/Animations/");
            if (kayChar || kayAnim)
            {
                mi.animationType = ModelImporterAnimationType.Legacy;
                mi.importAnimation = kayAnim;
                mi.generateAnimations = ModelImporterGenerateAnimations.GenerateAnimations;
            }
            else
            {
                mi.animationType = ModelImporterAnimationType.None;
                mi.importAnimation = false;
            }
            mi.importCameras = false;
            mi.importLights = false;
            mi.isReadable = false;
        }
    }
}
#endif
