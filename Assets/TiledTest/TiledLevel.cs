using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Xml.Linq;

using Assets.TiledTest;
using Assets.Unsprite;

using UnityEngine;

public class TiledLevel : MonoBehaviour
{
	#region Fields

	private readonly Dictionary<string, TileInfo> tiles = new Dictionary<string, TileInfo>();

	private float GlobalScale = 10f;

	private GameObject hero;

	//boundaries

	private int maxPosX = int.MinValue;

	private int maxPosY = int.MinValue;

	private int minPosX = int.MaxValue;

	private int minPosY = int.MaxValue;

	private TileType[,] physx;

	#endregion

	#region Enums

	public enum TileType : byte
	{
		None = 0,

		Brick,

		Slope1,

		Slope2,

		Slope3,

		Slope4,
	}

	#endregion

	#region Methods

	/// <summary>
	/// </summary>
	/// <param name="el"></param>
	private void AddTile(XElement el)
	{
		try
		{
			string name = el.Attribute("name").Value;
			string atlas = el.Attribute("atlas").Value;
			string[] pos = el.Attribute("pos").Value.Split(',');

			this.tiles.Add(name, new TileInfo(name, atlas, uint.Parse(pos[0]), uint.Parse(pos[1])));
		}
		catch (Exception exc)
		{
			Debug.Log("Error parsing map.");
		}
	}

	// Use this for initialization

	/// <summary>
	///     test and resolve collisions
	/// </summary>
	private void CollisionDetection()
	{
		this.ResolveCollisions(this.hero.GetComponent<PhysxSprite>());
	}

	//creates a rect from tile in real coordinates
	private Rect CreateRect(int i, int j)
	{
		return new Rect(i * this.GlobalScale, j * this.GlobalScale, this.GlobalScale, this.GlobalScale);
	}

	private int DCD(float coord, bool countLast = true)
	{
		int res = (int)Math.Floor(coord / this.GlobalScale);
		if (res == coord / this.GlobalScale && !countLast)
		{
			res -= 1; //if not rounded, then don't count last quad
			//Debug.Log("NOT ROUNDEEED!");
		}
		return res;
	}
	private int DCU(float coord, bool countLast = true)
	{
		int res = (int)Math.Ceiling(coord / this.GlobalScale);
		if (res == coord / this.GlobalScale && !countLast)
		{
			res += 1; //if not rounded, then don't count last quad
			//Debug.Log("NOT ROUNDEEED!");
		}
		return res;
	}

	private Rect GetCharAABB(PhysxSprite ps)
	{
		var aabb = new Rect(
			ps.Position.x + this.GlobalScale / 2f,
			ps.Position.y,
			this.GlobalScale * ps.Size.x - this.GlobalScale,
			this.GlobalScale * ps.Size.y - this.GlobalScale*0.2f);
		return aabb;
	}

	//in absolute tile coords
	private TileType GetObstacle(int scX, int scY)
	{
		if (scX < this.minPosX || scX > this.maxPosX || scY > this.maxPosY || scY < this.minPosY)
		{
			//Debug.Log("out " + scX + " " + scY + "  > " + minPosX + ";" + maxPosX + ">" + minPosY + ";" + maxPosY + ">");
			return TileType.None;
		}
		return this.physx[scX - this.minPosX, scY - this.minPosY];
	}

	private TileType GetObstacle(float scX, float scY)
	{
		return this.GetObstacle((int)scX, (int)scY);
	}

	/// <summary>
	/// </summary>
	private void InitCharacters()
	{
		this.hero = new GameObject();
		this.hero.name = "AxeBattler";
		var spr = this.hero.AddComponent<PhysxSprite>();

		spr.SetTexture("Atlases/AxeBattler");
		spr.Size = new Vector2(2f, 3f);
		spr.Scale = new Vector2(this.GlobalScale * spr.Size.x, this.GlobalScale * spr.Size.y);

		//create animations
		spr.CreateAnimationFrames(1, 9);
		spr.AddAnimation("Stand", 0, 0, 3, -1);
		spr.AddAnimation("Walk", 1, 4, 4, -1);
		spr.AddAnimation("WalkUp", 5, 9, 4, -1);

		//setup default animation (no need actually)
		spr.PlayAnimation("Stand");
	}

