using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.TiledTest.light
{
	using UnityEngine;

	public class Lightsource
	{
		public Vector2 Point;

		public float MaxRadius;

		public float Angle;

		public float AngleRange;

		public Lightsource(Vector2 point, float maxRadius, float angle, float angleRange)
		{
			this.Point = point;
			this.MaxRadius = maxRadius;
			this.Angle = angle;
			this.AngleRange = angleRange;
		}
	}
}
