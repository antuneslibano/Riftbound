using UnityEditor;
using UnityEngine;

namespace Riftbound.EditorTools
{
    /// <summary>
    /// Runs once per Editor session and makes sure the minimal project settings the prototype relies on are in place:
    /// Main scene in Build Settings, portrait orientation and the legacy Input Manager being enabled
    /// (touch + mouse input use UnityEngine.Input).
    /// </summary>
    [InitializeOnLoad]
    static class RiftboundProjectSetup
    {
        const string SessionKey = "Riftbound.ProjectSetupChecked";

        static RiftboundProjectSetup()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += Check;
        }

        [MenuItem("Riftbound/Validate Project Setup", priority = 41)]
        static void Check()
        {
            bool sceneListed = false;
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path == RiftboundBuild.ScenePath && s.enabled) sceneListed = true;
            if (!sceneListed)
            {
                RiftboundBuild.EnsureSceneInBuildSettings();
                Debug.Log("[Riftbound] Added " + RiftboundBuild.ScenePath + " to Build Settings.");
            }

            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
            {
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                Debug.Log("[Riftbound] Default orientation set to Portrait.");
            }

            // 0 = Input Manager (old), 1 = Input System (new), 2 = Both. The prototype needs 0 or 2.
            var assets = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            if (assets != null && assets.Length > 0)
            {
                var so = new SerializedObject(assets[0]);
                var prop = so.FindProperty("activeInputHandler");
                if (prop != null && prop.intValue == 1)
                {
                    prop.intValue = 2;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.LogWarning("[Riftbound] Active Input Handling switched to 'Both' (the prototype uses the legacy Input Manager). Restart the Editor to apply.");
                }
            }

#if !ENABLE_LEGACY_INPUT_MANAGER
            Debug.LogError("[Riftbound] Legacy Input Manager is disabled: set Project Settings > Player > Active Input Handling to 'Input Manager (Old)' or 'Both' and restart.");
#endif
        }
    }
}
