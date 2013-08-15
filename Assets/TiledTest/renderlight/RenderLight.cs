using System;
using System.Collections.Generic;
using System.Linq;

using Assets.TiledTest;
using Assets.TiledTest.light;

using UnityEngine;

public class RenderLight : MonoBehaviour
{
	#region Fields

	public Shader LightShader;
	public Shader DarkShader;
	public RenderTexture RenderTexture;

	public Lightsource ParentLight;


	private Material mat;
	private Material darkMat;

	#endregion

	// Use this for initialization

	#region Methods

	/// <summary>
	/// </summary>
	/// <param name="light"></param>
	private void ConstructAndDrawLightCone(LightCone light)
	{
		this.mat.SetVector("_Colour", new Vector4(1, 1, 1, 1));
		this.mat.SetVector("_LightPos", new Vector4(light.Point.x, light.Point.y, 0, 0));
		this.mat.SetFloat("_MaxDist", light.MaxRadius);
		this.mat.SetPass(0);

		GL.Begin(GL.TRIANGLES);

		float step = (float)Math.PI / 20f;
		int steps = (int)(light.AngleRange * 2f / step);
		int i = 0;
		for (float ang = light.Angle - light.AngleRange; ; ang += step)
		{
			if (i++ >= steps) break;

			var left = new Vector2((float)(light.MaxRadius * Math.Cos(ang)), (float)(light.MaxRadius * Math.Sin(ang)));
			var right = new Vector2(
				(float)(light.MaxRadius * Math.Cos(ang + step)),
				(float)(light.MaxRadius * Math.Sin(ang + step)));

			GL.Vertex3(light.Point.x, light.Point.y, 0);
			GL.Vertex3(light.Point.x + left.x, light.Point.y + left.y, 0);
			GL.Vertex3(light.Point.x + right.x, light.Point.y + right.y, 0);
		}

		GL.End();
	}

