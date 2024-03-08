using Vintagestory.GameContent;
using Vintagestory.API.Util;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using System;
using System.IO;
using System.Linq;
using Vintagestory.API.Config;

namespace SoldierCommand {
	public class EntityArcher : EntityHumanoid {
		public EntityArcher() { 
			AnimManager = new HumanAnimationManager();
			Stats
				.Register("ownerID")
				.Register("groupID")
				.Register("");
		}
		
		protected InventoryArcher gearInv;
		protected InventoryArcher headInv;

		public override IInventory GearInventory => gearInv;
		public IInventory HeadInventory => headInv;
		// Item Slots relating to the actual gear inventory of the entity.
		public override ItemSlot LeftHandItemSlot => gearInv[15];
		public override ItemSlot RightHandItemSlot => gearInv[16];
		public ItemSlot BackItemSlot => gearInv[17];
		public ItemSlot AmmoItemSlot => gearInv[18];
		public ItemSlot HealItemSlot => gearInv[19];
		// Item Slots relating to the head, face, and accessories slots.
		public ItemSlot FacesSlot => headInv[0];
		public ItemSlot HairsSlot => headInv[1];
		public ItemSlot ExtraSlot => headInv[2];
		public ItemSlot BeardSlot => headInv[3];

		public EntityTalkUtil talkUtil { get; set; }

		protected InventoryDialog InventoryDialog { get; set; }

		protected const string InventoryTreeKey = "inventory";
		protected const string HumanHeadTreeKey = "humanhead";
		protected const string PlayerUIDTreeKey = "ownerLoyalty";
		protected const string GroupsUIDTreeKey = "groupLoyalty";

		protected virtual string inventoryId => "gear-" + EntityId;
		protected virtual string humanheadId => "head-" + EntityId;

		protected const int packetId_OpenInventory = 1000;
		protected const int packetId_CloseInventory = 1001;

		// Friend & Foe Identifiers.
		public IPlayer ownerINT;
		public string ownerUID;
		public int groupUID;

		// Stats here for stuff like walkspeed, damage modifiers, and so on.
		public float walkSpeed = 1f;
		public float swimSpeed = 1f;

		public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d) {
			base.Initialize(properties, api, InChunkIndex3d);
			// Initialize gear slots if not done yet.
			if (gearInv == null) {
				gearInv = new InventoryArcher(inventoryId, api);
				gearInv.SlotModified += GearInvSlotModified;
			} else {
				gearInv.LateInitialize(inventoryId, api);
			}
			// Initialize head slots if not done yet.
			if (headInv == null) {
				headInv = new InventoryArcher(humanheadId, api);
				headInv.SlotModified += GearInvSlotModified;
			} else {
				headInv.LateInitialize(humanheadId, api);
			}
			// Register stuff for client-side api.
			if (api is ICoreClientAPI capi) {
				talkUtil = new EntityTalkUtil(capi, this);
			}
			// Register inventory and slot listeners.
			if (api is ICoreServerAPI) {
				WatchedAttributes.RegisterModifiedListener(InventoryTreeKey, readInventoryFromAttributes);
				WatchedAttributes.RegisterModifiedListener(HumanHeadTreeKey, readInventoryFromAttributes);
				WatchedAttributes.RegisterModifiedListener(PlayerUIDTreeKey, readInventoryFromAttributes);
				WatchedAttributes.RegisterModifiedListener(GroupsUIDTreeKey, readInventoryFromAttributes);
			}
			readInventoryFromAttributes();
			// Apply healthboost based on armor values.
			if (api.Side == EnumAppSide.Server) {
				GetBehavior<EntityBehaviorHealth>().onDamaged += (dmg, dmgSource) => HealthUtility.handleDamaged(World.Api,this, dmg, dmgSource);
			}
			// Task handling here.
			ICoreServerAPI sapi = (ICoreServerAPI)(object)((api is ICoreServerAPI) ? api : null);
			if (sapi != null) {
				(sapi.World).RegisterGameTickListener(delegate { HandleTasksOutOfCombat(); }, 10000, 10000);
			}
		}

