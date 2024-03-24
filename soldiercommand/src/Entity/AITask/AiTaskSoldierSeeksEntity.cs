using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace SoldierCommand {
	public class AiTaskSoldierSeeksEntity : AiTaskSeekEntity {
		long lastCheckTotalMs { get; set; }
		long lastCheckCooldown { get; set; } = 500;
		long lastCallForHelp { get; set; }
		private long lastOwnerLookup { get; set; }

		private BehaviorGearItems _behaviorGearItems;
		private BehaviorGearItems behaviorGearItems {
			get {
				if (_behaviorGearItems == null && lastOwnerLookup + 5000 < entity.World.ElapsedMilliseconds) {
					lastOwnerLookup = entity.World.ElapsedMilliseconds;
					_behaviorGearItems = entity.GetBehavior<BehaviorGearItems>();
				}
				return _behaviorGearItems;
			}
		}

		public AiTaskSoldierSeeksEntity(EntityAgent entity) : base(entity) { }

		public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig) {
			base.LoadConfig(taskConfig, aiConfig);
		}

		public override bool ShouldExecute() {
			if (whenInEmotionState == null) {
				return false;
			}
			if (lastCheckTotalMs + lastCheckCooldown > entity.World.ElapsedMilliseconds) {
				return false;
			} else {
				lastCheckTotalMs = entity.World.ElapsedMilliseconds;
			}
			if (targetEntity != null && targetEntity.Alive && EntityInReach(targetEntity)) {
				targetPos = targetEntity.ServerPos.XYZ;
				return true;
			} else {
				targetEntity = null;
			}
			if (attackedByEntity != null && attackedByEntity.Alive && EntityInReach(attackedByEntity)) {
				targetEntity = attackedByEntity;
				targetPos = targetEntity.ServerPos.XYZ;
				return true;
			} else {
				attackedByEntity = null;
			}
			if (lastSearchTotalMs + searchWaitMs < entity.World.ElapsedMilliseconds) {
				lastSearchTotalMs = entity.World.ElapsedMilliseconds;
				targetEntity = partitionUtil.GetNearestInteractableEntity(entity.ServerPos.XYZ, seekingRange, potentialTarget => IsTargetableEntity(potentialTarget, seekingRange));
				if (targetEntity != null && targetEntity.Alive && EntityInReach(targetEntity)) {
					targetPos = targetEntity.ServerPos.XYZ;
					return true;
				} else {
					targetEntity = null;
					return false;
				}
			}
			return false;
		}

		public override bool IsTargetableEntity(Entity ent, float range, bool ignoreEntityCode = false) {
			if (targetEntity == null) {
				return false;
			}
			if (targetEntity is EntityPlayer player) {
				string owner = behaviorGearItems.ownerUID;
				int group = behaviorGearItems.groupUID;
				if (player.PlayerUID == owner) {
					return false;
				}
				if (!entity.Api.World.Config.GetAsBool("PvpOff") && player.Player.GetGroup(group) != null) {
					return true;
				}
			}
			if (ent.ServerPos.SquareDistanceTo(entity.ServerPos) > range * range) {
				return false;
			}
			return base.IsTargetableEntity(ent, range, ignoreEntityCode);
		}

		public override void StartExecute() {
			base.StartExecute();
			world.Logger.Chat("Started Seeking Execute on: " + targetEntity.ToString());
		}

		public override bool ContinueExecute(float dt) {
			return targetEntity != null && EntityInReach(targetEntity) && base.ContinueExecute(dt);
		}

		public override void OnEntityHurt(DamageSource source, float damage) {
			base.OnEntityHurt(source, damage);
			if (source.Type != EnumDamageType.Heal && lastCallForHelp + 5000 < entity.World.ElapsedMilliseconds) {
				lastCallForHelp = entity.World.ElapsedMilliseconds;
				// Alert all surrounding units! We're under attack!
				foreach (var soldier in entity.World.GetEntitiesAround(entity.ServerPos.XYZ, 15, 4, entity => (entity is EntityArcher))) {
					var taskManager = soldier.GetBehavior<EntityBehaviorTaskAI>().TaskManager;
					taskManager.GetTask<AiTaskSoldierSeeksEntity>()?.OnAllyAttacked(source.SourceEntity);
					taskManager.GetTask<AiTaskSoldierRangeAttack>()?.OnAllyAttacked(source.SourceEntity);
				}
			}
		}

		public void OnAllyAttacked(Entity byEntity) {
			if (targetEntity == null || !targetEntity.Alive) {
				targetEntity = byEntity;
			}
		}

		private bool EntityInReach(Entity candidate) {
			var squareDistance = candidate.ServerPos.SquareDistanceTo(entity.ServerPos.XYZ);
			return squareDistance < seekingRange * seekingRange * 2 && squareDistance > 4;
		}
	}
}