	/// <summary>
	/// </summary>
	/// <param name="light"></param>
	/// <param name="inf"></param>
	/// <param name="tilePos"></param>
	private void ConstructAndDrawObstacleVolume(LightCone light, TileInfo inf, Rect tilePos)
	{
		var allowed = new List<TileType>
		              {
			              TileType.Brick,
			              TileType.Slope1,
			              TileType.Slope2,
			              TileType.Slope3,
			              TileType.Slope4
		              };

		if (!allowed.Contains(inf.Type))
		{
			return;
		}


		//clamp light angle between 0 and 2PI
		if (light.Angle < 0)
		{
			light.Angle += 2f * (float)Math.PI;
		}
		else if (light.Angle > 2f * Math.PI)
		{
			light.Angle -= 2f * (float)Math.PI;
		}
		float startAngle = light.Angle - light.AngleRange;
		float endAngle = light.Angle + light.AngleRange;

		var smalllist = new List<UnoVert>();
		//smalllist represents the front segment of tile, back segment will be culled off

		if (inf.Type == TileType.Brick)
		{
			smalllist.Add(new UnoVert { objId = inf.ID, realPos = new Vector2(tilePos.xMin, tilePos.yMin) });
			smalllist.Add(new UnoVert { objId = inf.ID, realPos = new Vector2(tilePos.xMax, tilePos.yMin) });
			smalllist.Add(new UnoVert { objId = inf.ID, realPos = new Vector2(tilePos.xMax, tilePos.yMax) });
			smalllist.Add(new UnoVert { objId = inf.ID, realPos = new Vector2(tilePos.xMin, tilePos.yMax) });

			this.SetupSmalllist(ref smalllist, light);
			if (!this.IfVectorsInRange(smalllist, startAngle, endAngle))
			{
				return;
			}

			//now smalllist contains only front face of quad, which is always 3 vertices
		}
		else if (inf.Type == TileType.Slope1)
		{
			smalllist.Add(new UnoVert { objId = inf.ID, realPos = new Vector2(tilePos.xMin, tilePos.yMin) });
			smalllist.Add(new UnoVert { objId = inf.ID, realPos = new Vector2(tilePos.xMax, tilePos.yMax) });
			smalllist.Add(new UnoVert { objId = inf.ID, realPos = new Vector2(tilePos.xMin, tilePos.yMax) });

			this.SetupSmalllist(ref smalllist, light);
			if (!this.IfVectorsInRange(smalllist, startAngle, endAngle))
			{
				return;
			}
		}
		else if (inf.Type == TileType.Slope2)
		{
			smalllist.Add(new UnoVert { objId = inf.ID, realPos = new Vector2(tilePos.xMax, tilePos.yMin) });
			smalllist.Add(new UnoVert { objId = inf.ID, realPos = new Vector2(tilePos.xMax, tilePos.yMax) });
			smalllist.Add(new UnoVert { objId = inf.ID, realPos = new Vector2(tilePos.xMin, tilePos.yMax) });

			this.SetupSmalllist(ref smalllist, light);
			if (!this.IfVectorsInRange(smalllist, startAngle, endAngle))
			{
				return;
			}
		}
		else if (inf.Type == TileType.Slope3)
		{
			smalllist.Add(new UnoVert { objId = inf.ID, realPos = new Vector2(tilePos.xMin, tilePos.yMin) });
			smalllist.Add(new UnoVert { objId = inf.ID, realPos = new Vector2(tilePos.xMax, tilePos.yMin) });
			smalllist.Add(new UnoVert { objId = inf.ID, realPos = new Vector2(tilePos.xMax, tilePos.yMax) });

			this.SetupSmalllist(ref smalllist, light);
			if (!this.IfVectorsInRange(smalllist, startAngle, endAngle))
			{
				return;
			}
		}
		else if (inf.Type == TileType.Slope4)
		{
			smalllist.Add(new UnoVert { objId = inf.ID, realPos = new Vector2(tilePos.xMin, tilePos.yMin) });
			smalllist.Add(new UnoVert { objId = inf.ID, realPos = new Vector2(tilePos.xMax, tilePos.yMin) });
			smalllist.Add(new UnoVert { objId = inf.ID, realPos = new Vector2(tilePos.xMin, tilePos.yMax) });

			this.SetupSmalllist(ref smalllist, light);
			if (!this.IfVectorsInRange(smalllist, startAngle, endAngle))
			{
				return;
			}
		}

		//drawing itself

		this.darkMat.SetVector("_Colour", new Vector4(0, 0, 0, 1));
		this.darkMat.SetPass(0);
		GL.Begin(GL.TRIANGLES);

		UnoVert leftVert = smalllist.First();
		UnoVert rightVert = smalllist.Last();

		var leftVol = new Vector3(
			light.Point.x + (light.MaxRadius + 10f) * (float)Math.Cos(leftVert.angOffset),
			light.Point.y + (light.MaxRadius + 10f) * (float)Math.Sin(leftVert.angOffset),
			0);
		var rightVol = new Vector3(
			light.Point.x + (light.MaxRadius + 10f) * (float)Math.Cos(rightVert.angOffset),
			light.Point.y + (light.MaxRadius + 10f) * (float)Math.Sin(rightVert.angOffset),
			0);
		Vector3 leftFront = leftVert.GetVec3();
		Vector3 rightFront = rightVert.GetVec3();

		/*if (tilePos.x == 160 && tilePos.y == 40)
		{
			Debug.Log("damaged: " + leftVert.angOffset + " > " + rightVert.angOffset);
			//Debug.Log("totalCount : " + smalllist.Count() + ", leftvol: " + leftVol.x + ",  " + leftVol.y + ", rightVol: " + rightVol.x + ",  " + rightVol.y);
			//Debug.Log("totalCount : " + smalllist.Count() + ", leftvol: " + leftFront.x + ",  " + leftFront.y + ", rightVol: " + rightFront.x + ",  " + rightFront.y);
		}
		else if (tilePos.x == 160 && tilePos.y == 30)
		{
			Debug.Log("normal: " + leftVert.angOffset + " > " + rightVert.angOffset);
		}*/
		//Debug.Log(tilePos.ToString());

		//little bit further
		leftFront.x += 3f * (float)Math.Cos(rightVert.angOffset);
		leftFront.y += 3f * (float)Math.Sin(rightVert.angOffset);
		rightFront.x += 3f * (float)Math.Cos(rightVert.angOffset);
		rightFront.y += 3f * (float)Math.Sin(rightVert.angOffset);

		//render back volume
		GL.Vertex3(leftFront.x, leftFront.y, leftFront.z);
		GL.Vertex3(leftVol.x, leftVol.y, leftVol.z);
		GL.Vertex3(rightVol.x, rightVol.y, rightVol.z);

		GL.Vertex3(rightVol.x, rightVol.y, rightVol.z);
		GL.Vertex3(rightFront.x, rightFront.y, rightFront.z);
		GL.Vertex3(leftFront.x, leftFront.y, leftFront.z);

		//render front volume
		if (smalllist.Count == 3 && smalllist[1].distance < light.MaxRadius)
		{
			Vector3 midFront = smalllist[1].GetVec3();
			midFront.x += 3f * (float)Math.Cos(rightVert.angOffset);
			midFront.y += 3f * (float)Math.Sin(rightVert.angOffset);

			GL.Vertex3(midFront.x, midFront.y, midFront.z);
			GL.Vertex3(leftFront.x, leftFront.y, leftFront.z);
			GL.Vertex3(rightFront.x, rightFront.y, rightFront.z);
		}

		GL.End();
	}

	/// <summary>
	/// </summary>
	/// <param name="list"></param>
	/// <returns></returns>
	private bool IfVectorsInRange(List<UnoVert> list, float startAngle, float endAngle)
	{
		foreach (UnoVert ang in list)
		{
			if (ang.FitToAngles(startAngle, endAngle))
			{
				return true;
			}
		}
		return false;
	}

