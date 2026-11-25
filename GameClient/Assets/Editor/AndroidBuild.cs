#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GameClient.EditorTools
{
    /// <summary>
    /// Android ayarları + APK derlemesi. Menü: Tools/Android. Batch:
    /// Unity -batchmode -quit -projectPath GameClient -buildTarget Android -executeMethod GameClient.EditorTools.AndroidBuild.BuildApk
    /// Çıktı: GameClient/Builds/Android/*.apk (git'e girmez). İmza: Unity hata ayıklama anahtarı (yalnızca test kurulumu için).
    /// </summary>
    public static class AndroidBuild
    {
        // shortcut: oyun adı ve paket kimliği henüz onaylı değil (geçici); mağaza öncesi kullanıcıyla belirlenir.
        private const string Company = "GameStudio";
        private const string Product = "Koy Savunmasi";
        private const string PackageId = "com.gamestudio.koysavunmasi";
        private const string Version = "0.1.0";
        private const int VersionCode = 1;

        [MenuItem("Tools/Android/Ayarlari Uygula")]
        public static void ApplySettings()
        {
            PlayerSettings.companyName = Company;
            PlayerSettings.productName = Product;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
            PlayerSettings.Android.bundleVersionCode = VersionCode;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            // Google Play 64-bit ister; IL2CPP + ARM64
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // arayüz yatay tasarlandı: yalnızca yatay, iki yöne döner
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            // tüm ekranı kapla (çentik/kamera deliği dahil); arayüz kodda Screen.safeArea içine çizilir. En-boy 2.4'e kadar (ProjectSettings)
            PlayerSettings.Android.renderOutsideSafeArea = true;
            EditorUserBuildSettings.buildAppBundle = false; // .apk (doğrudan telefona kurulur)
            AssetDatabase.SaveAssets();
            Debug.Log($"[AndroidBuild] Ayarlar uygulandı: {PackageId} {Version} ({VersionCode}), IL2CPP ARM64, yatay");
        }

        [MenuItem("Tools/Android/APK Olustur")]
        public static void BuildApk()
        {
            ApplySettings();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Android"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"KoySavunmasi-{Version}-{VersionCode}.apk");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes, locationPathName = path, target = BuildTarget.Android, options = BuildOptions.None
            });
            var sum = report.summary;
            Debug.Log($"[AndroidBuild] Sonuç: {sum.result}, {sum.totalSize / (1024f * 1024f):F1} MB, {sum.totalTime}, {path}");
            if (Application.isBatchMode) EditorApplication.Exit(sum.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
#endif
