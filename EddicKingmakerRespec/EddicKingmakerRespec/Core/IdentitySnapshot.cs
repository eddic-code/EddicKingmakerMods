using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.Visual.Sound;

namespace EddicKingmakerRespec.Core
{
    /// <summary>
    /// A unit's identity captured before a respec: everything the rebuilt
    /// character must keep in a partial respec. All of these are [JsonProperty]
    /// members of UnitDescriptor, so the vanilla respec transplant (which
    /// serializes the freshly built unit over the old one) would otherwise
    /// replace them with blueprint defaults.
    /// </summary>
    internal sealed class IdentitySnapshot
    {
        public string UniqueId;
        public string CustomName;
        public Gender? CustomGender;
        public BlueprintUnitAsksList CustomAsks;
        public bool? LeftHandedOverride;
        public string CustomPrefabGuid;
        public DollData Doll;
        public int BirthDay;
        public int BirthMonth;

        public static IdentitySnapshot From(UnitEntityData unit)
        {
            UnitDescriptor d = unit.Descriptor;
            return new IdentitySnapshot
            {
                UniqueId = unit.UniqueId,
                CustomName = d.CustomName,
                CustomGender = d.CustomGender,
                CustomAsks = d.CustomAsks,
                LeftHandedOverride = d.LeftHandedOverride,
                CustomPrefabGuid = d.CustomPrefabGuid,
                Doll = d.Doll,
                BirthDay = d.BirthDay,
                BirthMonth = d.BirthMonth,
            };
        }

        // The vacuum unit the respec builds on is created with the original unit's UniqueId.
        public bool Matches(UnitDescriptor descriptor) => descriptor?.Unit?.UniqueId == UniqueId;

        /// <summary>Restores everything except the doll, which must be handled at commit time.</summary>
        public void ApplyScalarsTo(UnitDescriptor d)
        {
            d.CustomName = CustomName;
            d.CustomGender = CustomGender;
            d.CustomAsks = CustomAsks;
            d.LeftHandedOverride = LeftHandedOverride;
            d.CustomPrefabGuid = CustomPrefabGuid;
            d.BirthDay = BirthDay;
            d.BirthMonth = BirthMonth;
        }
    }
}