	/// <summary>
	/// </summary>
	private void InitLevel()
	{
		uint QuadSize = 32;

		//xml parsing
		var res = Resources.Load("map") as TextAsset; //xml
		XDocument doc = XDocument.Parse(res.text);

		//parse atlases, create sprites
		XElement sprites = doc.Root.Elements().FirstOrDefault(el => el.Name == "sprites");
		foreach (XElement el in sprites.Elements())
		{
			this.AddTile(el);
		}

		//init sprites itself
		XElement data = doc.Root.Elements().FirstOrDefault(el => el.Name == "data");
		foreach (XElement el in data.Elements())
		{
			var go = new GameObject();
			var sprite = go.AddComponent<Sprite>();

			string img = el.Attribute("img").Value;
			string[] pos = el.Attribute("pos").Value.Split(',');
			string flags = el.Attribute("flags").Value;

			TileInfo inf = this.tiles[img];
			sprite.SetTexture("Atlases/" + inf.AtlasName);
			sprite.CreateQuadAtlas(QuadSize);
			sprite.SetTile(inf.PosX, inf.PosY);
			sprite.Scale = new Vector2(this.GlobalScale, this.GlobalScale);
			sprite.Position = new Vector2(int.Parse(pos[0]) * this.GlobalScale, int.Parse(pos[1]) * this.GlobalScale);

			//get level boundaries
			if (int.Parse(pos[0]) > this.maxPosX)
			{
				this.maxPosX = int.Parse(pos[0]);
			}
			if (int.Parse(pos[1]) > this.maxPosY)
			{
				this.maxPosY = int.Parse(pos[1]);
			}
			if (int.Parse(pos[0]) < this.minPosX)
			{
				this.minPosX = int.Parse(pos[0]);
			}
			if (int.Parse(pos[1]) < this.minPosY)
			{
				this.minPosY = int.Parse(pos[1]);
			}
		}

		//init physics
		this.physx = new TileType[this.maxPosX - this.minPosX + 1, this.maxPosY - this.minPosY + 1];
		foreach (XElement el in data.Elements())
		{
			string flags = el.Attribute("flags").Value;
			string[] pos = el.Attribute("pos").Value.Split(',');

			int posx = int.Parse(pos[0]) - this.minPosX;
			int posy = int.Parse(pos[1]) - this.minPosY;

			if (flags == "impassible")
			{
				this.physx[posx, posy] = TileType.Brick;
			}
			else if (flags == "slope1")
			{
				this.physx[posx, posy] = TileType.Slope1;
			}
			else if (flags == "slope2")
			{
				this.physx[posx, posy] = TileType.Slope2;
			}
			else if (flags == "slope3")
			{
				this.physx[posx, posy] = TileType.Slope3;
			}
			else if (flags == "slope4")
			{
				this.physx[posx, posy] = TileType.Slope4;
			}
			else
			{
				this.physx[posx, posy] = TileType.None;
			}
		}
	}

