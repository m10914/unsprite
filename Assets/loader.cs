using Assets.Unsprite;

using UnityEngine;
using System.Collections;

public class loader : MonoBehaviour {


	private string oldAnimation = "";

	// Use this for initialization
	void Start () 
	{
	
		// that's a good example, how it can be done through api
		for (var i = 0; i < 1; i++)
		{
			GameObject obj = new GameObject();
			obj.name = "textsprite";

			Sprite component = obj.AddComponent<Sprite>();
			component.SetTexture("AxeBattler");
			component.Scale = new Vector2(4.93f, 7.54f);
			component.Position = new Vector2(Random.value*20-10, Random.value*20-10);

			component.CreateAnimationFrames(1, 9);
			component.AddAnimation("Stand", 0, 0, 3, -1);
			component.AddAnimation("Walk", 1, 4, 3, -1);

			component.PlayAnimation("Stand");		
		}
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		GameObject obj = GameObject.Find("textsprite");
		Sprite spr = obj.GetComponent<Sprite>();

		string currentAnim = "Stand";
		if (Input.GetKey(KeyCode.D))
		{
			currentAnim = "Walk";
			spr.bReflect = false;
			obj.transform.Translate(6f*Time.deltaTime, 0f, 0f);
		}
		else if (Input.GetKey(KeyCode.A))
		{
			currentAnim = "Walk";
			spr.bReflect = true;
			obj.transform.Translate(-6f*Time.deltaTime, 0f, 0f);
		}


		if (currentAnim != oldAnimation)
		{
			spr.PlayAnimation(currentAnim);
			oldAnimation = currentAnim;
		}
	}
}
