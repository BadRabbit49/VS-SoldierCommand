using Vintagestory.GameContent;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using System;
using System.Linq;
using System.Reflection;

namespace SoldierCommand {
	public enum EnlistedStatus { CIVILIAN, ENLISTED, DESERTER }
	public enum Specialization { EMPTY, MELEE, RANGE, BANNR, MEDIC }
	public enum CurrentCommand { WANDER, FOLLOW, RETURN, ATTACK, DEFEND, REVIVE }
	
	public class BehaviorGearItems : EntityBehavior {
		public BehaviorGearItems(Entity entity) : base(entity) { }

		/** DEFINE ATTRIBUTE TREES **/
		public ITreeAttribute specialty {
			get {
				if (entity.WatchedAttributes.GetTreeAttribute("specialty") == null) {
					entity.WatchedAttributes.SetAttribute("specialty", new TreeAttribute());
				}
				return entity.WatchedAttributes.GetTreeAttribute("specialty");
			}
			set {
				entity.WatchedAttributes.SetAttribute("specialty", value);
				entity.WatchedAttributes.MarkPathDirty("specialty");
			}
		}

		public ITreeAttribute loyalties {
			get {
				if (entity.WatchedAttributes.GetTreeAttribute("loyalties") == null) {
					entity.WatchedAttributes.SetAttribute("loyalties", new TreeAttribute());
				}
				return entity.WatchedAttributes.GetTreeAttribute("loyalties");
			}
			set {
				entity.WatchedAttributes.SetAttribute("loyalties", value);
				entity.WatchedAttributes.MarkPathDirty("loyalties");
			}
		}

		/** DEFINE ENUMERATOR VARIABLES **/
		public EnlistedStatus enlistedStatus {
			get {
				EnlistedStatus level;
				if (Enum.TryParse<EnlistedStatus>(specialty.GetString("enlistedStatus"), out level)) {
					return level;
				} else {
					return EnlistedStatus.CIVILIAN;
				}
			}
			set {
				specialty.SetString("enlistedStatus", value.ToString());
				entity.WatchedAttributes.MarkPathDirty("specialty");
			}
		}
		
		public Specialization specialization {
			get {
				Specialization level;
				if (Enum.TryParse<Specialization>(specialty.GetString("specialization"), out level)) {
					return level;
				} else {
					return Specialization.EMPTY;
				}
			}
			set {
				specialty.SetString("specialization", value.ToString());
				entity.WatchedAttributes.MarkPathDirty("specialty");
			}
		}

		public CurrentCommand currentCommand {
			get {
				CurrentCommand level;
				if (Enum.TryParse<CurrentCommand>(specialty.GetString("currentCommand"), out level)) {
					return level;
				} else {
					return CurrentCommand.WANDER;
				}
			}
			set {
				specialty.SetString("currentCommand", value.ToString());
				entity.WatchedAttributes.MarkPathDirty("specialty");
			}
		}

		public override void Initialize(EntityProperties properties, JsonObject attributes) {
			base.Initialize(properties, attributes);
		}

		public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled) {
			base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);

