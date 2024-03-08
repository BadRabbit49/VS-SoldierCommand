using Vintagestory.API.Common;
using System;

namespace SoldierCommand {
	public class BlockSoldierPost : Block {
		public EnumPostSize postSize {
			get {
				EnumPostSize size = EnumPostSize.SMALL;
				Enum.TryParse(Variant["size"].ToUpper(), out size);
				return size;
			}
		}
	}

	public enum EnumPostSize {SMALL, MEDIUM, LARGE}
}