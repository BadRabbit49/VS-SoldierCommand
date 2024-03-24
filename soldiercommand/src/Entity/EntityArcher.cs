using Vintagestory.GameContent;
using Vintagestory.API.Util;
using Vintagestory.API.Server;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using System;
using System.IO;
using System.Linq;

namespace SoldierCommand {
	public class EntityArcher : EntityHumanoid {
		public EntityArcher() { AnimManager = new HumanAnimationManager(); }

		public InventoryArcher gearInv;
		public override IInventory GearInventory => gearInv;
		public override ItemSlot LeftHandItemSlot => gearInv[15];
		public override ItemSlot RightHandItemSlot => gearInv[16];
		public ItemSlot BackItemSlot => gearInv[17];
		public ItemSlot AmmoItemSlot => gearInv[18];
		public ItemSlot HealItemSlot => gearInv[19];
		protected virtual string inventoryId => "gear-" + EntityId;
		public InventoryDialog InventoryDialog { get; set; }
		public EntityTalkUtil talkUtil { get; set; }

		private BehaviorGearItems _behaviorGearItems;
		private BehaviorGearItems behaviorGearItems {
			get {
				if (_behaviorGearItems == null) {
					_behaviorGearItems = GetBehavior<BehaviorGearItems>();
				}
				return _behaviorGearItems;
			}
		}

		public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d) {
			base.Initialize(properties, api, InChunkIndex3d);
			var gearitems = GetBehavior<BehaviorGearItems>();
			// Initialize gear slots if not done yet.
			if (gearInv == null) {
				gearInv = new InventoryArcher(inventoryId, api);
				gearInv.SlotModified += GearInvSlotModified;
			} else {
				gearInv.LateInitialize(inventoryId, api);
			}
			// Register stuff for client-side api.
			if (api is ICoreClientAPI capi) {
				talkUtil = new EntityTalkUtil(capi, this);
			}
			// Register listeners if api is on server.
			if (api is ICoreServerAPI sapi) {
				WatchedAttributes.RegisterModifiedListener(gearitems.InventoryTreeKey, ReadInventoryFromAttributes);
			}
			ReadInventoryFromAttributes();
			// Apply healthboost based on armor values.
			if (api.Side == EnumAppSide.Server) {
				GetBehavior<EntityBehaviorHealth>().onDamaged += (dmg, dmgSource) => HealthUtility.handleDamaged(World.Api, this, dmg, dmgSource);
			}
		}

