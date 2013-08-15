namespace Assets.TiledTest.light
{
	using UnityEngine;


	/// <summary>
	/// 
	/// </summary>
	public class Lightsource
	{
		
	}


	/// <summary>
	/// 
	/// </summary>
	public class LightCone : Lightsource
	{
		public Vector2 Point;
		public float MaxRadius;
		public float Angle;
		public float AngleRange;

		public Vector3 Color = Vector3.one;

		public Camera renderCamera;

		public LightCone(Vector2 point, float maxRadius, float angle, float angleRange)
		{
			this.Point = point;
			this.MaxRadius = maxRadius;
			this.Angle = angle;
			this.AngleRange = angleRange;
		}
	}
}
