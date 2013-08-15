using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Threading;
using System.Xml.Linq;
using System.Xml.Serialization;

using Assets.TiledTest;
using Assets.TiledTest.light;
using Assets.Unsprite;

using UnityEngine;

public class TiledLevel : MonoBehaviour
{
	#region Fields

	//level description
	public List<List<TileInfo>> Tiles;

	public List<Lightsource> Lights;


	//other stuff
	public float GlobalScale = 10f;

	private GameObject hero;

	public uint Mode = 0; //0 for tile, 1 for physx

	private float QuadSize = 32;

	private Vector2 atlasOffset = new Vector2(10, 80);

	private Vector2 atlasSize = new Vector2(300, 300);

	private Texture atlasTexture;

	private Vector2 pickedTile = new Vector2(0, 0);

	private Texture physxTexture;

	private uint pickedPhysx = 0;

	private bool bPlaymode = false;


	public Shader DarkShader;
	public Shader LightShader;

	public RenderTexture TargetTexture;

	#endregion

	#region Methods


	/// <summary>
	/// adds light source with it's own camera
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="maxRadius"></param>
	/// <param name="initAngle"></param>
	/// <param name="angleRange"></param>
	private void AddLightCone(Vector2 pos, float maxRadius, float initAngle, float angleRange)
	{
		LightCone nlc = new LightCone(pos, maxRadius, initAngle, angleRange);
		Lights.Add(nlc);

		GameObject nc = new GameObject();
		Camera cam = nc.AddComponent<Camera>();
		cam.tag = "lightcam";
		cam.orthographic = true;
		cam.backgroundColor = new Color(0,0,0,0);
		cam.name = "lightcam";
		cam.aspect = (float)Screen.width/(float)Screen.height;
		cam.depth = 10; //after main, but before post

		RenderLight rl = nc.AddComponent<RenderLight>();
		rl.RenderTexture = new RenderTexture(Screen.width, Screen.height, 16);
		rl.DarkShader = DarkShader;
		rl.LightShader = LightShader;
		rl.ParentLight = nlc;

		cam.targetTexture = rl.RenderTexture;

		//this is my customized blur shader, which is now capable to work with c#
		Component blur = nc.AddComponent("Blur");
		blur.SendMessage("SetBlurShader", Shader.Find("Hidden/FastBlur"));

		nlc.renderCamera = cam;
	}


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
		}
		return res;
	}

	private int DCU(float coord, bool countLast = true)
	{
		int res = (int)Math.Ceiling(coord / this.GlobalScale);
		if (res == coord / this.GlobalScale && !countLast)
		{
			res += 1; //if not rounded, then don't count last quad
		}
		return res;
	}

	private Rect GetCharAABB(PhysxSprite ps)
	{
		var aabb = new Rect(
			ps.Position.x + this.GlobalScale / 2f,
			ps.Position.y,
			this.GlobalScale * ps.Size.x - this.GlobalScale,
			this.GlobalScale * ps.Size.y - this.GlobalScale * 0.2f);
		return aabb;
	}

	//in absolute tile coords
	private TileType GetObstacle(int scX, int scY)
	{
		if (scX < 0 || scX > this.Tiles.Count || scY > this.Tiles[scX].Count || scY < 0)
		{
			return TileType.None;
		}
		return this.Tiles[scX][scY].Type;
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
		spr.SetChromokey(0, 67, 88);
	}


	/// <summary>
	/// 
	/// </summary>
	private void RespawnCharacter()
	{
		var spr = hero.GetComponent<PhysxSprite>();
		spr.Position = new Vector2(0, 0);
		spr.Speed = new Vector2(0, 0);
	}


	/// <summary>
	/// 
	/// </summary>
	private void SaveLevel()
	{
		// save level
		BinaryFormatter frmt = new BinaryFormatter();
		MemoryStream stream = new MemoryStream();

		frmt.Serialize(stream, this.Tiles);
		frmt.Serialize(stream, TileInfo.instocount);

		File.WriteAllBytes("out.txt", stream.GetBuffer());
		stream.Dispose();
	}


	/// <summary>
	/// 
	/// </summary>
	private void LoadLevel()
	{
		BinaryFormatter frmt = new BinaryFormatter();
		MemoryStream stream = new MemoryStream(File.ReadAllBytes("out.txt"));

		this.Tiles = frmt.Deserialize(stream) as List<List<TileInfo>>;
		TileInfo.instocount = (uint)frmt.Deserialize(stream);

		stream.Dispose();
		this.InitLevel();
	}


	private void NewLevel()
	{
		this.Tiles = new List<List<TileInfo>>();
	}


	// init atlass and destroy atlas
	private void InitAtlas()
	{
		atlasTexture = Resources.Load("Atlases/test") as Texture;
		physxTexture = Resources.Load("Atlases/physics") as Texture;
	}

	/// <summary>
	/// afterload init
	/// </summary>
	private void InitLevel()
	{
		int i, j;

		// afterload optimization
		if (this.Tiles == null) return;

		for (i = 0; i < this.Tiles.Count; i++)
		{
			for (j = 0; j < this.Tiles[i].Count; j++)
			{
				TileInfo inf = this.Tiles[i][j];
				this.CreateNewSprite(inf, new Vector2(i * this.GlobalScale, j * this.GlobalScale));
			}
		}
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="inf"></param>
	/// <param name="pos"></param>
	private void CreateNewSprite(TileInfo inf, Vector2 pos)
	{
		//create
		var go = new GameObject();
		var sprite = go.AddComponent<Sprite>();

		go.name = "tile_" + inf.ID;

		sprite.SetTexture("Atlases/" + inf.AtlasName);
		sprite.CreateQuadAtlas((uint)QuadSize);
		sprite.SetTile(inf.PosX + inf.PosY * 8);
		sprite.Scale = new Vector2(this.GlobalScale, this.GlobalScale);
		sprite.Position = pos;
		sprite.Layer = -100f;
		sprite.SetChromokey(163, 73, 164);
	}

	private void DeleteSprite(TileInfo inf)
	{
		DestroyImmediate(GameObject.Find("tile_" + inf.ID));
	}

	private void ClearTiles()
	{
		int i, j;

		for (i = 0; i < this.Tiles.Count; i++)
		{
			for (j = 0; j < this.Tiles[i].Count; j++)
			{
				TileInfo inf = this.Tiles[i][j];
				this.DeleteSprite(inf);
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
	/// 
	/// </summary>
	/// <param name="ps"></param>
	/// <param name="offset"></param>
	private void ResolveCollisionsXAxis(PhysxSprite ps, float offset)
	{
		// wanna move x by dx
		Rect charRect = this.GetCharAABB(ps);
		float dx = offset;
		float resDx, resDy;
		int limx;
		int setx;


		// if movement happened
		if (dx != 0)
		{
			resDx = dx;
			resDy = 0;

			if (dx > 0)
			{
				setx = this.DCU(charRect.xMin, false);
				limx = this.DCD(charRect.xMax + dx);
			}
			else //dx < 0
			{
				setx = this.DCD(charRect.xMax, false);
				limx = this.DCD(charRect.xMin + dx);
			}

			//Debug.Log("check x from " + setx + " to " + (limx - 1));

			for (int i = setx; i != limx + Math.Sign(dx); i += Math.Sign(dx))
			{
				for (int j = this.DCD(charRect.yMin, false); j <= this.DCD(charRect.yMax, false); j++)
				{
					//if there's an obstacle, stop at it immediately
					TileType obstacle = this.GetObstacle(i, j);
					if (obstacle == TileType.Brick)
					{
						Rect tpr = this.CreateRect(i, j);
						float tempres = 0;

						if (Math.Abs(tpr.xMin - charRect.xMax) < Math.Abs(tpr.xMax - charRect.xMin))
						{
							tempres = tpr.xMin - charRect.xMax;
						}
						else
						{
							tempres = tpr.xMax - charRect.xMin;
						}

						if ((Math.Sign(resDx) == Math.Sign(tempres) || tempres == 0) && Math.Abs(tempres) < Math.Abs(resDx))
						{
							resDx = tempres;
						}
					}

					else if (obstacle == TileType.Slope1)
					{
						//advance y position, no difference in x
						if (dx < 0)
						{
							Rect tpr = this.CreateRect(i, j);
							float tempres = 0;
							float leftx = charRect.xMin + dx;

							if (leftx < tpr.xMin)
							{
								tempres = tpr.yMin - charRect.yMax;
							}
							else if (leftx > tpr.xMax)
							{
								tempres = 0;
							}
							else
							{
								float shouldbe = (j + 1) * GlobalScale - (tpr.xMax - leftx);
								tempres = shouldbe - charRect.yMax;
								ps.Grounded = true;
							}

							if (tempres < 0) resDy = tempres; //float upwards
						}

					}
					else if (obstacle == TileType.Slope2)
					{
						//advance y position, no difference in x
						if (dx > 0)
						{
							Rect tpr = this.CreateRect(i, j);
							float tempres = 0;
							float rightx = charRect.xMax + dx;

							if (rightx > tpr.xMax)
							{
								tempres = tpr.yMin - charRect.yMax;
							}
							else if (rightx < tpr.xMin)
							{
								tempres = 0;
							}
							else
							{
								float shouldbe = (j + 1) * GlobalScale - (rightx - tpr.xMin);
								tempres = shouldbe - charRect.yMax;
								ps.Grounded = true;
							}

							if (tempres < 0) resDy = tempres; //float upwards
						}
					}
					else if (obstacle == TileType.Slope4)
					{
						//advance y position, no difference in x
						if (dx > 0)
						{
							Rect tpr = this.CreateRect(i, j);
							float tempres = 0;
							float leftx = charRect.xMin + dx;

							if (leftx < tpr.xMin)
							{
								tempres = tpr.yMax - charRect.yMin;
							}
							else if (leftx > tpr.xMax)
							{
								tempres = 0;
							}
							else
							{
								float shouldbe = j * GlobalScale + (tpr.xMax - leftx);
								tempres = shouldbe - charRect.yMin;
								ps.Grounded = true;
							}

							if (tempres > 0) resDy = tempres; //float downwards
						}

					}
					else if (obstacle == TileType.Slope3)
					{
						//advance y position, no difference in x
						if (dx > 0)
						{
							Rect tpr = this.CreateRect(i, j);
							float tempres = 0;
							float rightx = charRect.xMax + dx;

							if (rightx > tpr.xMax)
							{
								tempres = tpr.yMax - charRect.yMin;
							}
							else if (rightx < tpr.xMin)
							{
								tempres = 0;
							}
							else
							{
								float shouldbe = j * GlobalScale - (rightx - tpr.xMin);
								tempres = shouldbe - charRect.yMin;
								ps.Grounded = true;
							}

							if (tempres > 0) resDy = tempres; //float upwards
						}
					}
					else if (obstacle == TileType.Ladder)
					{
						ps.bOnLadder = true;
					}
					else if (obstacle == TileType.Water)
					{
						ps.bInWater = true;
					}
					//TODO: slope3, slope4
				}
			}

			ps.Position = ps.Position + new Vector2(resDx, resDy);
		}
		else //if dx == 0
		{
			for (int i = this.DCD(charRect.xMin, false); i <= this.DCD(charRect.yMax, false); i ++)
			{
				for (int j = this.DCD(charRect.yMin, false); j <= this.DCD(charRect.yMax, false); j++)
				{
					//if there's an obstacle, stop at it immediately
					TileType obstacle = this.GetObstacle(i, j);
					if (obstacle == TileType.Ladder)
					{
						ps.bOnLadder = true;
					}
					else if (obstacle == TileType.Water)
					{
						ps.bInWater = true;
					}
				}

			}
		}
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="ps"></param>
	/// <param name="offset"></param>
	private void ResolveCollisionsYAxis(PhysxSprite ps, float offset)
	{
		// wanna move y by dy
		Rect charRect = this.GetCharAABB(ps);
		float dy = offset;
		float resDx, resDy;
		int limy;
		int sety;

		//if movement happened
		if (dy != 0)
		{
			bool bFound = false;

			resDy = dy;
			resDx = 0;

			if (dy > 0)
			{
				sety = this.DCU(charRect.yMin);
				limy = this.DCD(charRect.yMax + dy);
			}
			else
			{
				sety = this.DCD(charRect.yMax, false);
				limy = this.DCD(charRect.yMin + dy);
			}

			for (int i = sety; i != limy + Math.Sign(dy); i += Math.Sign(dy))
			{
				for (int j = this.DCD(charRect.xMin); j <= this.DCD(charRect.xMax, false); j++)
				{
					//if there's an obstacle, stop at it immediately
					TileType obstacle = this.GetObstacle(j, i);
					if (obstacle == TileType.Brick)
					{
						float tempres = 0;

						Rect tpr = this.CreateRect(j, i);
						if (Math.Abs(tpr.yMin - charRect.yMax) < Math.Abs(tpr.yMax - charRect.yMin))
						{
							tempres = tpr.yMin - charRect.yMax;
						}
						else
						{
							tempres = tpr.yMax - charRect.yMin;
						}

						if ((Math.Sign(resDy) == Math.Sign(tempres) || tempres == 0) && Math.Abs(tempres) < Math.Abs(resDy))
						{
							bFound = true;
							resDy = tempres;
						}
					}
					else if (obstacle == TileType.Slope1)
					{
						Rect tpr = this.CreateRect(j, i);
						float tempres = 0;

						if (charRect.xMin >= tpr.xMin && charRect.xMin <= tpr.xMax)
						{
							float shouldbe = (i + 1) * GlobalScale - (tpr.xMax - charRect.xMin);
							tpr.yMin = shouldbe;

							if (Math.Abs(tpr.yMin - charRect.yMax) < Math.Abs(tpr.yMax - charRect.yMin))
							{
								tempres = tpr.yMin - charRect.yMax;
							}
							else
							{
								tempres = tpr.yMax - charRect.yMin;
							}
							if (Math.Abs(tempres) < 0.001) tempres = 0;

							//Debug.Log(shouldbe + " > " + charRect.yMax);
							//Debug.Log("wanted to move to " + resDy + ", but dist to slope is " + tempres);

							if ((Math.Sign(resDy) == Math.Sign(tempres) || (tempres == 0 && resDy > 0))
							    && Math.Abs(tempres) < Math.Abs(resDy) && dy > 0)
							{
								bFound = true;
								resDy = tempres;
								ps.Grounded = true;
							}
						}
					}
					else if (obstacle == TileType.Slope2)
					{
						Rect tpr = this.CreateRect(j, i);
						float tempres = 0;

						if (charRect.xMax >= tpr.xMin && charRect.xMax <= tpr.xMax)
						{
							float shouldbe = (i + 1) * GlobalScale - (charRect.xMax - tpr.xMin);
							tpr.yMin = shouldbe;

							if (Math.Abs(tpr.yMin - charRect.yMax) < Math.Abs(tpr.yMax - charRect.yMin))
							{
								tempres = tpr.yMin - charRect.yMax;
							}
							else
							{
								tempres = tpr.yMax - charRect.yMin;
							}
							if (Math.Abs(tempres) < 0.001) tempres = 0;

							//Debug.Log(shouldbe + " > " + charRect.yMax);
							//Debug.Log("wanted to move to " + resDy + ", but dist to slope is " + tempres);

							if ((Math.Sign(resDy) == Math.Sign(tempres) || (tempres == 0 && resDy > 0))
							    && Math.Abs(tempres) < Math.Abs(resDy) && dy > 0)
							{
								bFound = true;
								resDy = tempres;
								ps.Grounded = true;
							}
						}
					}
					else if (obstacle == TileType.Slope4)
					{
						Rect tpr = this.CreateRect(j, i);
						float tempres = 0;

						if (charRect.xMin >= tpr.xMin && charRect.xMin <= tpr.xMax)
						{
							float shouldbe = i * GlobalScale + (tpr.xMax - charRect.xMin);
							tpr.yMin = shouldbe;

							if (Math.Abs(tpr.yMin - charRect.yMax) < Math.Abs(tpr.yMax - charRect.yMin))
							{
								tempres = tpr.yMin - charRect.yMax;
							}
							else
							{
								tempres = tpr.yMax - charRect.yMin;
							}
							if (Math.Abs(tempres) < 0.001) tempres = 0;

							//Debug.Log(shouldbe + " > " + charRect.yMax);
							//Debug.Log("wanted to move to " + resDy + ", but dist to slope is " + tempres);

							if ((Math.Sign(resDy) == Math.Sign(tempres) || (tempres == 0 && resDy < 0))
							    && Math.Abs(tempres) < Math.Abs(resDy) && dy < 0)
							{
								bFound = true;
								resDy = tempres;
							}
						}
					}
					else if (obstacle == TileType.Slope3)
					{
						Rect tpr = this.CreateRect(j, i);
						float tempres = 0;

						if (charRect.xMax >= tpr.xMin && charRect.xMax <= tpr.xMax)
						{
							float shouldbe = i * GlobalScale + (charRect.xMax - tpr.xMin);
							tpr.yMin = shouldbe;

							if (Math.Abs(tpr.yMin - charRect.yMax) < Math.Abs(tpr.yMax - charRect.yMin))
							{
								tempres = tpr.yMin - charRect.yMax;
							}
							else
							{
								tempres = tpr.yMax - charRect.yMin;
							}
							if (Math.Abs(tempres) < 0.001) tempres = 0;

							//Debug.Log(shouldbe + " > " + charRect.yMax);
							//Debug.Log("wanted to move to " + resDy + ", but dist to slope is " + tempres);

							if ((Math.Sign(resDy) == Math.Sign(tempres) || (tempres == 0 && resDy < 0))
							    && Math.Abs(tempres) < Math.Abs(resDy) && dy < 0)
							{
								bFound = true;
								resDy = tempres;
							}
						}
					}
					else if (obstacle == TileType.Ladder)
					{
						ps.bOnLadder = true;
					}
					else if (obstacle == TileType.Water)
					{
						ps.bInWater = true;
					}
				}
			}

			ps.Position = ps.Position + new Vector2(resDx, resDy);
			if (resDy < dy && dy > 0 && bFound)
			{
				ps.Grounded = true;
			}
			else if (dy < 0 && resDy > dy && bFound)
			{
				ps.IsJumping = false;
				if (ps.Speed.y < 0) ps.Speed.y = 0;
			}

		}
		else //if dy == 0
		{
			for (int i = this.DCD(charRect.yMin); i < this.DCU(charRect.yMax, false); i++)
			{
				for (int j = this.DCD(charRect.xMin); j <= this.DCD(charRect.xMax, false); j++)
				{
					TileType obstacle = this.GetObstacle(j, i);

					if (obstacle == TileType.Ladder)
					{
						ps.bOnLadder = true;
					}
					else if (obstacle == TileType.Water)
					{
						ps.bInWater = true;
					}
				}
			}
		}
	}


	/// <summary>
	/// </summary>
	/// <param name="ps"></param>
	private void ResolveCollisions(PhysxSprite ps)
	{
		//cap speed
		if (ps.Speed.y > 30f * this.GlobalScale)
		{
			ps.Speed.y = 30f * this.GlobalScale;
		}

		float dx = ps.Speed.x * Time.deltaTime;
		float dy = ps.Speed.y * Time.deltaTime;


		//Debug.Log(dy);

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
		ps.bOnLadder = false;
		ps.bInWater = false;

		this.ResolveCollisionsXAxis(ps, dx);
		this.ResolveCollisionsYAxis(ps, dy);
	}



	/// <summary>
	/// 
	/// </summary>
	private void Start()
	{
		TargetTexture = new RenderTexture(Screen.width, Screen.height, 16);
		Camera.main.targetTexture = TargetTexture;

		this.Tiles = new List<List<TileInfo>>();
		this.Lights = new List<Lightsource>();

		this.InitAtlas();

		this.InitLevel();
		this.InitCharacters();

		//init lights
		this.AddLightCone(new Vector2(170, -30), 100, (float)(Math.PI / 2), (float)(Math.PI / 4f));
		this.AddLightCone(new Vector2(40, 0), 100, (float)(Math.PI / 2), (float)(Math.PI / 4f));
	}



	/// <summary>
	/// 
	/// </summary>
	private void UpdateControls()
	{
		float gravity = 147.8f;

		// set some variables
		var sprman = Camera.main.GetComponent<SpritesManager>();
		sprman.Sprites.ForEach(el => el.SetChromokey(163, 73, 164));

		// run physics
		var ps = this.hero.GetComponent<PhysxSprite>();
		ps.SetChromokey(0, 67, 88);

		if (ps.Grounded)
		{
			ps.Speed.y = gravity * Time.deltaTime * this.GlobalScale;
		}
		if (ps.bInWater)
		{
			ps.Speed.y += gravity / 15f * Time.deltaTime * this.GlobalScale;
		}
		else ps.Speed.y += gravity * Time.deltaTime * this.GlobalScale;
		//no gravity on ladder


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

		//jumping from ground
		if ((Input.GetKeyDown(KeyCode.Space)) && ps.Grounded)
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

		//jumping in ladder
		if ((Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W)) && ps.bOnLadder)
		{
			ps.Speed.y = -4.8f * this.GlobalScale;
		}
		else if (Input.GetKey(KeyCode.S) && ps.bOnLadder)
		{
			ps.Speed.y = 4.8f * this.GlobalScale;
		}
		else if (ps.bOnLadder)
		{
			ps.Speed.y = 0;
		}

		//jumping in water
		if ((Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W)) && ps.bInWater)
		{
			ps.Speed.y = -3f * this.GlobalScale;
		}


		//set animation
		ps.PlayAnimationIfNotTheSame(currentAnimation);

		// CD and resolving
		this.CollisionDetection();
	}


	/// <summary>
	/// 
	/// </summary>
	private void Update()
	{
		//middlebtn down
		if (Input.GetMouseButton(2))
		{
			Camera.main.transform.Translate(-Input.GetAxis("Mouse X") * 10, -Input.GetAxis("Mouse Y") * 10, 0);
		}


		//left mouse down
		if (Input.GetMouseButton(0))
		{
			//toolbar
			if (Input.mousePosition.x <= 350)
			{
				float mpy = Screen.height - Input.mousePosition.y;

				//atlas pick
				if (Input.mousePosition.x < atlasOffset.x + atlasSize.x && Input.mousePosition.x > atlasOffset.x
				    && mpy < atlasOffset.y + atlasSize.y && mpy > atlasOffset.y)
				{
					float textureStep = QuadSize * atlasSize.x / atlasTexture.width;
					pickedTile = new Vector2(
						x: (float)Math.Floor((Input.mousePosition.x - this.atlasOffset.x) / textureStep),
						y: (float)Math.Floor((mpy - this.atlasOffset.y) / textureStep));

					this.Mode = 0;
				}

					//physx pick
				else if (Input.mousePosition.x < atlasOffset.x + atlasSize.x && Input.mousePosition.x > atlasOffset.x
				         && mpy > atlasOffset.y + atlasSize.y + 10 && mpy < atlasOffset.y + atlasSize.y + 60)
				{
					float textureStep = atlasSize.x / 8f;
					pickedPhysx = (uint)Math.Floor((Input.mousePosition.x - this.atlasOffset.x) / textureStep);

					this.Mode = 1;
				}

			}
				//click field
			else
			{
				//worldspace coordinates
				Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

				Vector2 picked = new Vector2(
					x: (float)Math.Floor(mouseWorld.x / this.GlobalScale),
					y: (float)Math.Floor(mouseWorld.y / this.GlobalScale));


				if (this.Mode == 0) //tile editing
				{
					this.SetTileGraphics(picked);
				}
				else //physx editing
				{
					this.SetTilePhysics(picked);
				}
			}
		}


		//right mouse down
		if (Input.GetMouseButton(1))
		{
			LightCone light = Lights[0] as LightCone;
			Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

			//direct light
			light.Angle = (float)Math.Atan2(mouseWorld.y - light.Point.y, mouseWorld.x - light.Point.x);
			light.MaxRadius = (new Vector2(mouseWorld.x, mouseWorld.y) - light.Point).magnitude;
			if (light.Angle < 0) light.Angle += (float)Math.PI * 2f;
		}


		if (bPlaymode)
		{
			this.UpdateControls();
			//camera follow hero
			Camera.main.transform.position = new Vector3(
				hero.transform.position.x,
				hero.transform.position.y,
				Camera.main.transform.position.z);
		}


		GameObject[] lightcams = GameObject.FindGameObjectsWithTag("lightcam");
		foreach (var cam in lightcams)
		{
			cam.transform.position = Camera.main.transform.position;
			cam.transform.rotation = Camera.main.transform.rotation;
			(cam.GetComponent<Camera>()).orthographicSize = Camera.main.orthographicSize;
		}

	}

	#endregion


	/// <summary>
	/// extends current level to picked size
	/// </summary>
	/// <param name="picked"></param>
	private void ExtendSizesOfField(Vector2 picked)
	{
		int i, j;

		//extend by x
		for (i = this.Tiles.Count - 1; i < picked.x; i++)
		{
			this.Tiles.Add(new List<TileInfo>());
		}

		//extend by y
		for (j = 0; j < this.Tiles.Count; j++)
		{
			for (i = this.Tiles[j].Count - 1; i < picked.y; i++)
			{
				TileInfo inf = new TileInfo("test", 0, 0, TileType.None);
				this.Tiles[j].Add(inf);
				this.CreateNewSprite(inf, new Vector2(j * GlobalScale, (i + 1) * GlobalScale));
			}
		}
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="picked">coordinates of picled tile</param>
	public void SetTileGraphics(Vector2 picked)
	{
		this.ExtendSizesOfField(picked);

		//Debug.Log(picked);

		//delete old if exists
		this.DeleteSprite(this.Tiles[(int)picked.x][(int)picked.y]);

		//create new one
		TileInfo inf = new TileInfo("test", (uint)this.pickedTile.x, (uint)this.pickedTile.y, TileType.None);
		this.Tiles[(int)picked.x][(int)picked.y] = inf;
		this.CreateNewSprite(
			this.Tiles[(int)picked.x][(int)picked.y],
			new Vector2(picked.x * GlobalScale, picked.y * GlobalScale));
	}

	public void SetTilePhysics(Vector2 picked)
	{
		//check if exists
		if (picked.x >= Tiles.Count || picked.y >= Tiles[(int)picked.x].Count) return;

		this.Tiles[(int)picked.x][(int)picked.y].Type = (TileType)pickedPhysx;
		Debug.Log("set " + picked + " to " + (TileType)pickedPhysx);
	}



	/// <summary>
	/// 
	/// </summary>
	public void OnGUI()
	{
		if (hero.GetComponent<PhysxSprite>().Grounded == true) GUI.Box(new Rect(360, 10, 10, 10), "G");

		GUI.Box(new Rect(0, 0, 330, Screen.height), "Editor menu");

		//savelevel
		if (GUI.Button(new Rect(10, 10, 70, 30), "Save"))
		{
			this.SaveLevel();
		}

		//loadlevel
		if (GUI.Button(new Rect(10, 40, 70, 30), "Load"))
		{
			this.ClearTiles();
			this.LoadLevel();
		}

		//playmode
		if (this.bPlaymode == false)
		{
			if (GUI.Button(new Rect(90, 40, 70, 30), "PlayOn"))
			{
				this.bPlaymode = true;
				this.RespawnCharacter();
			}
		}
		else
		{
			if (GUI.Button(new Rect(90, 40, 70, 30), "PlayOff"))
			{
				this.bPlaymode = false;
			}
		}


		//show atlas
		GUI.DrawTexture(
			new Rect(this.atlasOffset.x, this.atlasOffset.y, atlasSize.x, atlasSize.y),
			atlasTexture,
			ScaleMode.ScaleToFit);

		//show selected texture
		float texstepx = QuadSize / atlasTexture.width;
		float texstepy = -QuadSize / atlasTexture.height;
		GUI.DrawTextureWithTexCoords(
			new Rect(260, 20, 50, 50),
			atlasTexture,
			new Rect(pickedTile.x * texstepx, (pickedTile.y + 1) * texstepy, texstepx, -texstepy));

		//show physx
		GUI.DrawTexture(
			new Rect(this.atlasOffset.x, this.atlasOffset.y + atlasSize.y + 15, atlasSize.x, 50),
			physxTexture,
			ScaleMode.ScaleToFit);
	}
}


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