		public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player) {
			if ((player.GetGroup(WatchedAttributes.GetInt("groupUID")) != null || WatchedAttributes.GetString("ownerUID") == player.PlayerUID) && Alive) {
				return new WorldInteraction[] { new WorldInteraction() { MouseButton = EnumMouseButton.Right } };
			} else {
				return base.GetInteractionHelp(world, es, player);
			}
		}

		public override void OnTesselation(ref Shape entityShape, string shapePathForLogging) {
			base.OnTesselation(ref entityShape, shapePathForLogging);
			foreach (var slot in GearInventory) {
				addGearToShape(slot, entityShape, shapePathForLogging);
			}
		}

		public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data) {
			base.OnReceivedClientPacket(player, packetid, data);
			// Opening and Closing inventory packets.
			if (packetid == 1501) {
				player.InventoryManager.OpenInventory(GearInventory);
			}
			if (packetid == 1502) {
				player.InventoryManager.CloseInventory(GearInventory);
			}
		}

		public override void OnReceivedServerPacket(int packetid, byte[] data) {
			base.OnReceivedServerPacket(packetid, data);
			if (packetid == 1500) {
				TreeAttribute tree = new TreeAttribute();
				SerializerUtil.FromBytes(data, (r) => tree.FromBytes(r));
				gearInv.FromTreeAttributes(tree);
				foreach (var slot in gearInv) {
					slot.OnItemSlotModified(slot.Itemstack);
				}
			}
			if (packetid == 1502) {
				(World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(GearInventory);
				InventoryDialog?.TryClose();
			}
		}

		public override bool ShouldReceiveDamage(DamageSource damageSource, float damage) {
			if (damageSource.CauseEntity is EntityPlayer) {
				EntityPlayer attacker = (EntityPlayer)damageSource.CauseEntity;
				// We must have everything to do the thing or something...
				if (HasBehavior<BehaviorGearItems>()) {
					bool FriendlyFire = Api.World.Config.GetAsBool("FriendlyFireG");
					// Don't let the entity take damage from other players within the group if friendly fire for groups is turned off.
					if (attacker != behaviorGearItems.cachedOwner && attacker.Player.GetGroup(behaviorGearItems.cachedGroup.GroupUid) != null && !FriendlyFire) {
						return false;
					}
					// Don't let the entity take damage from its owner if friendly fire for owners is turned off. But do if they are sneaking.
					if (attacker == behaviorGearItems.cachedOwner && attacker.ServerControls.Sneak && !FriendlyFire) {
						return false;
					}
				}
			}
			return true;
		}

		public override string GetInfoText() {
			try {
				string returnText = base.GetInfoText();
				if (behaviorGearItems.cachedGroup != null) {
					// Why can't I get the name of the group here!?
					returnText = string.Concat(returnText, "\n", Lang.Get("soldiercommand:gui-profile-group"), behaviorGearItems.cachedGroup.GroupName);
				}
				if (behaviorGearItems.cachedOwner != null) {
					returnText = string.Concat(returnText, "\n", Lang.Get("soldiercommand:gui-profile-owner"), behaviorGearItems.cachedOwner.PlayerName);
				}
				returnText = string.Concat(returnText, "\n", Lang.Get("soldiercommand:gui-enlistment-" + behaviorGearItems.enlistedStatus.ToString().ToLower()));
				if (behaviorGearItems.enlistedStatus != EnlistedStatus.CIVILIAN) {
					returnText = string.Concat(returnText, "\n", Lang.Get("soldiercommand:gui-specialist-" + behaviorGearItems.specialization.ToString().ToLower()));
				}
				return returnText;
			} catch {
				// Womp Womp.
				return string.Concat(base.GetInfoText());
			}
		}

		public override void FromBytes(BinaryReader reader, bool forClient) {
			base.FromBytes(reader, forClient);
			if (gearInv == null) {
				gearInv = new InventoryArcher(Code.Path, "gearInv-" + EntityId, null);
			}
			gearInv.FromTreeAttributes(GetInventoryTree());
		}

		public override void ToBytes(BinaryWriter writer, bool forClient) {
			// Save as much as possible, but ignore anything if it catches a null reference exception.
			try { gearInv.ToTreeAttributes(GetInventoryTree()); } catch (NullReferenceException) { }
			base.ToBytes(writer, forClient);
		}

		public ITreeAttribute GetInventoryTree() {
			if (!WatchedAttributes.HasAttribute("inventory")) {
				ITreeAttribute tree = new TreeAttribute();
				gearInv.ToTreeAttributes(tree);
				WatchedAttributes.SetAttribute("inventory", tree);
			}
			return WatchedAttributes.GetTreeAttribute("inventory");
		}

		public void GearInvSlotModified(int slotId) {
			ITreeAttribute tree = new TreeAttribute();
			WatchedAttributes["inventory"] = tree;
			gearInv.ToTreeAttributes(tree);
			WatchedAttributes.MarkPathDirty("inventory");
			// If on server-side, not client, sent the packetid on the channel.
			if (Api is ICoreServerAPI sapi) {
				sapi.Network.BroadcastEntityPacket(EntityId, 1500, SerializerUtil.ToBytes((w) => tree.ToBytes(w)));
			}
		}

		public virtual void ToggleInventoryDialog(IPlayer player) {
			if (Api.Side != EnumAppSide.Client) {
				return;
			} else {
				var capi = (ICoreClientAPI)Api;
				if (InventoryDialog == null) {
					InventoryDialog = new InventoryDialog(gearInv, this, capi);
					InventoryDialog.OnClosed += OnInventoryDialogClosed;
				}
				if (!InventoryDialog.TryOpen()) {
					return;
				}
				player.InventoryManager.OpenInventory(GearInventory);
				capi.Network.SendEntityPacket(EntityId, 1501);
				return;
			}
		}

		public virtual void OnInventoryDialogClosed() {
			var capi = (ICoreClientAPI)Api;
			capi.World.Player.InventoryManager.CloseInventory(GearInventory);
			capi.Network.SendEntityPacket(EntityId, 1502);
			InventoryDialog?.Dispose();
			InventoryDialog = null;
		}

		public void ReadInventoryFromAttributes() {
			ITreeAttribute treeAttribute = WatchedAttributes["inventory"] as ITreeAttribute;
			if (gearInv != null && treeAttribute != null) {
				gearInv.FromTreeAttributes(treeAttribute);
			}
			(Properties.Client.Renderer as EntitySkinnableShapeRenderer)?.MarkShapeModified();
		}
	}
}