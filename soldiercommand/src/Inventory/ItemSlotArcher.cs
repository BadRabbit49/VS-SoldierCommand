using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SoldierCommand {
	public abstract class ItemSlotArcher : ItemSlot {
		public ItemSlotArcher(InventoryArcher inventory) : base(inventory) {
			MaxSlotStackSize = 1;
		}
	}

	public class ItemSlotArcherWear : ItemSlotArcher {
		protected static Dictionary<EnumCharacterDressType, string> iconByDressType = new Dictionary<EnumCharacterDressType, string> {
			{ EnumCharacterDressType.Foot, "boots" },
			{ EnumCharacterDressType.Hand, "gloves" },
			{ EnumCharacterDressType.Shoulder, "cape" },
			{ EnumCharacterDressType.Head, "hat" },
			{ EnumCharacterDressType.LowerBody, "trousers" },
			{ EnumCharacterDressType.UpperBody, "shirt" },
			{ EnumCharacterDressType.UpperBodyOver, "pullover" },
			{ EnumCharacterDressType.Neck, "necklace" },
			{ EnumCharacterDressType.Arm, "bracers" },
			{ EnumCharacterDressType.Waist, "belt" },
			{ EnumCharacterDressType.Emblem, "medal" },
			{ EnumCharacterDressType.Face, "mask" }
		};
		protected EnumCharacterDressType dressType;

		public bool IsArmorSlot { get; protected set; }

		public override EnumItemStorageFlags StorageType => EnumItemStorageFlags.Outfit;

		public ItemSlotArcherWear(InventoryArcher inventory, EnumCharacterDressType dressType, int slotId) : base(inventory) {
			this.dressType = dressType;
			IsArmorSlot = IsArmor(dressType);
			switch (slotId) {
				case 12: BackgroundIcon = "itemslot-head.svg"; break;
				case 13: BackgroundIcon = "itemslot-body.svg"; break;
				case 14: BackgroundIcon = "itemslot-legs.svg"; break;
				default: iconByDressType.TryGetValue(dressType, out BackgroundIcon); break;
			}
		}

		public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge) {
			return IsAcceptable(sourceSlot) && base.CanTakeFrom(sourceSlot, priority);
		}

		public override bool CanHold(ItemSlot sourceSlot) {
			return IsAcceptable(sourceSlot) && base.CanHold(sourceSlot);
		}

		private bool IsAcceptable(ItemSlot sourceSlot) {
			return ItemSlotCharacter.IsDressType(sourceSlot?.Itemstack, dressType);
		}

		public static bool IsArmor(EnumCharacterDressType dressType) {
			switch (dressType) {
				case EnumCharacterDressType.ArmorHead:
				case EnumCharacterDressType.ArmorBody:
				case EnumCharacterDressType.ArmorLegs:
				return true;
			}
			return false;
		}
	}

	public class ItemSlotArcherHand : ItemSlotArcher {
		public ItemSlotArcherHand(InventoryArcher inventory, int slotId) : base(inventory) {
			switch (slotId) {
				case 15: BackgroundIcon = "itemslot-shield.svg"; StorageType = EnumItemStorageFlags.Offhand; break;
				case 16: BackgroundIcon = "itemslot-sword.svg"; break;
			}
		}

		public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge) {
			return IsAcceptable(sourceSlot) && base.CanTakeFrom(sourceSlot, priority);
		}

		public override bool CanHold(ItemSlot sourceSlot) {
			return IsAcceptable(sourceSlot) && base.CanHold(sourceSlot);
		}

		private bool IsAcceptable(ItemSlot sourceSlot) {
			var collectible = sourceSlot?.Itemstack?.Collectible;
			// Check if the item can be placed on a toolrack, or held like a lantern or torch.
			return collectible?.Attributes?["toolrackTransform"]?.Exists ?? collectible?.Attributes?["heldTpIdleAnimation"]?.Exists ?? false;
		}
	}

	public class ItemSlotArcherBack : ItemSlotArcher {
		public override EnumItemStorageFlags StorageType => EnumItemStorageFlags.Backpack;

		public ItemSlotArcherBack(InventoryArcher inventory) : base(inventory) {
			BackgroundIcon = "itemslot-backpack.svg";
		}

		public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge) {
			return IsAcceptable(sourceSlot) && base.CanTakeFrom(sourceSlot, priority);
		}

		public override bool CanHold(ItemSlot sourceSlot) {
			return IsAcceptable(sourceSlot) && base.CanHold(sourceSlot);
		}

		private bool IsAcceptable(ItemSlot sourceSlot) {
			// Only allow empty backpacks for now to avoid issues.
			return CollectibleObject.IsEmptyBackPack(sourceSlot.Itemstack);
		}
	}

	public class ItemSlotArcherAmmo : ItemSlotArcher {
		public override EnumItemStorageFlags StorageType => EnumItemStorageFlags.Arrow;
		public ItemSlotArcherAmmo(InventoryArcher inventory) : base(inventory) {
			BackgroundIcon = "itemslot-quiver.svg";
		}

		public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge) {
			return IsAcceptable(sourceSlot) && base.CanTakeFrom(sourceSlot, priority);
		}

		public override bool CanHold(ItemSlot sourceSlot) {
			return IsAcceptable(sourceSlot) && base.CanHold(sourceSlot);
		}

		private bool IsAcceptable(ItemSlot sourceSlot) {
			var collectible = sourceSlot?.Itemstack?.Collectible;
			// Only allow arrows or other types of ammo.
			return sourceSlot?.Itemstack?.Item is ItemArrow || collectible.Attributes["projectile"].Exists;
		}
	}

	public class ItemSlotArcherHeal : ItemSlotArcher {
		public ItemSlotArcherHeal(InventoryArcher inventory) : base(inventory) {
			BackgroundIcon = "itemslot-bandage.svg";
		}

		public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge) {
			return IsAcceptable(sourceSlot) && base.CanTakeFrom(sourceSlot, priority);
		}

		public override bool CanHold(ItemSlot sourceSlot) {
			return IsAcceptable(sourceSlot) && base.CanHold(sourceSlot);
		}

		private bool IsAcceptable(ItemSlot sourceSlot) {
			var collectible = sourceSlot?.Itemstack?.Collectible;
			// Make sure the item is a healing item.
			return collectible?.Attributes?["healthByType"]?.Exists ?? false;
		}
	}
}