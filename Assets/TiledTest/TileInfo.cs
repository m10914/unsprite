namespace Assets.TiledTest
{
	using System;


	public enum TileType : uint
	{
		None = 0,

		Brick,

		Slope1,

		Slope2,

		Slope3,

		Slope4,

		Water,

		Ladder
	}


	[Serializable]
	public class TileInfo
	{
		public static uint instocount = 0 ;

		public string AtlasName;

		public uint PosX;

		public uint PosY;

		public TileType Type;

		public uint ID;

		public TileInfo(string atlasName, uint posX, uint posY, TileType type)
		{
			this.ID = instocount++;

			this.AtlasName = atlasName;
			this.PosX = posX;
			this.PosY = posY;
			this.Type = type;
		}
	}
}