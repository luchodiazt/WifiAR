using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARCore;
using UnityEngine;
using UnityEngine.Rendering;
using WifiAR.Diagnostics;

namespace WifiAR.EditorTools
{
    /// <summary>Keeps project configuration reproducible instead of relying on manual clicks.</summary>
    public static class ProjectSetup
    {
        // Permanent once the app is published on Google Play. Change before the first upload if needed.
        const string ApplicationId = "com.luisdiaz.wifiar";
        const int MinSdk = 30; // AR Mobile template default
        const int TargetSdk = 36;

        [MenuItem("Tools/WifiAR/Configure Project")]
        public static void ConfigureProject()
        {
            NamedBuildTarget android = NamedBuildTarget.Android;

            PlayerSettings.SetApplicationIdentifier(android, ApplicationId);
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)MinSdk;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)TargetSdk;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // The app is useless without AR, but depth is only a nice-to-have: requiring it would
            // hide the app on Google Play for every ARCore phone without Depth API support.
            ARCoreSettings arCore = ARCoreSettings.GetOrCreateSettings();
            arCore.requirement = ARCoreSettings.Requirement.Required;
            arCore.depth = ARCoreSettings.Requirement.Optional;
            EditorUtility.SetDirty(arCore);

            AssetDatabase.SaveAssets();
            Debug.Log($"WifiAR: Android settings applied ({ApplicationId}, IL2CPP, ARM64, API {MinSdk}-{TargetSdk}, GLES3, portrait, ARCore required, depth optional).");
        }

        [MenuItem("Tools/WifiAR/Add Signal Probe To Scene")]
        public static void AddSignalProbe()
        {
            SignalProbe existing = Object.FindAnyObjectByType<SignalProbe>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("WifiAR: scene already has a Signal Probe.");
                return;
            }

            var probe = new GameObject("Signal Probe", typeof(SignalProbe));
            Undo.RegisterCreatedObjectUndo(probe, "Add Signal Probe");
            EditorSceneManager.MarkSceneDirty(probe.scene);
            Selection.activeGameObject = probe;
        }
    }
}
