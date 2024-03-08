using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;

namespace SoldierCommand {
	public class BlockEntitySoldierPost : BlockEntity, IPointOfInterest {
		public Vec3d Position => Pos.ToVec3d();
		public Vec3d MiddlePostion => Pos.ToVec3d().AddCopy(MiddleOffset);
		public Vec3d MiddleOffset => new Vec3d(Block.Attributes["x"].AsDouble(), Block.Attributes["y"].AsDouble(), Block.Attributes["z"].AsDouble());
		public string Type => "soldierpost";

		public override void Initialize(ICoreAPI api) {
			base.Initialize(api);
			if (api is ICoreServerAPI sapi) {
				sapi.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
			}
		}

		public override void OnBlockBroken(IPlayer byPlayer = null) {
			base.OnBlockBroken(byPlayer);
			(Api as ICoreServerAPI)?.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
		}

		public override void OnBlockRemoved() {
			base.OnBlockRemoved();
			(Api as ICoreServerAPI)?.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
		}
	}
}