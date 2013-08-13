namespace Assets.TiledTest
{
	using Assets.Unsprite;

	using UnityEngine;

	public class PhysxSprite : Sprite
	{
		public Vector2 Speed;

		public Vector2 Size;

		public bool Grounded;

		public bool bOnLadder;

		public bool bInWater;


		public bool IsJumping;
		public float JumpingTime;

		public void UpdateCoords()
		{
			//this.transform.Translate(new Vector3(Speed.x * Time.deltaTime * Glo));
		}
	}
}