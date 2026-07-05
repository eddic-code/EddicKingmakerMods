using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;

namespace EddicKingmakerTweaks.Tweaks
{
    /// <summary>
    /// Drops the Improved Unarmed Strike prerequisite from the whole Crane
    /// feat chain (Style, Wing, Riposte). Verified in the blueprint dump:
    /// each feat carries a PrerequisiteFeature component pointing at
    /// ImprovedUnarmedStrike; the other requirements (Dodge, the monk-1 OR
    /// BAB "Any" group, and the chain's own feat prerequisites) are separate
    /// components and stay untouched.
    /// </summary>
    internal static class CraneStylePrerequisites
    {
        private const string ImprovedUnarmedStrikeId = "7812ad3672a4b9a4fb894ea402095167";

        // Feat blueprints (guids from blueprints.txt).
        private static readonly (string Name, string Guid)[] Feats =
        {
            ("Crane Style", "0c17102f650d9044290922b0fad9132f"),
            ("Crane Wing", "af0aae1b973114f47a19ea532237b5fc"),
            ("Crane Riposte", "59eb2a5507975244c893402d582bf77b"),
        };

        public static void Apply()
        {
            foreach (var (name, guid) in Feats)
            {
                var feat = (BlueprintFeature)Main.Library.BlueprintsByAssetId[guid];
                var trimmed = feat.ComponentsArray
                    .Where(c => !(c is PrerequisiteFeature p
                                  && p.Feature != null
                                  && p.Feature.AssetGuid == ImprovedUnarmedStrikeId))
                    .ToArray();

                if (trimmed.Length == feat.ComponentsArray.Length)
                {
                    Main.Logger.Warning($"CraneStylePrerequisites: no Improved Unarmed Strike prerequisite found on {name} — blueprint layout changed?");
                    continue;
                }

                feat.ComponentsArray = trimmed;
            }
        }
    }
}