			if (byEntity is EntityPlayer player && mode == EnumInteractMode.Interact) {
				// While a STRANGER has something in their ACTIVE SLOT try commands.
				if (enlistedStatus == EnlistedStatus.CIVILIAN && entity.Alive && cachedOwner == null && itemslot.Itemstack != null && !player.Controls.Sneak) {
					TryRecruiting(itemslot, player.Player);
					return;
				}
				// Try to revive if the entity is dead but not a carcass.
				if (!entity.Alive && itemslot.Itemstack != null && player.Controls.Sneak) {
					TryReviveWith(itemslot);
					return;
				}
				// While the OWNER or a GROUPMEMBER has something in their ACTIVE SLOT try commands.
				if (!entity.Alive && itemslot.Itemstack != null && (ownerUID == player.PlayerUID || player.Player.GetGroup(groupUID) != null)) {
					TryOrderRally(itemslot, player.Player);
				}
				// While the OWNER is SNEAKING with an EMPTY SLOT open inventory dialogbox.
				if (ownerUID == player.PlayerUID && player.Controls.Sneak && itemslot.Empty) {
					(entity as EntityArcher).ToggleInventoryDialog(player.Player);
					return;
				}
			}
		}

		public override void OnEntitySpawn() {
			base.OnEntitySpawn();
			// If there is a player within a reasonable range of spawn, make them the owner.
			EntityPlayer player = entity.World.GetPlayersAround(entity.Pos.XYZ, 15, 15).First<IPlayer>()?.Entity;
			if (player != null) {
				ownerUID = player?.PlayerUID.ToString();
				// If the player belongs to any groups, add them to this.
				if (player?.Player?.GetGroups()?.Length > 0) {
					groupUID = player.Player.GetGroups().ElementAt(0).GroupUid;
				}
			}
		}

		public override void OnEntityDespawn(EntityDespawnData despawn) {
			base.OnEntityDespawn(despawn);
			(entity as EntityArcher).InventoryDialog?.TryClose();
		}

		public override void OnEntityLoaded() {
			base.OnEntityLoaded();
			// If the entity has an owner it already belongs to then set it.
			if (entity.WatchedAttributes.HasAttribute("loyalties")) {
				_cachedOwner = entity.World.PlayerByUid(ownerUID);
				// If the entity has a group it already belongs to then set it.
				if (_cachedGroup?.GroupUid != null) {
					_cachedGroup = _cachedOwner.GetGroups()[0];
				}
			}
		}

		public override string PropertyName() {
			return "SoldierGearItems";
		}

		public string InventoryTreeKey = "inventory";
		public string LoyaltiesTreeKey = "loyalties";
		public string GearstatsTreeKey = "specialty";

		public float reloadSpeed { get; set; } = 500f;
		public float aimingSpeed { get; set; } = 500f;
		public float shieldArmor { get; set; } = 0f;
		public ItemStack arrowsStack {
			get => specialty.GetItemstack("arrowsStack");
			set {
				EntityArcher thisEntity = entity as EntityArcher;
				if (!thisEntity.AmmoItemSlot.Empty) {
					specialty.SetItemstack("arrowsStack", thisEntity.AmmoItemSlot.Itemstack);
				} else {
					specialty.SetItemstack("healthStack", null);
				}
				entity.WatchedAttributes.MarkPathDirty("specialty");
			}
		}
		public ItemStack healthStack {
			get => specialty.GetItemstack("healthStack");
			set {
				EntityArcher thisEntity = entity as EntityArcher;
				if (!thisEntity.HealItemSlot.Empty) {
					specialty.SetItemstack("healthStack", thisEntity.HealItemSlot.Itemstack);
				} else {
					specialty.SetItemstack("healthStack", null);
				}
				entity.WatchedAttributes.MarkPathDirty("specialty");
			}
		}

		private IPlayer _cachedOwner;
		public IPlayer cachedOwner {
			get {
				if (_cachedOwner?.PlayerUID == ownerUID) {
					return _cachedOwner;
				}
				if (String.IsNullOrEmpty(ownerUID)) {
					return null;
				}
				_cachedOwner = entity.World.PlayerByUid(ownerUID);
				return _cachedOwner;
			}
		}

		private PlayerGroupMembership _cachedGroup;
		public PlayerGroupMembership cachedGroup {
			get {
				if (_cachedGroup?.GroupUid == groupUID) {
					return _cachedGroup;
				}
				if (_cachedGroup?.GroupUid == 0) {
					return null;
				}
				// Join the first group they are a member of if possible.
				if (_cachedOwner != null) {
					if (_cachedOwner.GetGroups().Length > 1) {
						_cachedGroup = _cachedOwner.Groups[0];
					}
				}
				return _cachedGroup;
			}
		}

		private BlockEntityPost _cachedBlock;
		public BlockEntityPost cachedBlock {
			get {
				if (_cachedBlock == null) {
					return null;
				}
				return _cachedBlock;
			}
		}

		public string ownerUID {
			get => loyalties.GetString("ownerUID");
			set {
				loyalties.SetString("ownerUID", value);
				entity.WatchedAttributes.MarkPathDirty("loyalties");
			}
		}

		public int groupUID {
			get => loyalties.GetInt("groupUID");
			set {
				loyalties.SetInt("groupUID", value);
				entity.WatchedAttributes.MarkPathDirty("loyalties");
			}
		}

		public BlockPos blockPOS {
			get => loyalties.GetBlockPos("blockPOS");
			set {
				loyalties.SetBlockPos("blockPOS", value);
				entity.WatchedAttributes.MarkPathDirty("loyalties");
			}
		}

		public void PlaceBlockPOS(BlockEntityPost post) {
			_cachedBlock = post;
			blockPOS = post.Position.AsBlockPos;
		}

		public void ClearBlockPOS() {
			// Can keep the blockPOS so troops can still return home even if it is destroyed.
			_cachedBlock = null;
			return;
		}

		private void TryRecruiting(ItemSlot itemslot, IPlayer player) {
			// If the entity isn't already owned, giving it some kind of currency will hire it on to join.
			if (itemslot.Itemstack.ItemAttributes["currency"].Exists) {
				itemslot.TakeOut(1);
				itemslot.MarkDirty();
				// Set the owner to this player, and set the enlistment to ENLISTED.
				enlistedStatus = EnlistedStatus.ENLISTED;
				_cachedOwner = player;
				ownerUID = player.PlayerUID;
				// If the owner also is in a group then go ahead and join that too.
				if (player.GetGroups().Length > 0) {
					_cachedGroup = player.GetGroup(player.Groups[0].GroupUid);
					groupUID = player.Groups[0].GroupUid;
				}
			}
		}
		
		private void TryReviveWith(ItemSlot itemslot) {
			try {
				if (itemslot.Itemstack.Collectible.Attributes["health"].Exists && entity.GetBehavior<EntityBehaviorHarvestable>()?.IsHarvested != true) {
					entity.Revive();
					if (entity.HasBehavior<EntityBehaviorHealth>()) {
						entity.GetBehavior<EntityBehaviorHealth>().Health = itemslot.Itemstack.Collectible.Attributes["health"].AsFloat();
					}
					itemslot.TakeOut(1);
					itemslot.MarkDirty();
				}
			} catch {
				entity.Api.World.Logger.Error("Caught error here! Item path was: " + itemslot?.Itemstack?.Collectible?.Code?.ToString());
			}
		}
		
		private void TryOrderRally(ItemSlot itemslot, IPlayer player) {
			if (itemslot.Itemstack.Item is ItemBanner) {
				EntityPlayer playerEnt = player.Entity;
				// Activate orders for all surrounding soldiers of this player's faction to follow them!
				foreach (Entity soldier in entity.World.GetEntitiesAround(entity.ServerPos.XYZ, 15, 4, entity => (SoldierUtility.CanFollowThis(entity, playerEnt)))) {
					var taskManager = soldier.GetBehavior<EntityBehaviorTaskAI>().TaskManager;
					taskManager.AllTasks.Clear();
					taskManager.ExecuteTask<AiTaskFollowEntityLeader>();
				}
			}
		}
	}
}
