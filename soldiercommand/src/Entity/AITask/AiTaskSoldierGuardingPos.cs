using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace SoldierCommand {
	public class AiTaskSoldierGuardingPos : AiTaskBase {
		double? x;
		double? y;
		double? z;
		float moveSpeed = 0.01f;
		float range = 40f;
		float maxDistance = 10f;
		bool stuck = false;
		public AiTaskSoldierGuardingPos(EntityAgent entity) : base(entity) { }

		public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig) {
			base.LoadConfig(taskConfig, aiConfig);
			if (taskConfig["movespeed"] != null) {
				moveSpeed = taskConfig["movespeed"].AsFloat(0.01f);
			}
			if (taskConfig["searchrange"] != null) {
				range = taskConfig["searchrange"].AsFloat(40f);
			}
			if (taskConfig["maxdistance"] != null) {
				maxDistance = taskConfig["maxdistance"].AsFloat(10f);
			}
		}

		public override bool ShouldExecute() {
			// TODO: Setup guarding execution parameters.
			return false;
		}

		public override void StartExecute() {
			base.StartExecute();
			if (x != null && y != null && z != null) {
				pathTraverser.WalkTowards(new Vec3d((double)x, (double)y, (double)z), moveSpeed, maxDistance / 2, OnGoalReached, OnStuck);
			}
			stuck = false;
		}

		public override bool ContinueExecute(float dt) {
			if (x == null || y == null || z == null) {
				return false;
			}
			if (entity.ServerPos.SquareDistanceTo((double)x, (double)y, (double)z) < maxDistance * maxDistance / 4) {
				pathTraverser.Stop();
				return false;
			}
			return !stuck && pathTraverser.Active;
		}

		private void OnStuck() {
			stuck = true;
		}

		private void OnGoalReached() {
			// Do nothing.
		}
	}
}