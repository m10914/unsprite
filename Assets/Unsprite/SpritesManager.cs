using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using UnityEngine;
using System.Collections;
using Sprite = Assets.Unsprite.Sprite;

public class SpritesManager : MonoBehaviour
{

	public Shader InShader;

	private List<Sprite> _sprites;

	private Dictionary<string, Material> _materials;

	public List<Sprite> Sprites
	{
		get
		{
			if(_sprites == null)
				_sprites = new List<Sprite>();
			return _sprites;
		}
	}

	public Dictionary<string, Material> Materials
	{
		get
		{
			if (_materials == null) _materials = new Dictionary<string, Material>();
			return _materials;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="matName"></param>
	public Material TryGetRefMaterial(string matName)
	{
		Material mat;
		if (this.Materials.TryGetValue(matName, out mat))
		{
			return mat;
		}
		else return null;
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="spr"></param>
	public void RegisterSprite(Sprite spr)
	{
		//add to materials collection
		if (!Materials.ContainsKey(spr.TextureName))
		{
			Materials.Add(spr.TextureName, spr.Material);
		}

		//add to sprites collection
		Sprites.Add(spr);
	}


	// Use this for initialization
	void Start () {	
	}
	
	// Update is called once per frame
	void Update () {
	}


	/// <summary>
	/// resort sprites by layer property
	/// </summary>
	void Resort()
	{
		List<Sprite> sorted = Sprites.OrderBy(el => el.Layer).ToList();
		_sprites = sorted;
	}



	/// <summary>
	/// 
	/// </summary>
	void OnPostRender()
	{
		this.Resort();

		//begin render
		GL.PushMatrix();

		foreach (var spr in this.Sprites)
		{
			Material tempmat = spr.Material;
			tempmat.SetPass(0);

			// setup matrix
			
			GL.MultMatrix(spr.GetTransformMatrix());
			Rect texcoords = spr.GetCurrentTextCoord();

			//Debug.Log(spr.GetCurrentFrame() +" "+ texcoords.yMin+"-"+texcoords.yMax+";"+texcoords.xMin+"-"+texcoords.yMax);

			GL.Begin(GL.QUADS);

			// draw vertices
			GL.TexCoord2(texcoords.xMin, texcoords.yMin);
			GL.Vertex3(0, 0, 0);

			GL.TexCoord2(texcoords.xMax, texcoords.yMin);
			GL.Vertex3(1, 0, 0);
			
			GL.TexCoord2(texcoords.xMax, texcoords.yMax);
			GL.Vertex3(1, 1, 0);

			GL.TexCoord2(texcoords.xMin, texcoords.yMax);
			GL.Vertex3(0, 1, 0);

			GL.End();
			
			
		}

		//end render
		GL.PopMatrix();
	}





}

