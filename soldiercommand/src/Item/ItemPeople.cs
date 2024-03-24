using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace SoldierCommand;

public class ItemPeople : Item {
	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity) {
		return null;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling) {
		if (blockSel == null) {
			return;
		}

		IPlayer player = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID);
		if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak)) {
			return;
		}

		if (!(byEntity is EntityPlayer) || player.WorldData.CurrentGameMode != EnumGameMode.Creative) {
			slot.TakeOut(1);
			slot.MarkDirty();
		}

		AssetLocation assetLocation = new AssetLocation(Code.Domain, CodeEndWithoutParts(1));
		EntityProperties entityType = byEntity.World.GetEntityType(assetLocation);
		if (entityType == null) {
			byEntity.World.Logger.Error("ItemHuman: No such entity - {0}", assetLocation);
			if (api.World.Side == EnumAppSide.Client) {
				(api as ICoreClientAPI).TriggerIngameError(this, "nosuchentity", $"No such entity loaded - '{assetLocation}'.");
			}
			return;
		}

		Entity entity = byEntity.World.ClassRegistry.CreateEntity(entityType);
		if (entity == null) {
			return;
		}

		entity.ServerPos.X = (float)(blockSel.Position.X + ((!blockSel.DidOffset) ? blockSel.Face.Normali.X : 0)) + 0.5f;
		entity.ServerPos.Y = blockSel.Position.Y + ((!blockSel.DidOffset) ? blockSel.Face.Normali.Y : 0);
		entity.ServerPos.Z = (float)(blockSel.Position.Z + ((!blockSel.DidOffset) ? blockSel.Face.Normali.Z : 0)) + 0.5f;
		entity.ServerPos.Yaw = byEntity.Pos.Yaw + MathF.PI + MathF.PI / 2f;
		entity.Pos.SetFrom(entity.ServerPos);
		entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
		entity.Attributes.SetString("origin", "playerplaced");
		JsonObject attributes = Attributes;
		if (attributes != null && attributes.IsTrue("setGuardedEntityAttribute")) {
			entity.WatchedAttributes.SetLong("guardedEntityId", byEntity.EntityId);
			if (byEntity is EntityPlayer entityPlayer) {
				entity.WatchedAttributes.SetString("guardedPlayerUid", entityPlayer.PlayerUID);
			}
		}
		try {
			BlockEntity blockEnt = api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
			// If placed on a brazier soldier post then set that to be their post.
			if (blockEnt is BlockEntityPost post) {
				post.IgnitePost();
				post.IsCapacity(entity.EntityId);
			}
		} catch { }
		byEntity.World.SpawnEntity(entity);
		handHandling = EnumHandHandling.PreventDefaultAction;
	}

	public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity byEntity, EnumHand hand) {
		EntityProperties entityType = byEntity.World.GetEntityType(new AssetLocation(Code.Domain, CodeEndWithoutParts(1)));
		if (entityType == null) {
			return base.GetHeldTpIdleAnimation(activeHotbarSlot, byEntity, hand);
		}
		if (Math.Max(entityType.CollisionBoxSize.X, entityType.CollisionBoxSize.Y) > 1f) {
			return "holdunderarm";
		}
		return "holdbothhands";
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot) {
		return new WorldInteraction[1] {
			new WorldInteraction {
				ActionLangCode = "heldhelp-place",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
	}
}