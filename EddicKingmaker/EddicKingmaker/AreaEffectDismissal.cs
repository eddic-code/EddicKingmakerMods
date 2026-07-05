using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.PubSubSystem;

namespace EddicKingmaker
{
    /// <summary>
    /// Dismisses lingering area effects out of combat (ported from the
    /// RemoveAreaEffects mod), e.g. Grease or Cloudkill left over after a fight.
    /// Only the spell AoEs whitelisted by the original mod are removed: other
    /// area effects (traps, scripted zones, environment hazards) must survive,
    /// so everything not on the list is left strictly alone.
    /// </summary>
    internal static class AreaEffectDismissal
    {
        public static void DismissWhitelisted()
        {
            if (Game.Instance.Player.ControllableCharacters.Any(unit => unit.IsInCombat))
            {
                NotifyWarning("Cannot remove area effects in combat.");
                return;
            }

            var dismissible = Game.Instance.State.AreaEffects
                .Where(area => DismissibleAreas.Contains(area.Blueprint.AssetGuid))
                .ToList();
            if (dismissible.Count == 0)
            {
                NotifyWarning("No area effects to remove.");
                return;
            }

            foreach (var area in dismissible)
                area.ForceEnd();

            NotifyLog($"Removed {dismissible.Count} area effect{(dismissible.Count == 1 ? "" : "s")}.");
        }

        private static void NotifyWarning(string message)
        {
            EventBus.RaiseEvent<IWarningNotificationUIHandler>(h => h.HandleWarning(message, true));
        }

        private static void NotifyLog(string message)
        {
            Game.Instance.UI.BattleLogManager?.LogView?.AddLogEntry(message, GameLogStrings.Instance.DefaultColor);
        }

        // Guid whitelist taken verbatim from the RemoveAreaEffects mod: the
        // player-castable area spells that are safe to dismiss.
        private static readonly HashSet<string> DismissibleAreas = new HashSet<string>
        {
            "f4dc3f53090627945b83f16ebf3146a6",
			"e122151e93e44e0488521aed9e51b617",
			"cae4347a512809e4388fb3949dc0bc67",
			"6c116b31887c6284fbd41c070f6422f6",
			"6df1ac314d4e6e9418e34470b79f90d8",
			"cf742a1d377378e4c8799f6a3afff1ba",
			"bcb6329cefc66da41b011299a43cc681",
			"d46313be45054b248a1f1656ddb38614",
			"d086b1aeb367a5b43808d34c321955d1",
			"4c695315962bf9a4ea7fc7e2bb3e2f60",
			"6b2b1ba6ec6487f46b8c76b603abba6b",
			"e09010a73354a794293ebc7b33c2d130",
			"d64b08ae01012e34cbc55b3a27ea29b7",
			"72328360f1eeeb94d8a43d51db96eccb",
			"b21bc337e2beaa74b8823570cd45d6dd",
			"bb87c7513a16b9a44b4948a4e932a81b",
			"16e0e4c6a16f68c49832340b93706499",
			"eca936a9e235875498d1e74ff7c09ecd",
			"beccc33f543b1f8469c018982c23ac06",
			"aa2e0a0fe89693f4e9205fd52c5ba3e5",
			"d59890fb468915946bae085439bd0881",
			"1d649d8859b25024888966ba1cc291d1",
			"1f45c8b0a735097439a9dac04f5b0161",
			"fd323c05f76390749a8555b13156813d",
			"6ea87a0ff5df41c459d641326f9973d5",
			"48aa66d1a15515e40b07bc1f5fb80f64",
			"35a62ad81dd5ae3478956c61d6cd2d2e",
			"3659ce23ae102ca47a7bf3a30dd98609",
			"4b19dd893a4b80a49905903bcd56b9e2",
			"c26aa67475bdb64449b0e0be6a9ea823",
			"38a2979db34ad0f45a449e5eb174729f",
			"267f19ba174b21e4d9baf30afd589068",
			"0af604484b5fcbb41b328750797e3948",
			"2a90aa7f771677b4e9624fa77697fdc6",
			"d12f759590ac61b40870a0725b92a985",
			"f3b3f32b7f9f35b4cb4114d633b6de6d",
			"a4d33389f2b7b824889169d227cab729",
			"724d174829a1c1949a4a7ba99cfb06a0",
			"2414e5c126976604584ebcee90395eee",
			"af830491079fea141ad5f46e2dcf93cf",
			"740b3ba212b5bb448becf202a97cdbf4",
			"edb2896d49015434bbbe401ee27338c3",
			"3b65f77ec33ab764592803685fe6891e",
			"f92cdd3b43a744f4f8abeacb913c92fb",
			"c6b4fc6e73c25de4f83378c959144dc8",
			"9a9895cbb91a15d48a0368ee8d0f650e",
			"2cad16fcffefe3240b2d6dc3d33ff580",
			"182de1c07ecb56d448cd6d3237ae4b81",
			"2eef9ca9e79968547a01d06d3828e17f",
			"6a64cc20d5820dc4cb3907b36ce6ac13",
			"757b40456bbe27a46bbf18a57d64f31b",
			"bb4ddd5e7d64a4a49ba71fe8275d1552"
        };
    }
}
