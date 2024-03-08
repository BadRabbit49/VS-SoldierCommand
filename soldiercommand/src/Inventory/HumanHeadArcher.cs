using Vintagestory.API.Common;

namespace SoldierCommand {
	public class HumandHeadArcher : InventoryGeneric {
		public const int FacesSlotId = 0;
		public const int HairsSlotId = 1;
		public const int ExtraSlotId = 2;
		public const int BeardSlotId = 3;
		public ItemSlot FacesItemSlot => this[0];
		public ItemSlot HairsItemSlot => this[1];
		public ItemSlot ExtraItemSlot => this[2];
		public ItemSlot BeardItemSlot => this[3];
		public override ItemSlot this[int slotId] { get => slots[slotId]; set => slots[slotId] = value; }
		public override int Count => slots.Length;
		public HumandHeadArcher(string className, string instanceId, ICoreAPI api) : base(4, className, instanceId, api) { }
		public HumandHeadArcher(string invId, ICoreAPI api) : base(4, invId, api) { }

		public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge) {
			return (!isMerge) ? (baseWeight + 1f) : (baseWeight + 3f);
		}

		protected override ItemSlot NewSlot(int slotId) {
			switch (slotId) {
				case 15:
					return new ItemSlotArcherHand(this, slotId);
				case 16:
					return new ItemSlotArcherHand(this, slotId);
				case 17:
					return new ItemSlotArcherBack(this);
				case 18:
					return new ItemSlotArcherAmmo(this);
				case 19:
					return new ItemSlotArcherHeal(this);
				default:
					return new ItemSlotArcherWear(this, (EnumCharacterDressType)slotId, slotId);
			}
		}
	}
}