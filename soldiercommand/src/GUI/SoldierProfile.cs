using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace SoldierCommand {
	public class SoldierProfile : GuiDialog {
		public override string ToggleKeyCombinationCode => null;

		private long targetEntityId;
		private int currentY = 20;

		string soldierName;
		bool abandon = false;

		public SoldierProfile(ICoreClientAPI capi, long targetEntityId) : base(capi) {
			this.targetEntityId = targetEntityId;
			var targetEntity = capi.World.GetEntityById(targetEntityId);
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;

			SingleComposer = capi.Gui.CreateCompo("SoldierProfileDialog-", dialogBounds)
				.AddShadedDialogBG(bgBounds)
				.AddDialogTitleBar(Lang.Get("command:gui-profile-title"), () => TryClose())
				.BeginChildElements(bgBounds);
			SingleComposer.AddStaticText(Lang.Get("command:gui-profile-name"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, currentY, 200, 20));
			currentY += 35;
			SingleComposer.AddTextInput(ElementBounds.Fixed(0, currentY, 200, 40), (name) => {
				if (!string.IsNullOrEmpty(name) && name.Length > 50) {
					name = name.Substring(0, 50);
					SingleComposer.GetTextInput("soldierName").SetValue(name);
				}
				soldierName = name;
			}, null, "soldierName");
			SingleComposer.GetTextInput("soldierName").SetValue(targetEntity?.GetBehavior<EntityBehaviorNameTag>()?.DisplayName);
			currentY += 50;
			float? health;
			float? maxhealth;
			getHealthSat(out health, out maxhealth, targetEntity);
			if (health != null && maxhealth != null) {
				SingleComposer.AddStaticText(Lang.Get("command:gui-profile-currenthealth", health, maxhealth), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, currentY, 240, 20));
				currentY += 50;
			}

			SingleComposer.AddStaticText(Lang.Get("command:gui-profile-abandon"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, currentY, 200, 20));
			SingleComposer.AddSwitch(value => abandon = value, ElementBounds.Fixed(150, currentY, 200, 20), "abandon");
			currentY += 50;
			SingleComposer.AddButton(Lang.Get("command:gui-profile-ok"), () => onClick(), ElementBounds.Fixed(0, currentY, 90, 40))
				.AddButton(Lang.Get("command:gui-profile-cancel"), () => TryClose(), ElementBounds.Fixed(150, currentY, 90, 40))
				.EndChildElements().Compose();
		}

		void getHealthSat(out float? health, out float? maxHealth, Entity targetEntity) {
			health = null;
			maxHealth = null;
			ITreeAttribute healthTree = targetEntity?.WatchedAttributes.GetTreeAttribute("health");
			if (healthTree != null) {
				health = healthTree.TryGetFloat("currenthealth");
				maxHealth = healthTree.TryGetFloat("maxhealth");
			}
			if (health != null) {
				health = (float)Math.Round((float)health, 1);
			}
			if (maxHealth != null) {
				maxHealth = (float)Math.Round((float)maxHealth, 1);
			}
		}

		private bool onClick() {
			var message = new SoldierProfileMsg();
			message.soldierName = soldierName;
			message.abandon = abandon;
			message.targetEntityUID = targetEntityId;

			capi.Network.GetChannel("soldiernetwork").SendPacket<SoldierProfileMsg>(message);

			TryClose();
			return true;
		}
	}
}