	/// <summary>
	/// </summary>
	/// <param name="abR"></param>
	/// <param name="rect"></param>
	/// <returns></returns>
	private Vector2 ResolveCollisionBetween(Rect abR, Rect rect)
	{
		float dx = 0, dy = 0;

		//resolve x
		if ((abR.xMin >= rect.xMin && abR.xMax <= rect.xMax) || (abR.xMin <= rect.xMin && abR.xMax >= rect.xMax))
		{
			dx = float.MaxValue;
		}
		else if (abR.xMin >= rect.xMin && abR.xMin <= rect.xMax)
		{
			dx = rect.xMax - abR.xMin;
		}
		else if (abR.xMax >= rect.xMin && abR.xMax <= rect.xMax)
		{
			dx = rect.xMin - abR.xMax;
		}

		//resolve y
		if ((abR.yMin >= rect.yMin && abR.yMax <= rect.yMax) || (abR.yMin <= rect.yMin && abR.yMax >= rect.yMax))
		{
			dy = float.MaxValue;
		}
		else if (abR.yMin >= rect.yMin && abR.yMin <= rect.yMax)
		{
			dy = rect.yMax - abR.yMin;
		}
		else if (abR.yMax >= rect.yMin && abR.yMax <= rect.yMax)
		{
			dy = rect.yMin - abR.yMax;
		}

		if (dx != 0 && dy != 0)
		{
			return new Vector2(dx, dy);
		}
		return Vector2.zero;
	}

	/// <summary>
	/// </summary>
	/// <param name="ps"></param>
	private void ResolveCollisions(PhysxSprite ps)
	{
		float gravity = 207.8f;

		//cap speed
		if (ps.Speed.y > 30f * this.GlobalScale)
		{
			ps.Speed.y = 30f * this.GlobalScale;
		}

		float dx = ps.Speed.x * Time.deltaTime;
		float dy = ps.Speed.y * Time.deltaTime + gravity * Time.deltaTime * Time.deltaTime / 2f;
		float resDx = 0;
		float resDy = 0;


		if (Math.Abs(dx) > this.GlobalScale * 1f)
		{
			dx = this.GlobalScale * 1f * Math.Sign(dx);
		}
		if (Math.Abs(dy) > this.GlobalScale * 1f)
		{
			dy = this.GlobalScale * 1f * Math.Sign(dy);
		}

		//set grounded false
		ps.Grounded = false;


		// new-way CCD
	
		// wanna move x by dx
		Rect tempRect = this.GetCharAABB(ps);

		if (dx != 0)
		{
			int limx;
			int setx;
			resDx = dx;

			if (dx > 0)
			{
				setx = this.DCU(tempRect.xMin, false);
				limx = this.DCD(tempRect.xMax + dx);	
			}
			else //dx < 0
			{
				setx = this.DCD(tempRect.xMax, false);
				limx = this.DCD(tempRect.xMin + dx);
			}

			//Debug.Log("check x from " + setx + " to " + (limx - 1));

			for (int i = setx; i != limx + Math.Sign(dx); i+= Math.Sign(dx))
			{
				for (int j = this.DCD(tempRect.yMin, false); j <= this.DCD(tempRect.yMax, false); j++)
				{
					//if there's an obstacle, stop at it immediately
					TileType obstacle = this.GetObstacle(i, j);
					if (obstacle == TileType.Brick)
					{
						Rect tpr = this.CreateRect(i, j);
						float tempres = 0;

						if (Math.Abs(tpr.xMin - tempRect.xMax) < Math.Abs(tpr.xMax - tempRect.xMin))
						{
							tempres = tpr.xMin - tempRect.xMax;
						}
						else
						{
							tempres = tpr.xMax - tempRect.xMin;
						}

						if ((Math.Sign(resDx) == Math.Sign(tempres) || tempres == 0) && Math.Abs(tempres) < Math.Abs(resDx))
						{
							resDx = tempres;
							//Debug.Log(i + ";" + j + " pushed " + tempres + " (move) " + dx);
						}
						//else Debug.Log(i+";"+j+" tried to push "+tempres + " (move) " + dx);
					}
				}
			}

			this.hero.transform.Translate(resDx, 0, 0);
			//Debug.Log("resdx " + resDx);
		}


		// wanna move y by dy
		tempRect = this.GetCharAABB(ps);
		
		if (dy != 0)
		{
			bool bFound = false;
			int limy;
			int sety;

			resDy = dy;

			if (dy > 0)
			{
				sety = this.DCU(tempRect.yMin);
				limy = this.DCD(tempRect.yMax + dy);
			}
			else
			{
				sety = this.DCD(tempRect.yMax, false);
				limy = this.DCD(tempRect.yMin + dy);
			}

			//Debug.Log("check y from " + sety + " to " + (limy-1));

			for (int i = sety; i != limy + Math.Sign(dy); i+= Math.Sign(dy))
			{
				for (int j = this.DCD(tempRect.xMin); j <= this.DCD(tempRect.xMax); j++)
				{
					//if there's an obstacle, stop at it immediately
					TileType obstacle = this.GetObstacle(j, i);
					if (obstacle == TileType.Brick)
					{
						float tempres = 0;

						Rect tpr = this.CreateRect(j, i);
						if (Math.Abs(tpr.yMin - tempRect.yMax) < Math.Abs(tpr.yMax - tempRect.yMin))
						{
							tempres = tpr.yMin - tempRect.yMax;
						}
						else
						{
							tempres = tpr.yMax - tempRect.yMin;
						}

						if ((Math.Sign(resDy) == Math.Sign(tempres) || tempres == 0) && Math.Abs(tempres) < Math.Abs(resDy))
						{
							bFound = true;
							resDy = tempres;
							//Debug.Log(i + ";" + j + " pushed " + tempres + " (move) " + dy);
						}
						//else Debug.Log(i+";"+j+" tried to push "+tempres + " (move) " + dy);
					}
				}
			}

			//Debug.Log("resy " + resDy + ", pos " + ps.Position.y);

			this.hero.transform.Translate(0, resDy, 0);
			if (resDy <= 0 && bFound)
			{
				ps.Grounded = true;
			}
			
		}
	}



