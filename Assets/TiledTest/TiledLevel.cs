using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using Assets.TiledTest;
using Assets.Unsprite;

using UnityEngine;
using System.Collections;

public class TiledLevel : MonoBehaviour
{

	private float GlobalScale = 10f;

	//boundaries
	int minPosX = int.MaxValue;
	int minPosY = int.MaxValue;
	int maxPosX = int.MinValue;
	int maxPosY = int.MinValue;


	public enum TileType : byte
	{
		None = 0,
		Brick,
		Slope1,
		Slope2,
		Slope3,
		Slope4,
	}

	//arrays
	private Dictionary<string, TileInfo> tiles = new Dictionary<string, TileInfo>();
	private TileType[,] physx;

	private GameObject hero;


	/// <summary>
	/// 
	/// </summary>
	/// <param name="el"></param>
	private void AddTile(XElement el)
	{
		try
		{
			string name = el.Attribute("name").Value;
			string atlas = el.Attribute("atlas").Value;
			string[] pos = el.Attribute("pos").Value.Split(',');

			tiles.Add(name, new TileInfo(name, atlas, uint.Parse(pos[0]), uint.Parse(pos[1])));
		}
		catch (Exception exc)
		{
			Debug.Log("Error parsing map.");
		}
		
	}



	// Use this for initialization
	void Start ()
	{
		this.InitLevel();
		this.InitCharacters();
	}

	/// <summary>
	/// 
	/// </summary>
	void InitLevel()
	{
		uint QuadSize = 32;

		//xml parsing
		TextAsset res = Resources.Load("map") as TextAsset; //xml
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
			GameObject go = new GameObject();
			Sprite sprite = go.AddComponent<Sprite>();

			string img = el.Attribute("img").Value;
			string[] pos = el.Attribute("pos").Value.Split(',');
			string flags = el.Attribute("flags").Value;

			TileInfo inf = tiles[img];
			sprite.SetTexture("Atlases/" + inf.AtlasName);
			sprite.CreateQuadAtlas(QuadSize);
			sprite.SetTile(inf.PosX, inf.PosY);
			sprite.Scale = new Vector2(GlobalScale, GlobalScale);
			sprite.Position = new Vector2(int.Parse(pos[0]) * GlobalScale, int.Parse(pos[1]) * GlobalScale);

			//get level boundaries
			if (int.Parse(pos[0]) > maxPosX) maxPosX = int.Parse(pos[0]);
			if (int.Parse(pos[1]) > maxPosY) maxPosY = int.Parse(pos[1]);
			if (int.Parse(pos[0]) < minPosX) minPosX = int.Parse(pos[0]);
			if (int.Parse(pos[1]) < minPosY) minPosY = int.Parse(pos[1]);
		}

