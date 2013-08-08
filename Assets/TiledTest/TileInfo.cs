namespace Assets.TiledTest
{
	public class TileInfo
	{
		public string Name;

		public string AtlasName;

		public uint PosX;

		public uint PosY;

		public TileInfo(string name, string atlasName, uint posX, uint posY)
		{
			this.Name = name;
			this.AtlasName = atlasName;
			this.PosX = posX;
			this.PosY = posY;
		}
	}
}