using Assets.TiledTest;

using UnityEngine;
using System.Collections;

public class DisplayGridlines : MonoBehaviour
{

	public Shader DisplayGridlightsShader;
	private Material mat;

	public Shader DisplayPhysxShader;
	private Material physxmat;


	// Use this for initialization
	void Start () 
	{
		mat = new Material(DisplayGridlightsShader);
		physxmat = new Material(DisplayPhysxShader);
	}


	/// <summary>
	/// 
	/// </summary>
	void RenderGridlines()
	{
		//begin render
		GL.PushMatrix();
		mat.SetPass(0);

		GL.Begin(GL.QUADS);

		float lposx = Camera.main.transform.position.x - Camera.main.aspect * Camera.main.orthographicSize;
		float rposx = Camera.main.transform.position.x + Camera.main.aspect * Camera.main.orthographicSize;
		float lposy = Camera.main.transform.position.y - Camera.main.orthographicSize;
		float rposy = Camera.main.transform.position.y + Camera.main.orthographicSize;

		// draw vertices
		GL.Vertex3(lposx, lposy, -10);
		GL.Vertex3(rposx, lposy, -10);
		GL.Vertex3(rposx, rposy, -10);
		GL.Vertex3(lposx, rposy, -10);

		GL.End();

		//end render
		GL.PopMatrix();
	}


	/// <summary>
	/// 
	/// </summary>
	void RenderPhysx()
	{
		TiledLevel lev = GameObject.Find("TiledLevel").GetComponent<TiledLevel>();
		if (lev != null && lev.Mode == 1) //physxmode
		{
			int i, j;
			for (i = 0; i < lev.Tiles.Count; i++)
			{
				for (j = 0; j < lev.Tiles[i].Count; j++)
				{
					TileInfo inf = lev.Tiles[i][j];

					float lposx = i * lev.GlobalScale;
					float rposx = (i + 1) * lev.GlobalScale;
					float lposy = j * lev.GlobalScale;
					float rposy = (j + 1) * lev.GlobalScale;

					if (inf.Type == TileType.None)  continue;
					else if (inf.Type == TileType.Brick)
					{
						this.physxmat.SetVector("_ChromoKey", new Vector4(1f, 0.2f, 0.2f, 1));
						GL.PushMatrix();
						physxmat.SetPass(0);
						GL.Begin(GL.QUADS);

						// draw vertices
						GL.Vertex3(lposx, lposy, 0.1f);
						GL.Vertex3(rposx, lposy, 0.1f);
						GL.Vertex3(rposx, rposy, 0.1f);
						GL.Vertex3(lposx, rposy, 0.1f);

						GL.End();
						GL.PopMatrix();
					}
					else if (inf.Type == TileType.Slope1)
					{
						this.physxmat.SetVector("_ChromoKey", new Vector4(1f, 0.2f, 0.2f, 1));
						GL.PushMatrix();
						physxmat.SetPass(0);
						GL.Begin(GL.TRIANGLES);

						// draw vertices
						GL.Vertex3(lposx, lposy, 0.1f);
						GL.Vertex3(rposx, rposy, 0.1f);
						GL.Vertex3(lposx, rposy, 0.1f);

						GL.End();
						GL.PopMatrix();
					}
					else if (inf.Type == TileType.Slope2)
					{
						this.physxmat.SetVector("_ChromoKey", new Vector4(1f, 0.2f, 0.2f, 1));
						GL.PushMatrix();
						physxmat.SetPass(0);
						GL.Begin(GL.TRIANGLES);

						// draw vertices
						GL.Vertex3(rposx, lposy, 0.1f);
						GL.Vertex3(rposx, rposy, 0.1f);
						GL.Vertex3(lposx, rposy, 0.1f);

						GL.End();
						GL.PopMatrix();
					}
					else if (inf.Type == TileType.Slope3)
					{
						this.physxmat.SetVector("_ChromoKey", new Vector4(1f, 0.2f, 0.2f, 1));
						GL.PushMatrix();
						physxmat.SetPass(0);
						GL.Begin(GL.TRIANGLES);

						// draw vertices
						GL.Vertex3(lposx, lposy, 0.1f);
						GL.Vertex3(rposx, lposy, 0.1f);
						GL.Vertex3(rposx, rposy, 0.1f);

						GL.End();
						GL.PopMatrix();
					}
					else if (inf.Type == TileType.Slope4)
					{
						this.physxmat.SetVector("_ChromoKey", new Vector4(1f, 0.2f, 0.2f, 1));
						GL.PushMatrix();
						physxmat.SetPass(0);
						GL.Begin(GL.TRIANGLES);

						// draw vertices
						GL.Vertex3(lposx, lposy, 0.1f);
						GL.Vertex3(rposx, lposy, 0.1f);
						GL.Vertex3(lposx, rposy, 0.1f);

						GL.End();
						GL.PopMatrix();
					}
					else if (inf.Type == TileType.Ladder)
					{
						this.physxmat.SetVector("_ChromoKey", new Vector4(0.2f, 1f, 1f, 1));
						GL.PushMatrix();
						physxmat.SetPass(0);
						GL.Begin(GL.QUADS);

						// draw vertices
						GL.Vertex3(lposx, lposy, 0.1f);
						GL.Vertex3(rposx, lposy, 0.1f);
						GL.Vertex3(rposx, rposy, 0.1f);
						GL.Vertex3(lposx, rposy, 0.1f);

						GL.End();
						GL.PopMatrix();
					}
					else if (inf.Type == TileType.Water)
					{
						physxmat.SetVector("_ChromoKey", new Vector4(0.2f, 0.2f, 1f, 1));
						GL.PushMatrix();
						physxmat.SetPass(0);	
						GL.Begin(GL.QUADS);

						// draw vertices
						GL.Vertex3(lposx, lposy, 0.1f);
						GL.Vertex3(rposx, lposy, 0.1f);
						GL.Vertex3(rposx, rposy, 0.1f);
						GL.Vertex3(lposx, rposy, 0.1f);

						GL.End();
						GL.PopMatrix();
					}

				}
			}

		}
	}


	// postrender
	void OnPostRender ()
	{
		this.RenderGridlines();
		this.RenderPhysx();
	}
}