	private void Start()
	{
		this.InitLevel();
		this.InitCharacters();
	}

	private void Update()
	{
		float gravity = 147.8f;

		// set some variables
		var sprman = Camera.main.GetComponent<SpritesManager>();
		sprman.Sprites.ForEach(el => el.SetChromokey(163, 73, 164));

		// run physics
		var ps = this.hero.GetComponent<PhysxSprite>();
		ps.SetChromokey(0, 67, 88);

		if (!ps.Grounded)
		{
			ps.Speed.y += gravity * Time.deltaTime * this.GlobalScale;
		}
		else
		{
			ps.Speed.y = 0.1f; //just for penetration test
		}

		string currentAnimation = "Stand";

		if (Input.GetKey(KeyCode.D))
		{
			currentAnimation = "Walk";
			ps.bReflect = false;
			ps.Speed.x = 1.8f * this.GlobalScale;
		}
		else if (Input.GetKey(KeyCode.A))
		{
			currentAnimation = "Walk";
			ps.bReflect = true;
			ps.Speed.x = -1.8f * this.GlobalScale;
		}
		else
		{
			ps.Speed.x = 0;
		}

		if (Input.GetKeyDown(KeyCode.Space) && ps.Grounded)
		{
			ps.Speed.y = -30f * this.GlobalScale;
			ps.IsJumping = true;
			ps.JumpingTime = 0;
		}
		else if (Input.GetKey(KeyCode.Space) && ps.IsJumping)
		{
			ps.JumpingTime += Time.deltaTime;
			if (ps.JumpingTime > 0.1)
			{
				ps.IsJumping = false;
			}
			else
			{
				ps.Speed.y = -30f * this.GlobalScale;
			}
		}
		else
		{
			ps.IsJumping = false;
		}

		//set animation
		ps.PlayAnimationIfNotTheSame(currentAnimation);

		// CD and resolving
		this.CollisionDetection();
	}

	#endregion



