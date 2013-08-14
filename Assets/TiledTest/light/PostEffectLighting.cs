using UnityEngine;
using System.Collections;

public class PostEffectLighting : MonoBehaviour
{

	public RenderTexture LightTexture;

	public RenderTexture BaseTexture;

	public Shader PostEffectShader;

	private Material mat;


	// Use this for initialization
	void Start () {
	
		mat = new Material(PostEffectShader);
		mat.SetTexture("_MainTex", BaseTexture);
		mat.SetTexture("_LightTex", LightTexture);

	}



	void OnPostRender()
	{
		GL.PushMatrix();

		mat.SetPass(0);



		Camera postcam = GameObject.Find("PostEffectCam").GetComponent<Camera>();

		float lposx = postcam.transform.position.x - postcam.orthographicSize;
		float rposx = postcam.transform.position.x + postcam.orthographicSize;
		float lposy = postcam.transform.position.y - postcam.orthographicSize;
		float rposy = postcam.transform.position.y + postcam.orthographicSize;

		GL.Begin(GL.QUADS);

		// draw vertices
		GL.TexCoord2(1,0);
		GL.Vertex3(lposx, lposy, -10);
		GL.TexCoord2(0,0);
		GL.Vertex3(rposx, lposy, -10);
		GL.TexCoord2(0,1);
		GL.Vertex3(rposx, rposy, -10);
		GL.TexCoord2(1,1);
		GL.Vertex3(lposx, rposy, -10);

		GL.End();

		GL.PopMatrix();
	}
}