	// Update is called once per frame
	private void OnPostRender()
	{
		//this.InitializePointsArray();
		int i, j;

		TiledLevel lev = GameObject.Find("TiledLevel").GetComponent<TiledLevel>();
		List<List<TileInfo>> Tiles = lev.Tiles;

		// LIGHTCONE
		if (typeof(LightCone) == ParentLight.GetType())
		{
			LightCone light = ParentLight as LightCone;

			this.ConstructAndDrawLightCone(light);

			for (i = 0; i < Tiles.Count; i++)
			{
				for (j = 0; j < Tiles[i].Count; j++)
				{
					if ((new Vector2(i * lev.GlobalScale, j * lev.GlobalScale) - light.Point).magnitude
						< lev.GlobalScale + light.MaxRadius)
					{
						this.ConstructAndDrawObstacleVolume(
							light,
							Tiles[i][j],
							new Rect(i * lev.GlobalScale, j * lev.GlobalScale, lev.GlobalScale, lev.GlobalScale));
					}
				}
			}
		}
		
	}

	private void SetupSmalllist(ref List<UnoVert> list, LightCone light)
	{
		//calculate parameters
		foreach (UnoVert smal in list)
		{
			smal.CalculateParams(light);
		}

		//order list
		//list = list.OrderBy(el => el.angOffset).ToList();
		list.Sort(
			(v1, v2) =>
			{
				if (v1.angOffset < Math.PI * 0.5f && v2.angOffset > Math.PI * 1.5f) return 1;
				else if (v2.angOffset < Math.PI * 0.5f && v1.angOffset > Math.PI * 1.5f) return -1;
				else return v1.angOffset.CompareTo(v2.angOffset);
			});
		//that order could go wrong, because


		//if some vertices are with the same angle offset, remove back vertices (presume it's a convex shape)
		bool bFixed = false;
		while (!bFixed)
		{
			if (list.Count > 1 && list[0].angOffset == list[1].angOffset)
			{
				if (list[0].distance > list[1].distance)
				{
					list.RemoveAt(0);
				}
				else
				{
					list.RemoveAt(1);
				}

				continue;
			}

			if (list.Count > 1 && list.Last().angOffset == list[list.Count - 2].angOffset)
			{
				if (list.Last().distance > list[list.Count - 2].distance)
				{
					list.RemoveAt(list.Count - 1);
				}
				else
				{
					list.RemoveAt(list.Count - 2);
				}

				continue;
			}

			bFixed = true;
		}

		//now remove all backfaces vertices
		UnoVert[] temparr = list.GetRange(1, list.Count - 2).ToArray();
		UnoVert first = list.First();
		UnoVert last = list.Last();
		foreach (UnoVert tempy in temparr)
		{
			float distMedian = (tempy.angOffset - first.angOffset) / (last.angOffset - first.angOffset)
			                   * (last.distance - first.distance) + first.distance;
			if (tempy.distance > distMedian)
			{
				list.Remove(tempy);
			}
		}

		//Debug.Log("left " + list.Count);
	}

	private void Start()
	{
		this.mat = new Material(this.LightShader);
		this.darkMat = new Material(this.DarkShader);
	}

	#endregion

	/// <summary>
	/// </summary>
	public class UnoVert
	{
		#region Fields

		public uint objId;

		public Vector2 realPos;

		#endregion

		#region Public Properties

		public float angOffset { private set; get; }

		public float distance { private set; get; }

		#endregion

		#region Public Methods and Operators

		/// <summary>
		/// </summary>
		/// <param name="light"></param>
		public void CalculateParams(LightCone light)
		{
			Vector2 vec = this.realPos - light.Point;
			this.angOffset = (float)Math.Atan2(vec.y, vec.x);

			if (this.angOffset < 0)
			{
				this.angOffset += 2f * (float)Math.PI; //set from 0 to 2pi (now from -pi to pi)
			}

			this.distance = Math.Abs(vec.magnitude);
		}

		/// <summary>
		/// </summary>
		/// <param name="startAngle"></param>
		/// <param name="endAngle"></param>
		/// <returns></returns>
		public bool FitToAngles(float startAngle, float endAngle)
		{
			//clamp start angles
			float PI2 = (float)Math.PI * 2f;

			var ranges = new List<Vector2>();
			if (startAngle < 0)
			{
				ranges.Add(new Vector2(startAngle + PI2, PI2));
				startAngle = 0;
			}
			if (endAngle > PI2)
			{
				ranges.Add(new Vector2(0, endAngle - PI2));
				endAngle = PI2;
			}
			ranges.Add(new Vector2(startAngle, endAngle));

			//string tempres = "";
			//foreach (var rng in ranges) tempres += rng.ToString() + "  >>  ";
			//Debug.Log(tempres);

			//test if it fits
			for (int i = 0; i < ranges.Count; i++)
			{
				if (this.angOffset >= ranges[i].x && this.angOffset <= ranges[i].y)
				{
					return true;
				}
			}

			//if not in ranges, return false
			return false;
		}

		public Vector3 GetVec3()
		{
			return new Vector3(this.realPos.x, this.realPos.y, 0);
		}

		#endregion
	}
}