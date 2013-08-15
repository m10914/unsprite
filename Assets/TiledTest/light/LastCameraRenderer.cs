using UnityEngine;
using System.Collections;

public class LastCameraRenderer : MonoBehaviour
{
	private Material lastMaterial;

	public RenderTexture tex;

	// Use this for initialization
	void Start () {		
		lastMaterial = new Material(Shader.Find("Unlit/Texture"));
	}

	void OnPostRender()
	{
		RenderTexture tex = GameObject.Find("PostEffectCam").GetComponent<PostEffectLighting>().destTexture;

		lastMaterial.SetTexture("_MainTex", tex);
		lastMaterial.SetPass(0); //grab pass
		GL.Begin(GL.QUADS);

		Camera postcam = GameObject.Find("LastCamera").GetComponent<Camera>();

		float lposx = postcam.transform.position.x - postcam.aspect * postcam.orthographicSize;
		float rposx = postcam.transform.position.x + postcam.aspect * postcam.orthographicSize;
		float lposy = postcam.transform.position.y - postcam.orthographicSize;
		float rposy = postcam.transform.position.y + postcam.orthographicSize;

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
