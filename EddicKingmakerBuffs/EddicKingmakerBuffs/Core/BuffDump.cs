using System;
using System.IO;
using System.Linq;
using System.Text;
using Kingmaker;

namespace EddicKingmakerBuffs.Core
{
    /// <summary>
    /// Shift+F1: writes every buff the party can currently cast to
    /// Mods\EddicKingmakerBuffs\buffs-available.txt (guids ready to paste into the routine JSON)
    /// and creates the routine template for the current save if it doesn't exist yet.
    /// </summary>
    public static class BuffDump
    {
        public static void Write()
        {
            var player = Game.Instance?.Player;
            if (player == null || player.Party.Count == 0)
                return;

            var available = BuffScanner.ScanParty();
            var configPath = BuffRoutine.EnsureTemplateExists();
            var dumpPath = Path.Combine(Main.ModFolder, "buffs-available.txt");

            var sb = new StringBuilder();
            sb.AppendLine("EddicKingmakerBuffs — buffs the party can cast right now");
            sb.AppendLine($"Dumped {DateTime.Now:yyyy-MM-dd HH:mm} — spell availability reflects currently memorized/known spells.");
            sb.AppendLine();
            sb.AppendLine($"Copy a spell name into the SpellName field of: {configPath}");
            sb.AppendLine("Name matching ignores case and spaces. Targets: character names, or \"all\".");
            sb.AppendLine("PreferredCaster: optional character name.");
            sb.AppendLine("Then press F1 in game to apply the routine. [short] buffs last rounds only;");
            sb.AppendLine("[mass] buffs cover the party in one cast — leave their Targets list empty ([]),");
            sb.AppendLine("the caster casts them on themselves (e.g. Bless).");
            sb.AppendLine("[toggle] entries (songs, stances) are switched on, not cast: Targets is ignored,");
            sb.AppendLine("PreferredCaster picks who activates it. They stay on until you turn them off.");
            sb.AppendLine();

            foreach (var buff in available.Values.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase))
            {
                var tags = buff.IsActivatable
                    ? " [toggle]"
                    : (buff.Effects.IsLong ? "" : " [short]") + (buff.IsMass ? " [mass]" : "");
                sb.AppendLine($"{buff.Name}{tags}");
                foreach (var provider in buff.Providers)
                {
                    var left = provider.CastsLeft;
                    if (provider.Activatable != null)
                    {
                        var rounds = left < 0 ? "no resource cost" : $"{left} round(s) left";
                        sb.AppendLine($"    owner: {provider.Caster.CharacterName} ({rounds}{(provider.Activatable.IsOn ? ", currently ON" : "")})");
                    }
                    else
                    {
                        var uses = left < 0 ? "at will" : $"{left} cast(s) left";
                        var source = provider.Book != null ? provider.Book.Blueprint.name : "class ability";
                        sb.AppendLine($"    caster: {provider.Caster.CharacterName} ({source}, {uses})");
                    }
                }
            }

            File.WriteAllText(dumpPath, sb.ToString());
            Main.Logger.Log($"Dumped {available.Count} available buffs to {dumpPath}");
            Main.Logger.Log($"Routine config for this save: {configPath}");
        }
    }
}