	/*
	 //first process all x-based movement
		this.hero.transform.Translate(dx, 0, 0);

		for (int i = 0; i < this.physx.GetLength(0); i++)
		{
			for (int j = 0; j < this.physx.GetLength(1); j++)
			{
				TileType obstacle = this.GetObstacle(i + this.minPosX, j + this.minPosY);
				if (obstacle == TileType.Brick)
				{
					Rect temprect = this.CreateRect(i + this.minPosX, j + this.minPosY);
					Vector2 diff = this.ResolveCollisionBetween(this.GetCharAABB(ps), temprect);

					//resolve
					if (diff.x != 0d && diff.y != 0d)
					{
						if (diff.x == float.MaxValue && diff.y == float.MaxValue)
						{
							//consumption
							Debug.Log("Total consumption. REDO!!!");
						}

						if (diff.x != float.MaxValue)
						{
							ps.Position = new Vector2(ps.Position.x + diff.x, ps.Position.y);
							ps.Speed.x = 0;
						}
					}
				}
			}
		}

		//then process y-based movement
		this.hero.transform.Translate(0, dy, 0);

		for (int i = 0; i < this.physx.GetLength(0); i++)
		{
			for (int j = 0; j < this.physx.GetLength(1); j++)
			{
				TileType obstacle = this.GetObstacle(i + this.minPosX, j + this.minPosY);

				if (obstacle == TileType.Brick)
				{
					Rect temprect = this.CreateRect(i + this.minPosX, j + this.minPosY);
					Vector2 diff = this.ResolveCollisionBetween(this.GetCharAABB(ps), temprect);

					//resolve
					if (diff.x != 0d && diff.y != 0d)
					{
						if (diff.x == float.MaxValue && diff.y == float.MaxValue)
						{
							//consumption
							Debug.Log("Total consumption. REDO!!!");
						}

						if (diff.y != float.MaxValue)
						{
							ps.Position = new Vector2(ps.Position.x, ps.Position.y + diff.y);
							if (ps.Speed.y > 0 && diff.y < 0 || ps.Speed.y < 0 && diff.y > 0)
							{
								ps.Speed.y = 0;
							}
							if (diff.y < 0)
							{
								ps.Grounded = true;
							}
						}
					}
				}
				else if (obstacle == TileType.Slope2)
				{
					//check right corner
					Rect ab = this.GetCharAABB(ps);
					Rect temprect = this.CreateRect(i + this.minPosX, j + this.minPosY);

					if (ab.xMax <= temprect.xMax && ab.xMax >= temprect.xMin)
					{
						float dix = ab.xMax - temprect.xMin;
						if ((ab.yMax >= temprect.yMax - dix && ab.yMax <= temprect.yMax)
						    || (ab.yMin < temprect.yMax && ab.yMax >= temprect.yMax))
						{
							float diff = ab.yMax - (temprect.yMax - dix);
							ps.Position = new Vector2(ps.Position.x, ps.Position.y - diff);
							if (ps.Speed.y > 0 && diff > 0 || ps.Speed.y < 0 && diff < 0)
							{
								ps.Speed.y = 0;
							}
							if (diff > 0)
							{
								ps.Grounded = true;
							}
						}
					}
				}
				else if (obstacle == TileType.Slope1)
				{
					//check right corner
					Rect ab = this.GetCharAABB(ps);
					Rect temprect = this.CreateRect(i + this.minPosX, j + this.minPosY);

					if (ab.xMin <= temprect.xMax && ab.xMin >= temprect.xMin)
					{
						float dix = temprect.xMax - ab.xMin;
						if ((ab.yMax >= temprect.yMax - dix && ab.yMax <= temprect.yMax)
						    || (ab.yMin < temprect.yMax && ab.yMax >= temprect.yMax))
						{
							float diff = ab.yMax - (temprect.yMax - dix);
							ps.Position = new Vector2(ps.Position.x, ps.Position.y - diff);
							if (ps.Speed.y > 0 && diff > 0 || ps.Speed.y < 0 && diff < 0)
							{
								ps.Speed.y = 0;
							}
							if (diff > 0)
							{
								ps.Grounded = true;
							}
						}
					}
				}
			}
		}
	 */
}