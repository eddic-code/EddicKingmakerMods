using System;
using System.IO;

namespace EddicKingmaker
{
    /// <summary>
    /// Development utility: dumps every blueprint in the game library (name, guid,
    /// type — tab-separated) to a text file next to the mod dll, for grepping
    /// vanilla guids. Triggered by hotkey from Main.OnUpdate; not run automatically.
    /// Pattern from EldritchArcana's WriteBlueprints.
    /// </summary>
    internal static class BlueprintDump
    {
        public static void DumpAll()
        {
            if (Main.Library == null)
            {
                Main.Logger.Warning("Blueprint dump: library not loaded yet.");
                return;
            }

            var path = Path.Combine(Main.ModFolder ?? ".", "blueprints.txt");
            try
            {
                var blueprints = Main.Library.GetAllBlueprints();
                using (var writer = new StreamWriter(path, append: false))
                {
                    writer.WriteLine("Name\tAssetGuid\tType");
                    foreach (var blueprint in blueprints)
                    {
                        writer.WriteLine($"{blueprint.name}\t{blueprint.AssetGuid}\t{blueprint.GetType().FullName}");
                    }
                }
                Main.Logger.Log($"Dumped {blueprints.Count} blueprints to {path}");
            }
            catch (Exception e)
            {
                Main.Logger.Error($"Blueprint dump to {path} failed.");
                Main.Logger.LogException(e);
            }
        }
    }
}
