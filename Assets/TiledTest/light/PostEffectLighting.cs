using Assets.TiledTest.light;

using UnityEngine;
using System.Collections;

public class PostEffectLighting : MonoBehaviour
{

	
	public Shader PostEffectShader;
	public RenderTexture destTexture;

	private Material mat;
	


	// Use this for initialization
	void Start ()
	{
		mat = new Material(PostEffectShader);

		destTexture = new RenderTexture(Screen.width, Screen.height, 16);
		Camera postcam = GameObject.Find("PostEffectCam").GetComponent<Camera>();
		postcam.targetTexture = destTexture;
	}


	/// <summary>
	/// 
	/// </summary>
	void OnPostRender()
	{
		Camera postcam = GameObject.Find("PostEffectCam").GetComponent<Camera>();

		float lposx = postcam.transform.position.x - postcam.aspect * postcam.orthographicSize;
		float rposx = postcam.transform.position.x + postcam.aspect * postcam.orthographicSize;
		float lposy = postcam.transform.position.y - postcam.orthographicSize;
		float rposy = postcam.transform.position.y + postcam.orthographicSize;

		TiledLevel lev = GameObject.Find("TiledLevel").GetComponent<TiledLevel>();
		
		foreach (var light in lev.Lights)
		{
			//draw cones
			if (typeof(LightCone) == light.GetType())
			{
				mat.SetTexture("_LightTex", (light as LightCone).renderCamera.targetTexture);
				mat.SetPass(0); //grab pass
				mat.SetPass(1); //light multiply pass
				GL.Begin(GL.QUADS);

				// draw vertices
				GL.TexCoord2(1, 0);
				GL.Vertex3(lposx, lposy, -10);
				GL.TexCoord2(0, 0);
				GL.Vertex3(rposx, lposy, -10);
				GL.TexCoord2(0, 1);
				GL.Vertex3(rposx, rposy, -10);
				GL.TexCoord2(1, 1);
				GL.Vertex3(lposx, rposy, -10);

				GL.End();
			}
		}
		

		//final mix with original image
		mat.SetTexture("_MainTex", Camera.main.targetTexture);
		mat.SetPass(0); //grab pass
		mat.SetPass(2); //final mixing pass
		GL.Begin(GL.QUADS);

		// draw vertices
		GL.TexCoord2(1, 0);
		GL.Vertex3(lposx, lposy, -10);
		GL.TexCoord2(0, 0);
		GL.Vertex3(rposx, lposy, -10);
		GL.TexCoord2(0, 1);
		GL.Vertex3(rposx, rposy, -10);
		GL.TexCoord2(1, 1);
		GL.Vertex3(lposx, rposy, -10);

		GL.End();

	}
}
