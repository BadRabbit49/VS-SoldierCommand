using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using System;

namespace SoldierCommand {
	public class InventoryDialog : GuiDialog {
		protected InventoryArcher inv;
		protected EntityArcher owningEntity;
		protected Vec3d entityPos = new Vec3d();

		protected double FloatyDialogPosition => 0.6;
		protected double FloatyDialogAlign => 0.8;
		protected bool IsInRangeOfEntity => capi.World.Player.Entity.Pos.XYZ.Add(capi.World.Player.Entity.LocalEyePos).SquareDistanceTo(entityPos) <= Math.Pow(capi.World.Player.WorldData.PickingRange, 2);
		public override double DrawOrder => 0.2;
		public override bool UnregisterOnClose => true;
		public override bool PrefersUngrabbedMouse => false;
		public override bool DisableMouseGrab => false;
		public override string ToggleKeyCombinationCode => null;

		public InventoryDialog(InventoryArcher inv, EntityArcher ent, ICoreClientAPI capi) : base(capi) {
			this.inv = inv;
			this.owningEntity = ent;

			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;

			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0.0);

			double pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
			
			ElementBounds armourSlotBoundsHead = ElementStdBounds.SlotGrid(EnumDialogArea.None, 000.0, 020.0 + pad, 1, 1).FixedGrow(0.0, pad);
			ElementBounds armourSlotBoundsBody = ElementStdBounds.SlotGrid(EnumDialogArea.None, 000.0, 122.0 + pad, 1, 1).FixedGrow(0.0, pad);
			ElementBounds armourSlotBoundsLegs = ElementStdBounds.SlotGrid(EnumDialogArea.None, 000.0, 224.0 + pad, 1, 1).FixedGrow(0.0, pad);

