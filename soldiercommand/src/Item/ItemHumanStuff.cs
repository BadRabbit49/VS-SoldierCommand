using Vintagestory.API.Common;
using System;

namespace SoldierCommand {
	public class ItemHumanStuff : Item {
		public HumanGearType type {
			get {
				HumanGearType type;
				if (Enum.TryParse<HumanGearType>(Variant["type"].ToUpper(), out type)) {
					return type;
				} else {
					return HumanGearType.HAIRS;
				}
			}
		}

		public string weaponAssetLocation => Attributes["weaponAssetLocation"].AsString();

		public int backpackSlots => Attributes["backpackslots"].AsInt(0);
	}

	public enum HumanGearType { HAIRS, EXTRA, BEARD, FACES }
}