		public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode) {
			base.OnInteract(byEntity, slot, hitPosition, mode);
			if (Alive && byEntity is EntityPlayer player && mode == EnumInteractMode.Interact) {
				// While a STRANGER has something in their ACTIVE SLOT.
				if (ownerUID == null && slot.Itemstack != null) {
					// If the entity isn't already owned, giving it some kind of currency will hire it on to join.
					if (slot.Itemstack.ItemAttributes["currency"].Exists && !player.Controls.Sneak) {
						slot.TakeOut(1);
						ownerUID = player.PlayerUID;
						Attributes.SetString("ownerLoyalty", player.PlayerUID);
						// If the owner also is in a group then go ahead and join that too.
						if (player.Player.GetGroups().Length > 0) {
							groupUID = player.Player.GetGroups().ElementAt(0).GroupUid;
							Attributes.SetInt("groupLoyalty", player.Player.GetGroups().ElementAt(0).GroupUid);
						}
					}
				}
				// While the OWNER is SNEAKING with an EMPTY SLOT open inventory dialogbox.
				if (ownerUID == player.PlayerUID && player.Controls.Sneak && slot.Empty) {
					ToggleInventoryDialog(player.Player);
				}
			}
		}

		public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player) {
			if (WatchedAttributes.GetString("ownerLoyalty") == player.PlayerUID && Alive) {
				return new WorldInteraction[] {
					new WorldInteraction() {
						MouseButton = EnumMouseButton.Right
					}
				};
			} else {
				return base.GetInteractionHelp(world, es, player);
			}
		}

		public override void OnEntityLoaded() {
			base.OnEntityLoaded();
			// If the entity has a group it already belongs to then set it.
			if (WatchedAttributes.HasAttribute("groupLoyalty")) {
				groupUID = WatchedAttributes.GetAsInt("groupLoyalty");
			}
			// If the entity has an owner it already belongs to then set it.
			if (WatchedAttributes.HasAttribute("ownerLoyalty")) {
				ownerUID = WatchedAttributes.GetAsString("ownerLoyalty");
				IPlayer[] playersOnline = World.AllOnlinePlayers;
				// Go through all the players. If the owner is on, refresh to the owner's primary group.
				foreach (IPlayer player in playersOnline) {
					if (player.PlayerUID == ownerUID) {
						groupUID = player.GetGroups().ElementAt(0).GroupUid;
						WatchedAttributes.SetInt("groupLoyalty", player.GetGroups().ElementAt(0).GroupUid);
						ownerINT = player;
					}
				}
			}
		}

		public override void OnEntitySpawn() {
			base.OnEntitySpawn();
			// If there is a player within a reasonable range of spawn, make them the owner.
			EntityPlayer player = AiUtility.PlayerWithinRangeOfPos(ServerPos.XYZ, 10, World);
			if (player != null) {
				ownerUID = player.PlayerUID;
				WatchedAttributes.SetString("ownerLoyalty", ownerUID);
				if (player.Player.GetGroups().Length > 0) {
					groupUID = player.Player.GetGroups().ElementAt(0).GroupUid;
					WatchedAttributes.SetInt("groupLoyalty", groupUID);
				}
			}
		}

		public override void OnEntityDespawn(EntityDespawnData despawn) {
			base.OnEntityDespawn(despawn);
			InventoryDialog?.TryClose();
		}

		public override void OnTesselation(ref Shape entityShape, string shapePathForLogging) {
			base.OnTesselation(ref entityShape, shapePathForLogging);
			foreach (var slot in GearInventory) {
				addGearToShape(slot, entityShape, shapePathForLogging);
			}
			foreach (var slot in HeadInventory) {
				addGearToShape(slot, entityShape, shapePathForLogging);
			}
		}

		public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data) {
			base.OnReceivedClientPacket(player, packetid, data);
			switch (packetid) {
				case packetId_OpenInventory: player.InventoryManager.OpenInventory(GearInventory); break;
				case packetId_CloseInventory: player.InventoryManager.CloseInventory(GearInventory); break;
				default: break;
			}
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
			if (packetid == packetId_CloseInventory) {
				(World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(GearInventory);
				InventoryDialog?.TryClose();
			}
		}

		public override bool ShouldReceiveDamage(DamageSource damageSource, float damage) {
			if (damageSource.CauseEntity is EntityPlayer) {
				EntityPlayer attacker = (EntityPlayer)damageSource.CauseEntity;
				// Don't let the entity take damage from other players within the group if friendly fire for groups is turned off.
				if (attacker.PlayerUID != ownerUID && attacker.Player.GetGroup(groupUID) != null && !SoldierConfig.Current.FriendlyFireG) {
					return false;
				}
				// Don't let the entity take damage from its owner if friendly fire for owners is turned off. But do if they are sneaking.
				if (attacker.PlayerUID == ownerUID && attacker.ServerControls.Sneak && !SoldierConfig.Current.FriendlyFireG) {
					return false;
				}
			}
			return true;
		}

		public override double GetWalkSpeedMultiplier(double groundDragFactor = 0.3) {
			double mul = base.GetWalkSpeedMultiplier(groundDragFactor);
			// Apply walk speed modifiers from armor, etc
			mul *= GameMath.Clamp(walkSpeed, 0, 999);
			if (!servercontrols.Sneak) {
				mul *= GlobalConstants.SneakSpeedMultiplier;
			}

			return mul;
		}

		public override string GetInfoText() {
			if (ownerINT != null && groupUID != 0) {
				return string.Concat(base.GetInfoText(), "\n", Lang.Get("gui-archer-group", ownerINT.GetGroup(groupUID).GroupName));
			} else if (ownerINT != null) {
				return string.Concat(base.GetInfoText(), "\n", Lang.Get("gui-archer-owner", ownerINT?.PlayerName));
			} else {
				return string.Concat(base.GetInfoText());
			}
		}

		// READ AND WRITE BYTES
		public override void FromBytes(BinaryReader reader, bool forClient) {
			base.FromBytes(reader, forClient);
			ITreeAttribute tree = WatchedAttributes["inventory"] as ITreeAttribute;
			ITreeAttribute face = WatchedAttributes["humanhead"] as ITreeAttribute;
			if (gearInv == null) {
				gearInv = new InventoryArcher(Code.Path, "gearInv-" + EntityId, null);
			}
			if (headInv == null) {
				headInv = new InventoryArcher(Code.Path, "headInv-" + EntityId, null);
			}
			gearInv.FromTreeAttributes(getInventoryTree());
			headInv.FromTreeAttributes(getHumanHeadTree());
		}

		public override void ToBytes(BinaryWriter writer, bool forClient) {
			// Save as much as possible, but ignore anything if it catches a null reference exception.
			try { gearInv.ToTreeAttributes(getInventoryTree()); } catch (NullReferenceException) { }
			try { headInv.ToTreeAttributes(getHumanHeadTree()); } catch (NullReferenceException) { }
			base.ToBytes(writer, forClient);
		}

		protected virtual void ToggleInventoryDialog(IPlayer player) {
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
				capi.Network.SendEntityPacket(EntityId, packetId_OpenInventory);
				return;
			}
		}

		protected virtual void OnInventoryDialogClosed() {
			var capi = (ICoreClientAPI)Api;
			capi.World.Player.InventoryManager.CloseInventory(GearInventory);
			capi.Network.SendEntityPacket(EntityId, packetId_CloseInventory);
			InventoryDialog?.Dispose();
			InventoryDialog = null;
		}

		public void HandleTasksOutOfCombat() {
			AiTaskManager taskManager = GetBehavior<EntityBehaviorTaskAI>().TaskManager;
			AiTaskSoldierMeleeAttack task = taskManager.GetTask<AiTaskSoldierMeleeAttack>();
			if (task != null) {
				Entity targetEntity = task.TargetEntity;
				if (((targetEntity != null) ? new bool?(targetEntity.Alive) : null) == true) {
					return;
				}
			}
			AiTaskSoldierSeeksEntity task2 = taskManager.GetTask<AiTaskSoldierSeeksEntity>();
			if (task2 != null) {
				Entity targetEntity2 = task2.TargetEntity;
				if (((targetEntity2 != null) ? new bool?(targetEntity2.Alive) : null) == true) {
					return;
				}
			}
			AiTaskSoldierRangeAttack task3 = taskManager.GetTask<AiTaskSoldierRangeAttack>();
			if (task3 != null) {
				Entity targetEntity3 = task3.TargetEntity;
				if (((targetEntity3 != null) ? new bool?(targetEntity3.Alive) : null) == true) {
					return;
				}
			}
		}

		// EQUIPMENT FUNCTIONS
		private void GearInvSlotModified(int slotId) {
			ITreeAttribute tree = new TreeAttribute();
			// WatchedAttributes["inventory"] = tree;
			gearInv.ToTreeAttributes(tree);
			WatchedAttributes.MarkPathDirty("inventory");
			// If on server-side, not client, sent the packetid on the channel.
			if (Api is ICoreServerAPI sapi) {
				sapi.Network.BroadcastEntityPacket(EntityId, 1235, SerializerUtil.ToBytes((w) => tree.ToBytes(w)));
			}
		}

		private void readInventoryFromAttributes() {
			ITreeAttribute treeAttribute = WatchedAttributes["inventory"] as ITreeAttribute;
			if (gearInv != null && treeAttribute != null) {
				gearInv.FromTreeAttributes(treeAttribute);
			}
			(Properties.Client.Renderer as EntitySkinnableShapeRenderer)?.MarkShapeModified();
		}

		private void readHumanHeadFromAttributes() {
			ITreeAttribute treeAttribute = WatchedAttributes["humanhead"] as ITreeAttribute;
			if (gearInv != null && treeAttribute != null) {
				gearInv.FromTreeAttributes(treeAttribute);
			}
			(Properties.Client.Renderer as EntitySkinnableShapeRenderer)?.MarkShapeModified();
		}

		private ITreeAttribute getInventoryTree() {
			if (!WatchedAttributes.HasAttribute("inventory")) {
				ITreeAttribute tree = new TreeAttribute();
				gearInv.ToTreeAttributes(tree);
				WatchedAttributes.SetAttribute("inventory", tree);
			}
			return WatchedAttributes.GetTreeAttribute("inventory");
		}

		private ITreeAttribute getHumanHeadTree() {
			if (!WatchedAttributes.HasAttribute("humanhead")) {
				ITreeAttribute tree = new TreeAttribute();
				headInv.ToTreeAttributes(tree);
				WatchedAttributes.SetAttribute("humanhead", tree);
			}
			return WatchedAttributes.GetTreeAttribute("humanhead");
		}
	}
}