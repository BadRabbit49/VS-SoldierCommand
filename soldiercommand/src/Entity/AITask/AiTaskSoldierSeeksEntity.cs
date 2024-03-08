using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace SoldierCommand {
	public class AiTaskSoldierSeeksEntity : AiTaskSeekEntity {
		protected long lastCheckTotalMs { get; set; }
		protected long lastCheckCooldown { get; set; } = 500;
		protected long lastCallForHelp { get; set; }

		public AiTaskSoldierSeeksEntity(EntityAgent entity) : base(entity) { }

		public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig) {
			base.LoadConfig(taskConfig, aiConfig);
		}

		public override bool ShouldExecute() {
			if (lastCheckTotalMs + lastCheckCooldown > entity.World.ElapsedMilliseconds) {
				return false;
			} else {
				lastCheckTotalMs = entity.World.ElapsedMilliseconds;
			}
			if (targetEntity != null && targetEntity.Alive && entityInReach(targetEntity)) {
				targetPos = targetEntity.ServerPos.XYZ;
				return true;
			} else {
				targetEntity = null;
			}
			if (attackedByEntity != null && attackedByEntity.Alive && entityInReach(attackedByEntity)) {
				targetEntity = attackedByEntity;
				targetPos = targetEntity.ServerPos.XYZ;
				return true;
			} else {
				attackedByEntity = null;
			}
			if (lastSearchTotalMs + searchWaitMs < entity.World.ElapsedMilliseconds) {
				lastSearchTotalMs = entity.World.ElapsedMilliseconds;
				targetEntity = partitionUtil.GetNearestInteractableEntity(entity.ServerPos.XYZ, seekingRange, potentialTarget => IsTargetableEntity(potentialTarget, seekingRange));
				if (targetEntity != null && targetEntity.Alive && entityInReach(targetEntity)) {
					targetPos = targetEntity.ServerPos.XYZ;
					return true;
				} else {
					targetEntity = null;
				}
			}
			return false;
		}

		public override bool IsTargetableEntity(Entity ent, float range, bool ignoreEntityCode = false) {
			if (ent == null) {
				return false;
			}
			var owner = (entity as EntityArcher).ownerINT;
			if (ent is EntityPlayer player) {
				if (player.PlayerUID == owner?.PlayerUID && owner != null) {
					return false;
				}
				if (SoldierConfig.Current.PvpOff && player.PlayerUID != owner?.PlayerUID) {
					return false;
				}
			}
			if (ent.ServerPos.SquareDistanceTo(entity.ServerPos) > range * range) {
				return false;
			}
			return base.IsTargetableEntity(ent, range, ignoreEntityCode);
		}

		public override void StartExecute() {
			base.StartExecute();
		}

		public override bool ContinueExecute(float dt) {
			return targetEntity != null && entityInReach(targetEntity) && base.ContinueExecute(dt);
		}

		public override void OnEntityHurt(DamageSource source, float damage) {
			base.OnEntityHurt(source, damage);
			if (source.Type != EnumDamageType.Heal && lastCallForHelp + 5000 < entity.World.ElapsedMilliseconds) {
				lastCallForHelp = entity.World.ElapsedMilliseconds;
				foreach (var soldier in entity.World.GetEntitiesAround(entity.ServerPos.XYZ, 15, 4, entity => (entity is EntityArcher))) {
					var taskManager = soldier.GetBehavior<EntityBehaviorTaskAI>().TaskManager;
					taskManager.GetTask<AiTaskSoldierSeeksEntity>()?.OnAllyAttacked(source.SourceEntity);
					taskManager.GetTask<AiTaskSoldierMeleeAttack>()?.OnAllyAttacked(source.SourceEntity);
					taskManager.GetTask<AiTaskSoldierRangeAttack>()?.OnAllyAttacked(source.SourceEntity);
				}
			}
		}

		public void OnAllyAttacked(Entity byEntity) {
			if (targetEntity == null || !targetEntity.Alive) {
				targetEntity = byEntity;
			}
		}

		private bool entityInReach(Entity candidate) {
			var squareDistance = candidate.ServerPos.SquareDistanceTo(entity.ServerPos.XYZ);
			return squareDistance < seekingRange * seekingRange * 2 && squareDistance > 250000;
		}
	}
}