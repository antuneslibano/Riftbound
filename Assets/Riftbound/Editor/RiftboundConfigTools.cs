using System.IO;
using Riftbound.Config;
using UnityEditor;
using UnityEngine;

namespace Riftbound.EditorTools
{
    /// <summary>Editor helpers for the balance assets.</summary>
    public static class RiftboundConfigTools
    {
        const string ConfigPath = "Assets/Riftbound/Resources/RiftboundConfig.asset";
        const string CardsFolder = "Assets/Riftbound/Config/Cards";

        [MenuItem("Riftbound/Select Game Config", priority = 0)]
        public static void SelectConfig()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (cfg == null)
            {
                EditorUtility.DisplayDialog("Riftbound", "Config not found at " + ConfigPath + ".\nUse Riftbound > Regenerate Default Config Assets.", "OK");
                return;
            }
            Selection.activeObject = cfg;
            EditorGUIUtility.PingObject(cfg);
        }

        [MenuItem("Riftbound/Open Main Scene", priority = 1)]
        public static void OpenMainScene()
        {
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RiftboundBuild.ScenePath);
        }

        /// <summary>Rebuilds the config + 8 cards (+ Spawnling) from <see cref="DefaultContent"/>. Overwrites balance edits!</summary>
        [MenuItem("Riftbound/Regenerate Default Config Assets", priority = 40)]
        public static void RegenerateDefaults()
        {
            if (!EditorUtility.DisplayDialog("Riftbound",
                    "Overwrite RiftboundConfig and all card assets with the built-in defaults?\nAll balance changes will be lost.",
                    "Overwrite", "Cancel"))
                return;

            Directory.CreateDirectory(CardsFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));

            var cards = DefaultContent.CreateCards(out var spawnling);
            spawnling = SaveOrReplace(spawnling, CardsFolder + "/Spawnling.asset");
            var saved = new CardDefinition[cards.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].kind == CardKind.ParasiteSpell) cards[i].parasiteSpawnCard = spawnling;
                saved[i] = SaveOrReplace(cards[i], CardsFolder + "/" + Title(cards[i].cardId) + ".asset");
            }

            var cfg = ScriptableObject.CreateInstance<GameConfig>();
            cfg.deck.AddRange(saved);
            SaveOrReplace(cfg, ConfigPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SelectConfig();
        }

        static T SaveOrReplace<T>(T obj, string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                // Keep the asset GUID (and every reference to it) by copying values into it.
                EditorUtility.CopySerialized(obj, existing);
                existing.name = Path.GetFileNameWithoutExtension(path);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            obj.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(obj, path);
            return obj;
        }

        static string Title(string id) => id.Length <= 1 ? id : id.Substring(0, 1) + id.Substring(1).ToLowerInvariant();
    }
}
