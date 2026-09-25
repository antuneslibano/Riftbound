using UnityEditor;
using UnityEngine;

namespace Riftbound.EditorTools
{
    /// <summary>
    /// Runs once per Editor session and makes sure the minimal project settings the prototype relies on are in place:
    /// Main scene in Build Settings, portrait orientation and a usable input backend
    /// (Input System package or legacy Input Manager, see RiftInput).
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

#if !(RIFTBOUND_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM) && !ENABLE_LEGACY_INPUT_MANAGER
            Debug.LogError("[Riftbound] No usable input backend. Install the Input System package (Window > Package Manager) " +
                           "and set Project Settings > Player > Active Input Handling to 'Input System Package (New)' or 'Both'.");
#endif

            FixDeprecatedSettings();
        }

        /// <summary>
        /// Removes the two deprecation warnings of recent Unity 6 versions:
        /// "Input Manager is marked for deprecation" (switch to the Input System package only) and
        /// "Dynamic Batching is deprecated" (turn it off for every platform; the prototype doesn't need it).
        /// </summary>
        [MenuItem("Riftbound/Fix Deprecation Warnings", priority = 42)]
        static void FixDeprecatedSettings()
        {
            var assets = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            bool changed = false;

#if RIFTBOUND_INPUT_SYSTEM
            // 0 = Input Manager (old), 1 = Input System Package (new), 2 = Both.
            var input = so.FindProperty("activeInputHandler");
            if (input != null && input.intValue != 1)
            {
                input.intValue = 1;
                changed = true;
                Debug.LogWarning("[Riftbound] Active Input Handling set to 'Input System Package (New)'. Restart the Editor to apply.");
            }
#endif

            var batching = so.FindProperty("m_BuildTargetBatching");
            if (batching != null && batching.isArray)
            {
                for (int i = 0; i < batching.arraySize; i++)
                {
                    var dyn = batching.GetArrayElementAtIndex(i).FindPropertyRelative("m_DynamicBatching");
                    if (dyn != null && dyn.boolValue)
                    {
                        dyn.boolValue = false;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                Debug.Log("[Riftbound] Deprecated project settings updated (Input System only, Dynamic Batching off).");
            }
        }
    }
}
