namespace Assets.Unsprite
{
	public class SpriteAnimationData
	{
		public string Name;

		public float Fps;

		public uint Start;

		public uint End;

		public int Loops;

		public float frameDeltaS;

		public SpriteAnimationData(string name, float fps, uint start, uint end, int loops)
		{
			this.Name = name;
			this.Fps = fps;
			this.Start = start;
			this.End = end;
			this.Loops = loops;

			this.frameDeltaS = (float)(1d / fps);
		}
	}
}