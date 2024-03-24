using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace SoldierCommand {
	public class AiTaskSoldierReturningTo : AiTaskBase {
		

		bool completed;
		float moveSpeed = 0.035f;
		long lastCheckTotalMs { get; set; }
		long lastCheckCooldown { get; set; } = 500;
		long lastOwnerLookup { get; set; }
		BlockEntityPost post = null;
		SoldierWaypointsTraverser soldierPathTraverser;
		BehaviorGearItems _behaviorGearItems;
		BehaviorGearItems behaviorGearItems {
			get {
				if (_behaviorGearItems == null && lastOwnerLookup + 5000 < entity.World.ElapsedMilliseconds) {
					lastOwnerLookup = entity.World.ElapsedMilliseconds;
					_behaviorGearItems = entity.GetBehavior<BehaviorGearItems>();
				}
				return _behaviorGearItems;
			}
		}

		public AiTaskSoldierReturningTo(EntityAgent entity) : base(entity) { }

		public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig) {
			base.LoadConfig(taskConfig, aiConfig);
			soldierPathTraverser = entity.GetBehavior<BehaviorTraverser>().soldierWaypointsTraverser;
			completed = false;
		}

		public override bool ShouldExecute() {
			// TODO: CHECK IF THEY HAVE ORDERS TO FOLLOW OR STAY AT THEIR POSITION. OTHERWISE LET THEM RETURN TO THEIR POST BLOCK.
			// Soldier must be dead and have their guardPost resupply have respawns available.
			if (lastCheckTotalMs + lastCheckCooldown < entity.World.ElapsedMilliseconds) {
				lastCheckTotalMs = entity.World.ElapsedMilliseconds;
			} else {
				return false;
			}

			if (behaviorGearItems.currentCommand == CurrentCommand.RETURN) {
				if (behaviorGearItems.cachedBlock != null) {
					post = behaviorGearItems.cachedBlock;
					if (post.soldierIds.Contains(entity.EntityId) && post.respawns > 0) {
						return true;
					}
				}
			}
			return false;
		}

		public override void StartExecute() {
			if (post != null) {
				completed = !soldierPathTraverser.NavigateTo(post.Pos.ToVec3d(), moveSpeed, 0.5f, goToPost, goToPost, true, 10000);
			} else {
				completed = true;
			}
			if (completed) {
				goToPost();
			} else {
				base.StartExecute();
			}
		}

		public override bool ContinueExecute(float dt) {
			if (lastCheckCooldown + 500 < entity.World.ElapsedMilliseconds && post != null && entity.MountedOn == null) {
				lastCheckCooldown = entity.World.ElapsedMilliseconds;
				if (entity.ServerPos.SquareDistanceTo(post.Pos.ToVec3d()) < 2) {
					goToPost();
				}
			}
			return completed;
		}

		public override void FinishExecute(bool cancelled) {
			soldierPathTraverser.Stop();
			base.FinishExecute(cancelled);
		}

		private void goToPost() {
			completed = true;
			soldierPathTraverser.Stop();
			if (post != null) {
			
			}
		}
	}
}