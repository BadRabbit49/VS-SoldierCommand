using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using ProtoBuf;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace SoldierCommand {
    public class SoldierCommand : ModSystem {
		Harmony harmony = new Harmony("badrabbit49.soldiercommand");
		public static ICoreServerAPI serverAPI;
		public static ICoreClientAPI clientAPI;
		
		public override void Start(ICoreAPI api) {
			base.Start(api);
			// Blocks
			api.RegisterBlockEntityClass("SoldierPost", typeof(BlockEntitySoldierPost));
			api.RegisterBlockClass("SoldierPost", typeof(BlockSoldierPost));
			// Entities
			api.RegisterEntity("EntitySoldier", typeof(EntitySoldier));
			api.RegisterEntity("EntityArcher", typeof(EntityArcher));
			// Behaviors
			api.RegisterEntityBehaviorClass("SoldierPathTraverser", typeof(BehaviorAlternatePathtraverser));
			// AITasks
			AiTaskRegistry.Register<AiTaskFollowPlayerLeader>("FollowPlayerLeader");
			AiTaskRegistry.Register<AiTaskSoldierGuardingPos>("SoldierGuardingPos");
			AiTaskRegistry.Register<AiTaskSoldierTravelToPos>("SoldierTravelToPos");
			AiTaskRegistry.Register<AiTaskSoldierMeleeAttack>("SoldierMeleeAttack");
			AiTaskRegistry.Register<AiTaskSoldierRangeAttack>("SoldierRangeAttack");
			AiTaskRegistry.Register<AiTaskSoldierSeekPostPos>("SoldierSeekPostPos");
			AiTaskRegistry.Register<AiTaskSoldierSeeksEntity>("SoldierSeeksEntity");
			AiTaskRegistry.Register<AiTaskSoldierTargetables>("SoldierTargetables");
			// Patch Everything
			if (!Harmony.HasAnyPatches("badrabbit49.soldiercommand")) {
				harmony.PatchAll();
			}
		}

		public override void StartClientSide(ICoreClientAPI api) {
			base.StartClientSide(api);
			clientAPI = api;
			api.Network.RegisterChannel("soldiernetwork").RegisterMessageType<SoldierCommandMsg>().RegisterMessageType<SoldierProfileMsg>().SetMessageHandler<SoldierProfileMsg>(OnSoldierProfileMsgClient);
		}

		public override void StartServerSide(ICoreServerAPI api) {
			base.StartServerSide(api);
			serverAPI = api;
			api.Network.RegisterChannel("soldiernetwork").RegisterMessageType<SoldierCommandMsg>().SetMessageHandler<SoldierCommandMsg>(OnSoldierCommandMsg).RegisterMessageType<SoldierProfileMsg>().SetMessageHandler<SoldierProfileMsg>(OnSoldierProfileMsgServer);
		}

		public override void Dispose() {
			base.Dispose();
			// Unload and Unpatch everything from the mod.
			harmony?.UnpatchAll(Mod.Info.ModID);
		}

		private void OnSoldierCommandMsg(IServerPlayer fromPlayer, SoldierCommandMsg networkMessage) {
			EntityPlayer player = serverAPI.World.PlayerByUid(networkMessage.playerUID)?.Entity;
			EntityAgent target = serverAPI.World.GetEntityById(networkMessage.targetEntityUID) as EntityAgent;
		}

		private void OnSoldierProfileMsgServer(IServerPlayer fromPlayer, SoldierProfileMsg networkMessage) {
			EntityAgent target = serverAPI.World.GetEntityById(networkMessage.targetEntityUID) as EntityAgent;
			target.GetBehavior<EntityBehaviorNameTag>()?.SetName(networkMessage.soldierName);
		}

		private void OnSoldierProfileMsgClient(SoldierProfileMsg networkMessage) {
			if (clientAPI != null) {
				EntityAgent entity = clientAPI.World.GetEntityById(networkMessage.oldEntityUID) as EntityAgent;
				if (entity != null) {
					clientAPI.ShowChatMessage(Lang.Get("command:message-finished-recruiting", entity.GetName()));
				}
				new SoldierProfile(clientAPI, networkMessage.targetEntityUID).TryOpen();
			}
		}
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class SoldierCommandMsg {
		public string playerUID;
		public string commandName;
		public string commandType;
		public long targetEntityUID;
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class SoldierProfileMsg {
		public string soldierName;
		public bool multiplyAllowed;
		public bool abandon;
		public long targetEntityUID;
		public long oldEntityUID;
	}

	public class SoldierResurrector {
		public string name;
		public string domain;
		public float healingValue;
	}

	public class SoldierConfig {
		public static SoldierConfig Current {get; set;}
		public List<SoldierResurrector> Resurrectors {get; set;}
		public bool PvpOff { get; set; }
		public bool Groups { get; set; }
		public bool FriendlyFireO { get; set; }
		public bool FriendlyFireG { get; set; }
		public bool GroupRelation { get; set; }
		public bool ArmorWeightOn { get; set; }
		public bool FalldamageOff { get; set; }
		public bool AllowTeleport { get; set; }

		// Make config for configurable settings. Duh.
		public static SoldierConfig getDefault() {
			var config = new SoldierConfig();
			// Default settings for config variables.
			config.PvpOff = false;
			config.Groups = true;
			config.FriendlyFireO = false;
			config.FriendlyFireG = false;
			config.GroupRelation = true;
			config.ArmorWeightOn = true;
			config.FalldamageOff = false;
			config.AllowTeleport = false;
			config.Resurrectors = new List<SoldierResurrector>(new SoldierResurrector[] {
                // Base Game
                new SoldierResurrector(){name = "bandage-clean", domain ="game", healingValue = 4},
				new SoldierResurrector(){name = "bandage-alcoholed", domain ="game", healingValue = 8},
				new SoldierResurrector(){name = "poultice-reed-horsetail", domain ="game", healingValue = 1},
				new SoldierResurrector(){name = "poultice-reed-honey-sulfur", domain ="game", healingValue = 2},
				new SoldierResurrector(){name = "poultice-linen-horsetail", domain ="game", healingValue = 2},
				new SoldierResurrector(){name = "poultice-linen-honey-sulfur", domain ="game", healingValue = 3},
			});
			return config;
		}
	}
}