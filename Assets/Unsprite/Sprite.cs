namespace Assets.Unsprite
{
	using System;
	using System.Collections.Generic;

	using UnityEngine;

	public class Sprite : MonoBehaviour
	{
		#region Fields

		public float Layer = 1;

		public string TextureName;

		public bool bReflect = false;

		//cool vars

		//animation stuff
		private readonly Dictionary<string, SpriteAnimationData> animations = new Dictionary<string, SpriteAnimationData>();

		private bool bAnimPlaying;

		private string currentAnimation;

		private uint currentFrame;

		private int currentLoops;

		private double currentTimer;

		private uint quadAtlasSize;

		private Vector2 sizes = Vector2.one;

		private float texStep = 0;

		#endregion

		#region Public Properties

		public Material Material { get; private set; }

		public Vector2 Position
		{
			get
			{
				return new Vector2(this.transform.position.x, this.transform.position.y);
			}
			set
			{
				this.transform.position = new Vector3(value.x, value.y, 1);
			}
		}

		public float Rotation
		{
			get
			{
				return this.transform.eulerAngles.z;
			}
			set
			{
				this.transform.eulerAngles = new Vector3(0, 0, value);
			}
		}

		public Vector2 Scale
		{
			get
			{
				return new Vector2(this.transform.localScale.x, this.transform.localScale.y);
			}
			set
			{
				this.transform.localScale = new Vector3(value.x, value.y, 1);
			}
		}

		public SpritesManager SpritesManager { get; private set; }

		#endregion

		#region Properties

		private uint sizeCols
		{
			get
			{
				return (uint)Math.Ceiling(1d / this.sizes.x);
			}
		}

		private uint sizeRows
		{
			get
			{
				return (uint)Math.Ceiling(1d / this.sizes.y);
			}
		}

		#endregion

		#region Public Methods and Operators


		public void SetChromokey(float r, float g, float b)
		{
			//setup chromokey as purple
			this.Material.SetVector("_ChromoKey", new Vector4(r, g, b, 0));
		}

		public void SetChromokey(byte r, byte g, byte b)
		{
			SetChromokey(r / 255f, g / 255f, b / 255f);
		}



		/// <summary>
		///     adds an animation
		/// </summary>
		/// <param name="loops">if -1 - infinite</param>
		public void AddAnimation(string name, uint startFrame, uint endFrame, float fps, int loops)
		{
			if (!this.animations.ContainsKey(name))
			{
				this.animations.Add(name, new SpriteAnimationData(name, fps, startFrame, endFrame, loops));
			}
		}

		/// <summary>
		///     separates image into frames or tiles
		/// </summary>
		/// <param name="rows"></param>
		/// <param name="cols"></param>
		public void CreateAnimationFrames(uint rows, uint cols)
		{
			this.sizes = new Vector2(1f / cols, 1f / rows);
		}

		public void CreateQuadAtlas(uint quadSizeInPixels)
		{
			this.quadAtlasSize = quadSizeInPixels; //delay
		}

		public uint GetCurrentFrame()
		{
			return this.currentFrame;
		}

		public Rect GetCurrentTextCoord()
		{
			return this.GetTexCoordByFrameNum(this.currentFrame);
		}

		/// <summary>
		/// </summary>
		/// <returns></returns>
		public Matrix4x4 GetTransformMatrix()
		{
			var mat = new Matrix4x4();
			var quat = new Quaternion { eulerAngles = new Vector3(0, 0, this.Rotation) };

			mat.SetTRS(new Vector3(this.Position.x, this.Position.y, 1), quat, new Vector3(this.Scale.x, this.Scale.y, 1));

			return mat;
		}

		public void PlayAnimation(string name)
		{
			SpriteAnimationData data;
			if (this.animations.TryGetValue(name, out data))
			{
				this.currentAnimation = name;
				this.currentTimer = 0;
				this.currentFrame = data.Start;
				this.currentLoops = 0;
				this.bAnimPlaying = true;
			}
		}

		public void PlayAnimationIfNotTheSame(string s)
		{
			if(this.currentAnimation != s)
				this.PlayAnimation(s);
		}


		public void SetAnimFrameSize(float sizeX, float sizeY)
		{
			this.sizes = new Vector2(sizeX, sizeY);
		}

		public void SetFrame(uint frameNum)
		{
			this.SetTile(frameNum);
		}

		public void SetFrame(uint fX, uint fY)
		{
			this.SetTile(fX, fY);
		}

		/// <summary>
		///     set texture through api
		/// </summary>
		/// <param name="texturename"></param>
		public void SetTexture(string texturename)
		{
			this.TextureName = texturename;
		}

		/// <summary>
		///     sets current frame or tile
		/// </summary>
		/// <param name="tileNum"></param>
		public void SetTile(uint tileNum)
		{
			this.currentFrame = tileNum;
		}

		public void SetTile(uint tX, uint tY)
		{
			this.SetTile(tX + tY * this.sizeCols);
		}

		#endregion

		#region Methods

		/// <summary>
		/// </summary>
		/// <returns></returns>
		private SpriteAnimationData GetCurrentAnimation()
		{
			SpriteAnimationData data;
			if (this.animations.TryGetValue(this.currentAnimation, out data))
			{
				return data;
			}
			return null;
		}

		/// <summary>
		/// </summary>
		/// <param name="frameNum"></param>
		/// <returns></returns>
		private Rect GetTexCoordByFrameNum(uint frameNum)
		{
			uint sizex = this.sizeCols;

			uint offset_x = frameNum % sizex;
			uint offset_y = frameNum / sizex;

			Rect rect;

			if (!this.bReflect)
			{
				rect = new Rect(offset_x * this.sizes.x, (1f - offset_y * this.sizes.y), this.sizes.x - texStep, -this.sizes.y + texStep);
			}
			else
			{
				rect = new Rect((offset_x + 1) * this.sizes.x, (1f - offset_y * this.sizes.y), -this.sizes.x + texStep, -this.sizes.y + texStep);
			}

			return rect;
		}

		/// <summary>
		/// </summary>
		private void Start()
		{
			// get spritesManager
			this.SpritesManager = Camera.main.GetComponent<SpritesManager>();


			// init material
			Material tempmat = this.SpritesManager.TryGetRefMaterial(this.TextureName);
			if (tempmat == null)
			{
				//allocate new material
				this.Material = new Material(this.SpritesManager.InShader);
				var tex = Resources.Load(this.TextureName) as Texture;
				if (tex != null)
				{
					this.Material.mainTexture = tex;
				}
				else
				{
					Debug.Log("Error loading texture " + this.TextureName);
				}
			}
			else
			{
				this.Material = tempmat;
			}


			//setup texstep
			texStep = (float)(1d / this.Material.mainTexture.width); 

			// setup pixel-based quad
			if (this.Material != null && this.quadAtlasSize > 0)
			{
				Texture tex = this.Material.mainTexture;

				var heightInQuads = (uint)(tex.height / this.quadAtlasSize);
				var widthInQuads = (uint)(tex.width / this.quadAtlasSize);

				this.CreateAnimationFrames(heightInQuads, widthInQuads);
			}


			//register sprite (essential action)
			this.SpritesManager.RegisterSprite(this);

			// that's all, folks! no heavy meshes and texture allocations,
			// every sprite just use preallocated material
		}

		/// <summary>
		/// </summary>
		private void Update()
		{
			if (this.currentAnimation != null && this.bAnimPlaying)
			{
				//play animation
				this.currentTimer += Time.deltaTime;

				SpriteAnimationData data = this.GetCurrentAnimation();

				//next frame
				if (this.currentTimer > data.frameDeltaS)
				{
					this.currentFrame++;
					this.currentTimer = 0;

					if (this.currentFrame > data.End)
					{
						this.currentLoops++;

						if (data.Loops == -1) //infinite
						{
							this.currentFrame = data.Start; //reset animation
						}
						else
						{
							if (this.currentLoops > data.Loops)
							{
								this.bAnimPlaying = false;
								this.currentFrame--; //set last frame and stop
							}
							else
							{
								this.currentFrame = data.Start; //reset animation
							}
						}
					}
				}
			}
		}

		#endregion
	}
}