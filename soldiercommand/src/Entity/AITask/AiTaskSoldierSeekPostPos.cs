using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using System.Collections.Generic;

namespace SoldierCommand {
    public class AiTaskSoldierSeekPostPos : AiTaskBase {
		private BlockEntitySoldierPost post { get; set; }

		private List<DayTimeFrame> duringDayTimeFrames = new List<DayTimeFrame>();

		int range = 15;
		long lastCheck;
		bool stuck = false;

		float moveSpeed = 0.02f;
		public AiTaskSoldierSeekPostPos(EntityAgent entity) : base(entity) {}

		public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig) {
			base.LoadConfig(taskConfig, aiConfig);
			if (taskConfig["duringDayTimeFrames"] != null) {
				duringDayTimeFrames.AddRange(taskConfig["duringDayTimeFrames"].AsObject<DayTimeFrame[]>(new DayTimeFrame[0]));
			}
			range = taskConfig["horRange"].AsInt(15);
			moveSpeed = taskConfig["movespeed"].AsFloat(0.02f);
		}

		public override bool ShouldExecute() {
			if (lastCheck + 10000 > entity.World.ElapsedMilliseconds)
				return false;
			lastCheck = entity.World.ElapsedMilliseconds;
			if (duringDayTimeFrames.Count > 0) {
				double hourOfDay = entity.World.Calendar.HourOfDay / entity.World.Calendar.HoursPerDay * 24f + (entity.World.Rand.NextDouble() * 0.3f - 0.15f);
				if (!duringDayTimeFrames.Exists(frame => frame.Matches(hourOfDay)))
					return false;
			}
			if (post == null || entity.ServerPos.SquareDistanceTo(post.Position) > 50) {
				post = entity.Api.ModLoader.GetModSystem<POIRegistry>().GetNearestPoi(entity.ServerPos.XYZ, range, isValidNonOccupiedNest) as BlockEntitySoldierPost;
			}

			return post != null && entity.ServerPos.SquareDistanceTo(post.Pos.ToVec3d()) > 2;
		}

		private bool isValidNonOccupiedNest(IPointOfInterest poi) {
			if (poi is BlockEntitySoldierPost post) {
				if (entity.World.GetEntitiesAround(post.Position, 3, 3, occupier => occupier.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager?.GetTask<AiTaskSoldierSeekPostPos>()?.post == post).Length == 0) {
					return true;
				} else {
					return false;
				}
			}

			return false;
		}

		public override void StartExecute() {
			base.StartExecute();
			stuck = false;
			pathTraverser.WalkTowards(post.MiddlePostion, moveSpeed, 0.12f, () => { }, () => stuck = true);
		}

		public override bool ContinueExecute(float dt) {
			return !stuck && pathTraverser.Active;
		}

		public override string ToString() {
			return base.ToString();
		}
	}
}