		//init physics
		physx = new TileType[maxPosX - minPosX + 1, maxPosY - minPosY + 1];
		foreach (XElement el in data.Elements())
		{
			string flags = el.Attribute("flags").Value;
			string[] pos = el.Attribute("pos").Value.Split(',');

			int posx = int.Parse(pos[0]) - (int)minPosX;
			int posy = int.Parse(pos[1]) - (int)minPosY;

			if (flags == "impassible")
			{
				physx[posx, posy] = TileType.Brick;
			}
			else if (flags == "slope1")
			{
				physx[posx, posy] = TileType.Slope1;
			}
			else if (flags == "slope2")
			{
				physx[posx, posy] = TileType.Slope2;
			}
			else if (flags == "slope3")
			{
				physx[posx, posy] = TileType.Slope3;
			}
			else if (flags == "slope4")
			{
				physx[posx, posy] = TileType.Slope4;
			}
			else physx[posx, posy] = TileType.None;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	void InitCharacters()
	{
		hero = new GameObject();
		hero.name = "AxeBattler";
		PhysxSprite spr = hero.AddComponent<PhysxSprite>();

		spr.SetTexture("Atlases/AxeBattler");
		spr.Size = new Vector2(2f, 3f);
		spr.Scale = new Vector2(GlobalScale*spr.Size.x, GlobalScale*spr.Size.y);

		//create animations
		spr.CreateAnimationFrames(1, 9);
		spr.AddAnimation("Stand", 0, 0, 3, -1);
		spr.AddAnimation("Walk", 1, 4, 4, -1);
		spr.AddAnimation("WalkUp", 5, 9, 4, -1);

		//setup default animation (no need actually)
		spr.PlayAnimation("Stand");
	}



	// Update is called once per frame
	void Update ()
	{
		float gravity = 147.8f;

		// set some variables
		SpritesManager sprman = Camera.main.GetComponent<SpritesManager>();
		sprman.Sprites.ForEach(el => el.SetChromokey(163,73,164));

		// run physics
		PhysxSprite ps = hero.GetComponent<PhysxSprite>();
		ps.SetChromokey(0,67,88);

		if (!ps.Grounded) ps.Speed.y += gravity * Time.deltaTime * GlobalScale;
		else ps.Speed.y = 0.1f; //just for penetration test

		string currentAnimation = "Stand";

		if (Input.GetKey(KeyCode.D))
		{
			currentAnimation = "Walk";
			ps.bReflect = false;
			ps.Speed.x = 1.8f * GlobalScale;
		}
		else if (Input.GetKey(KeyCode.A))
		{
			currentAnimation = "Walk";
			ps.bReflect = true;
			ps.Speed.x = -1.8f * GlobalScale;
		}
		else
		{
			ps.Speed.x = 0;
		}

		if (Input.GetKeyDown(KeyCode.Space) && ps.Grounded)
		{
			ps.Speed.y = -30f * GlobalScale;
			ps.IsJumping = true;
			ps.JumpingTime = 0;
		}
		else if (Input.GetKey(KeyCode.Space) && ps.IsJumping)
		{
			ps.JumpingTime += Time.deltaTime;
			if (ps.JumpingTime > 0.1) ps.IsJumping = false;
			else ps.Speed.y = -30f * GlobalScale;
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


	/// <summary>
	/// test and resolve collisions
	/// </summary>
	private void CollisionDetection()
	{	
		this.ResolveCollisions(hero.GetComponent<PhysxSprite>());
	}

	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="ps"></param>
	private void ResolveCollisions(PhysxSprite ps)
	{
		float gravity = 207.8f;

		//cap speed
		if (ps.Speed.y > 30f * GlobalScale) ps.Speed.y = 30f * GlobalScale;

		float dx = ps.Speed.x * Time.deltaTime;
		float dy = ps.Speed.y * Time.deltaTime + gravity * Time.deltaTime * Time.deltaTime / 2f;

		if (Math.Abs(dx) > GlobalScale * 1f) dx = GlobalScale * 1f * Math.Sign(dx);
		if (Math.Abs(dy) > GlobalScale * 1f) dy = GlobalScale * 1f * Math.Sign(dy);

		//set grounded false
		ps.Grounded = false;


		//first process all x-based movement
		hero.transform.Translate(dx, 0, 0);

		for (int i = 0; i < physx.GetLength(0); i++)
			for (int j = 0; j < physx.GetLength(1); j++)
			{
				TileType obstacle = this.GetObstacle(i + minPosX, j + minPosY);
				if (obstacle == TileType.Brick)
				{
					Rect temprect = this.CreateRect(i + minPosX, j + minPosY);
					Vector2 diff = ResolveCollisionBetween(GetCharAABB(ps), temprect);

					//resolve
					if ( diff.x != 0d && diff.y != 0d)
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

		//then process y-based movement
		hero.transform.Translate(0, dy, 0);

		for (int i = 0; i < physx.GetLength(0); i++)
			for (int j = 0; j < physx.GetLength(1); j++)
			{
				TileType obstacle = this.GetObstacle(i + minPosX, j + minPosY);

				if ( obstacle == TileType.Brick )
				{
					Rect temprect = this.CreateRect(i + minPosX, j + minPosY);
					Vector2 diff = ResolveCollisionBetween(GetCharAABB(ps), temprect);

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
								ps.Speed.y = 0;
							if (diff.y < 0) ps.Grounded = true;
						}
					}
				}
				else if (obstacle == TileType.Slope2)
				{
					//check right corner
					Rect ab = GetCharAABB(ps);
					Rect temprect = this.CreateRect(i + minPosX, j + minPosY);
					
					if (ab.xMax <= temprect.xMax && ab.xMax >= temprect.xMin)
					{
						float dix = ab.xMax-temprect.xMin;
						if ((ab.yMax >= temprect.yMax - dix && ab.yMax <= temprect.yMax) || 
							(ab.yMin < temprect.yMax && ab.yMax >= temprect.yMax))
						{
							float diff = ab.yMax - (temprect.yMax-dix);
							ps.Position = new Vector2(ps.Position.x, ps.Position.y - diff);
							if (ps.Speed.y > 0 && diff > 0 || ps.Speed.y < 0 && diff < 0)
								ps.Speed.y = 0;
							if (diff > 0) ps.Grounded = true;
						}
					}
				}
				else if (obstacle == TileType.Slope1)
				{
					//check right corner
					Rect ab = GetCharAABB(ps);
					Rect temprect = this.CreateRect(i + minPosX, j + minPosY);

					if (ab.xMin <= temprect.xMax && ab.xMin >= temprect.xMin)
					{
						float dix = temprect.xMax - ab.xMin;
						if ((ab.yMax >= temprect.yMax - dix && ab.yMax <= temprect.yMax) ||
							(ab.yMin < temprect.yMax && ab.yMax >= temprect.yMax))
						{
							float diff = ab.yMax - (temprect.yMax - dix);
							ps.Position = new Vector2(ps.Position.x, ps.Position.y - diff);
							if (ps.Speed.y > 0 && diff > 0 || ps.Speed.y < 0 && diff < 0)
								ps.Speed.y = 0;
							if (diff > 0) ps.Grounded = true;
						}
					}
				}
			}
				
		 
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="abR"></param>
	/// <param name="rect"></param>
	/// <returns></returns>
	private Vector2 ResolveCollisionBetween(Rect abR, Rect rect)
	{
		float dx = 0, dy = 0;

		//resolve x
		if ((abR.xMin >= rect.xMin && abR.xMax <= rect.xMax) || (abR.xMin <= rect.xMin && abR.xMax >= rect.xMax) )
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

		if (dx != 0 && dy != 0) return new Vector2(dx, dy);
		else return Vector2.zero;
	}


	private Rect GetCharAABB(PhysxSprite ps)
	{
		Rect aabb = new Rect(ps.Position.x + GlobalScale/2f, ps.Position.y, GlobalScale * ps.Size.x - GlobalScale, GlobalScale * ps.Size.y);
		return aabb;
	}

	//to discrete coordinate into tile-based space
	private float DC(float coord)
	{
		return (float)Math.Floor(coord / this.GlobalScale);
	}

	//creates a rect from tile in real coordinates
	private Rect CreateRect(int i, int j)
	{
		return new Rect(i * GlobalScale, j * GlobalScale, GlobalScale, GlobalScale);
	}

	//in absolute tile coords
	private TileType GetObstacle(int scX, int scY)
	{
		if (scX < minPosX || scX > maxPosX || scY > maxPosY || scY < minPosY)
		{
			//Debug.Log("out " + scX + " " + scY + "  > " + minPosX + ";" + maxPosX + ">" + minPosY + ";" + maxPosY + ">");
			return TileType.None;
		}
		return physx[scX - minPosX, scY - minPosY];
	}

	private TileType GetObstacle(float scX, float scY)
	{
		return this.GetObstacle((int)scX, (int)scY);
	}
}
