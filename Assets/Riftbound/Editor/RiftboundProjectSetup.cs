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
        }
    }
}
