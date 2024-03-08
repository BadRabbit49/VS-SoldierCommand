using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace SoldierCommand {
	public class BehaviorAlternatePathtraverser : EntityBehavior {

		public SoldierWaypointsTraverser soldierWaypointsTraverser { get; private set; }
		public BehaviorAlternatePathtraverser(Entity entity) : base(entity) { }

		public override void Initialize(EntityProperties properties, JsonObject attributes) {
			base.Initialize(properties, attributes);
			soldierWaypointsTraverser = new SoldierWaypointsTraverser(entity as EntityAgent);
		}

		public override void OnGameTick(float deltaTime) {
			base.OnGameTick(deltaTime);
			soldierWaypointsTraverser.OnGameTick(deltaTime);
		}

		public override string PropertyName() {
			return "AlternatePathtraverser";
		}
	}
}