			ElementBounds clothingsSlotsBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 000.0, 020.0 + pad, 1, 6).FixedGrow(0.0, pad);
			ElementBounds accessorySlotsBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 000.0, 020.0 + pad, 1, 6).FixedGrow(0.0, pad);

			ElementBounds rightSlotBoundsLHand = ElementStdBounds.SlotGrid(EnumDialogArea.None, 000.0, 020.0 + pad, 1, 1).FixedGrow(0.0, pad);
			ElementBounds rightSlotBoundsRHand = ElementStdBounds.SlotGrid(EnumDialogArea.None, 000.0, 071.0 + pad, 1, 1).FixedGrow(0.0, pad);
			ElementBounds backpackOnSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 000.0, 122.0 + pad, 1, 1).FixedGrow(0.0, pad);

			ElementBounds munitionsSlotsBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 000.0, 173.0 + pad, 1, 6).FixedGrow(0.0, pad);
			ElementBounds healthitmSlotsBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 000.0, 224.0 + pad, 1, 6).FixedGrow(0.0, pad);

			clothingsSlotsBounds.FixedRightOf(armourSlotBoundsHead, 10.0).FixedRightOf(armourSlotBoundsBody, 10.0).FixedRightOf(armourSlotBoundsLegs, 10.0);
			accessorySlotsBounds.FixedRightOf(clothingsSlotsBounds, 10.0);
			clothingsSlotsBounds.fixedHeight -= 6.0;
			accessorySlotsBounds.fixedHeight -= 6.0;
			backpackOnSlotBounds.FixedRightOf(accessorySlotsBounds, 10.0);
			rightSlotBoundsLHand.FixedRightOf(accessorySlotsBounds, 10.0);
			rightSlotBoundsRHand.FixedRightOf(accessorySlotsBounds, 10.0);
			munitionsSlotsBounds.FixedRightOf(accessorySlotsBounds, 10.0);
			healthitmSlotsBounds.FixedRightOf(accessorySlotsBounds, 10.0);

			SingleComposer = capi.Gui.CreateCompo("archercontents" + owningEntity.EntityId, dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(GetDialogName(ent), onClose: OnTitleBarClose);
			SingleComposer.BeginChildElements(bgBounds);

			SingleComposer
				.AddItemSlotGrid(inv, SendInvPacket, 1, new int[1] { InventoryArcher.HeadArmorSlotId }, armourSlotBoundsHead, "armorSlotsHead")
				.AddItemSlotGrid(inv, SendInvPacket, 1, new int[1] { InventoryArcher.BodyArmorSlotId }, armourSlotBoundsBody, "armorSlotsBody")
				.AddItemSlotGrid(inv, SendInvPacket, 1, new int[1] { InventoryArcher.LegsArmorSlotId }, armourSlotBoundsLegs, "armorSlotsLegs");
			SingleComposer
				.AddItemSlotGrid(inv, SendInvPacket, 1, InventoryArcher.ClothingsSlotIds, clothingsSlotsBounds, "clothingsSlots")
				.AddItemSlotGrid(inv, SendInvPacket, 1, InventoryArcher.AccessorySlotIds, accessorySlotsBounds, "accessorySlots");
			SingleComposer
				.AddItemSlotGrid(inv, SendInvPacket, 1, new int[1] { InventoryArcher.LHandItemSlotId }, rightSlotBoundsLHand, "otherSlotsLHnd")
				.AddItemSlotGrid(inv, SendInvPacket, 1, new int[1] { InventoryArcher.RHandItemSlotId }, rightSlotBoundsRHand, "otherSlotsRHnd")
				.AddItemSlotGrid(inv, SendInvPacket, 1, new int[1] { InventoryArcher.BPackItemSlotId }, backpackOnSlotBounds, "otherSlotsPack")
				.AddItemSlotGrid(inv, SendInvPacket, 1, new int[1] { InventoryArcher.MunitionsSlotId }, munitionsSlotsBounds, "otherSlotsAmmo")
				.AddItemSlotGrid(inv, SendInvPacket, 1, new int[1] { InventoryArcher.HealthItmSlotId }, healthitmSlotsBounds, "otherSlotsHeal")
				.EndChildElements();
			SingleComposer.Compose();
		}

		public string GetDialogName(EntityArcher ent) {
			// Get the name of the entity archer.
			string name = ent.GetName();
			// Ensure the returned name does not exceed the length of the original name.
			return name.Length > 30 ? name.Substring(0, 30) : name;
		}

		public override void OnFinalizeFrame(float dt) {
			base.OnFinalizeFrame(dt);
			entityPos = owningEntity.Pos.XYZ.Clone();
			entityPos.Add(owningEntity.SelectionBox.X2 - owningEntity.OriginSelectionBox.X2, 0.0, owningEntity.SelectionBox.Z2 - owningEntity.OriginSelectionBox.Z2);
			if (!IsInRangeOfEntity) {
				capi.Event.EnqueueMainThreadTask(delegate {
					TryClose();
				}, "closedarcherinvlog");
			}
		}

		public override void OnGuiClosed() {
			base.OnGuiClosed();
			capi.Network.SendPacketClient(capi.World.Player.InventoryManager.CloseInventory(inv));
			SingleComposer.GetSlotGrid("armorSlotsHead")?.OnGuiClosed(capi);
			SingleComposer.GetSlotGrid("armorSlotsBody")?.OnGuiClosed(capi);
			SingleComposer.GetSlotGrid("armorSlotsLegs")?.OnGuiClosed(capi);
			SingleComposer.GetSlotGrid("clothingsSlots")?.OnGuiClosed(capi);
			SingleComposer.GetSlotGrid("accessorySlots")?.OnGuiClosed(capi);
			SingleComposer.GetSlotGrid("otherSlotsLHnd")?.OnGuiClosed(capi);
			SingleComposer.GetSlotGrid("otherSlotsRHnd")?.OnGuiClosed(capi);
			SingleComposer.GetSlotGrid("otherSlotsPack")?.OnGuiClosed(capi);
			SingleComposer.GetSlotGrid("otherSlotsAmmo")?.OnGuiClosed(capi);
			SingleComposer.GetSlotGrid("otherSlotsHeal")?.OnGuiClosed(capi);
		}

		public override void OnRenderGUI(float deltaTime) {
			if (capi.Settings.Bool["immersiveMouseMode"]) {
				double offX = owningEntity.SelectionBox.X2 - owningEntity.OriginSelectionBox.X2;
				double offZ = owningEntity.SelectionBox.Z2 - owningEntity.OriginSelectionBox.Z2;
				Vec3d pos = MatrixToolsd.Project(new Vec3d(owningEntity.Pos.X + offX, owningEntity.Pos.Y + FloatyDialogPosition, owningEntity.Pos.Z + offZ), capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);
				if (pos.Z < 0.0) {
					return;
				}
				SingleComposer.Bounds.Alignment = EnumDialogArea.None;
				SingleComposer.Bounds.fixedOffsetX = 0.0;
				SingleComposer.Bounds.fixedOffsetY = 0.0;
				SingleComposer.Bounds.absFixedX = pos.X - SingleComposer.Bounds.OuterWidth / 2.0;
				SingleComposer.Bounds.absFixedY = (double)capi.Render.FrameHeight - pos.Y - SingleComposer.Bounds.OuterHeight * FloatyDialogAlign;
				SingleComposer.Bounds.absMarginX = 0.0;
				SingleComposer.Bounds.absMarginY = 0.0;
			}
			base.OnRenderGUI(deltaTime);
		}

		protected void OnTitleBarClose() {
			TryClose();
		}

		protected void SendInvPacket(object packet) {
			capi.Network.SendPacketClient(packet);
		}
	}
}