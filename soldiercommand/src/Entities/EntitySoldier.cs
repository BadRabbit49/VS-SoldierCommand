using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.API.Util;
using System;
using System.IO;
using Vintagestory.API.Server;

namespace SoldierCommand {
	public class EntitySoldier : EntityAgent {
		protected InventorySoldier gearInv;
		public override ItemSlot LeftHandItemSlot => gearInv.LeftHandItemSlot;
		public override ItemSlot RightHandItemSlot => gearInv.RightHandItemSlot;
		public override IInventory GearInventory => gearInv;
		protected virtual string inventoryId => "gearInv-" + EntityId;

		public EntitySoldier() {
			AnimManager = new PlayerAnimationManager();
		}

		public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d) {
			base.Initialize(properties, api, InChunkIndex3d);
			if (gearInv == null) {
				gearInv = new InventorySoldier(Code.Path, "gearInv-" + EntityId, api);
			} else {
				gearInv.Api = api;
			}
			gearInv.LateInitialize(gearInv.InventoryID, api);
			var slots = new ItemSlot[gearInv.Count];

			for (int i = 0; i < gearInv.Count; i++) {
				slots[i] = gearInv[i];
			}
			if (api.Side == EnumAppSide.Server) {
				GetBehavior<EntityBehaviorHealth>().onDamaged += (dmg, dmgSource) => applySoldierArmor(dmg, dmgSource);
			}
		}

		public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode) {
			base.OnInteract(byEntity, slot, hitPosition, mode);
			if (Alive && byEntity is EntityPlayer player && WatchedAttributes.GetString("guardedPlayerUid") == player.PlayerUID && mode == EnumInteractMode.Interact && !player.Controls.Sneak) {
				bool commandSit = WatchedAttributes.GetBool("commandSit", false);
				WatchedAttributes.SetBool("commandSit", !commandSit);
				WatchedAttributes.MarkPathDirty("commandSit");
			}
		}

		public override void OnEntitySpawn() {
			base.OnEntitySpawn();
		}

		public override void OnGameTick(float dt) {
			base.OnGameTick(dt);
		}

		public override void OnTesselation(ref Shape entityShape, string shapePathForLogging) {
			base.OnTesselation(ref entityShape, shapePathForLogging);
			foreach (var slot in GearInventory) {
				addGearToShape(slot, entityShape, shapePathForLogging);
			}

			AnimationCache.ClearCache(Api, this);
			base.OnTesselation(ref entityShape, shapePathForLogging);
		}

		public override void OnReceivedServerPacket(int packetid, byte[] data) {
			base.OnReceivedServerPacket(packetid, data);
			if (packetid == 1235) {
				TreeAttribute tree = new TreeAttribute();
				SerializerUtil.FromBytes(data, (r) => tree.FromBytes(r));
				gearInv.FromTreeAttributes(tree);
				foreach (var slot in gearInv) {
					slot.OnItemSlotModified(slot.Itemstack);
				}
			}
		}

		public override void FromBytes(BinaryReader reader, bool forClient) {
			base.FromBytes(reader, forClient);

			if (gearInv == null) {
				gearInv = new InventorySoldier(Code.Path, "gearInv-" + EntityId, null);
			}
			gearInv.FromTreeAttributes(getInventoryTree());
		}

		public override void ToBytes(BinaryWriter writer, bool forClient) {
			try {
				gearInv.ToTreeAttributes(getInventoryTree());
			} catch (NullReferenceException) {}
			base.ToBytes(writer, forClient);
		}

		public void DropInventoryOnGround() {
			for (int i = gearInv.Count - 1; i >= 0; i--) {
				if (gearInv[i].Empty) {
					continue;
				}
				Api.World.SpawnItemEntity(gearInv[i].TakeOutWhole(), ServerPos.XYZ);
				gearInv.MarkSlotDirty(i);
			}
		}

		private ITreeAttribute getInventoryTree() {
			if (!WatchedAttributes.HasAttribute("soldierinventory")) {
				ITreeAttribute tree = new TreeAttribute();
				gearInv.ToTreeAttributes(tree);
				WatchedAttributes.SetAttribute("soldierinventory", tree);
			}
			return WatchedAttributes.GetTreeAttribute("soldierinventory");
		}

		private float applySoldierArmor(float dmg, DamageSource dmgSource) {
			if (dmgSource.SourceEntity != null && dmgSource.Type != EnumDamageType.Heal) {
				foreach (var slot in GearInventory) {
					if (!slot.Empty) {
						dmg *= 1 - (slot.Itemstack.Item as ItemWearable).ProtectionModifiers.RelativeProtection;
					}
				}
			}
			return dmg;
		}

		public void UpdateShape(ItemSlot itemToEquip, ref Shape entityShape, string shapePathForLogging) {
			entityShape = addGearToShape(itemToEquip, entityShape, shapePathForLogging);
			AnimationCache.ClearCache(Api, this);
			return;
		}
	}
}