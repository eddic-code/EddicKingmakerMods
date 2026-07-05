using System;
using System.Collections.Generic;
using System.IO;
using Kingmaker;
using Newtonsoft.Json;

namespace EddicKingmakerBuffs.Core
{
    /// <summary>One configured buff: which spell, on whom, optionally by whom.</summary>
    public class BuffAssignment
    {
        /// <summary>
        /// The spell's display name as listed in buffs-available.txt.
        /// Matched against the party's available buffs ignoring case, spaces and [tags].
        /// </summary>
        [JsonProperty]
        public string SpellName = "";

        /// <summary>Resolved from SpellName against the party's available buffs on each run.</summary>
        [JsonIgnore]
        public string SpellGuid = "";

        /// <summary>Character names, or the single entry "all" for the whole party.</summary>
        [JsonProperty]
        public List<string> Targets = new List<string>();

        /// <summary>Optional character name; empty/null lets any capable caster cast it.</summary>
        [JsonProperty]
        public string PreferredCaster;
    }

    /// <summary>
    /// Per-save buff routine, hand-edited JSON until the mod grows a UI.
    /// Lives at Mods\EddicKingmakerBuffs\UserSettings\buffs-{GameId}.json.
    /// </summary>
    public class BuffRoutine
    {
        [JsonProperty]
        public bool AllowInCombat;

        [JsonProperty]
        public bool OverwriteExisting;

        [JsonProperty]
        public List<BuffAssignment> Buffs = new List<BuffAssignment>();

        /// <summary>
        /// The game hijacks JsonConvert.DefaultSettings with an opt-in contract resolver
        /// (members without [JsonProperty] are silently dropped). Never use the JsonConvert
        /// helpers here; this serializer is built from clean settings and bypasses the game's.
        /// </summary>
        private static readonly JsonSerializer Serializer =
            JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented });

        public static string PathForCurrentSave()
        {
            var player = Game.Instance?.Player;
            var gameId = player?.GameId;
            if (string.IsNullOrEmpty(gameId))
                return null;

            // Include the main character's name so the files are recognizable per playthrough.
            var name = player.MainCharacter.Value?.CharacterName ?? "Unknown";
            foreach (var invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid.ToString(), "");
            name = name.Replace(" ", "");
            if (name.Length == 0)
                name = "Unknown";

            var dir = Path.Combine(Main.ModFolder, "UserSettings");
            var path = Path.Combine(dir, $"buffs-{name}-{gameId}.json");

            // One-time migration from the old name-less layout.
            try
            {
                var legacy = Path.Combine(dir, $"buffs-{gameId}.json");
                if (!File.Exists(path) && File.Exists(legacy))
                {
                    File.Move(legacy, path);
                    Main.Logger.Log($"Migrated buff routine to {Path.GetFileName(path)}");
                }
            }
            catch (Exception e)
            {
                Main.Logger.LogException(e);
            }

            return path;
        }

        /// <summary>Null if no routine has been configured for this save yet.</summary>
        public static BuffRoutine LoadForCurrentSave()
        {
            var path = PathForCurrentSave();
            if (path == null || !File.Exists(path))
                return null;

            try
            {
                using (var reader = new JsonTextReader(new StringReader(File.ReadAllText(path))))
                {
                    return Serializer.Deserialize<BuffRoutine>(reader);
                }
            }
            catch (Exception e)
            {
                Main.Logger.Error($"Failed to read buff routine at {path}:");
                Main.Logger.LogException(e);
                return null;
            }
        }

        /// <summary>Writes an editable template if this save has no routine yet. Returns the file path.</summary>
        public static string EnsureTemplateExists()
        {
            var path = PathForCurrentSave();
            if (path == null || File.Exists(path))
                return path;

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var template = new BuffRoutine
            {
                Buffs =
                {
                    new BuffAssignment
                    {
                        SpellName = "paste a spell name from buffs-available.txt",
                        Targets = { "all" },
                    },
                },
            };

            using (var writer = new StringWriter())
            {
                Serializer.Serialize(writer, template);
                File.WriteAllText(path, writer.ToString());
            }
            return path;
        }
    }
}
