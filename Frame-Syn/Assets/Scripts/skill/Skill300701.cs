using UnityEngine;
using System.Collections;

public class Skill300701 : MonoBehaviour
{
	// 产生技能的玩家脚本
	private Player player;

	void Start ()
	{
		player = GetComponent<Player> ();
		// 技能动画
		player.animation.Play ("skill1");
		// 技能特效
		GameObject _skill = Resources.Load<GameObject> ("Prefabs/hero3007/Fx_hero3007_skill_01");
		GameObject skill = Instantiate (_skill, (Vector3)player.mover.position, transform.rotation);
		// 3 秒后销毁特效
		Destroy (skill, 3.0f);

		LogicFrame.Register (this);
	}

	void OnDestroy ()
	{
		LogicFrame.Unregister (this);
	}
	
	void Update ()
	{
	
	}

	void FrameUpdate()
	{
		
	}

}

