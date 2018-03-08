using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJson;

public class Projectile : MonoBehaviour
{
	// 产生飞行道具的玩家 id
	public int uid;
	// 目标玩家 id，若为 -1 表示打不中任何人
	public int aimId;
	// 飞行道具的距离界限
	public VInt distance;
	// 飞行道具的飞行速度
	public VInt speed;
	// 飞行道具的 y 方向上的偏移量
	public VInt offsetY;

	// 道具出现的初始位置
	private VInt3 initPosition;
	// 移动相关
	private LogicFrameMove mover;
	// 目标对象的脚本
	private Player aimPlayer;
	// 是否打中目标
	private bool isHit = false;

	void Start ()
	{
		initPosition = (VInt3)transform.position;
		mover = gameObject.AddComponent<LogicFrameMove> ();
		LogicFrame.Register (this);
	}

	void OnDestroy ()
	{
		LogicFrame.Unregister (this);
	}

	void FrameUpdate ()
	{
		// 这种情况是飞行道具打不中人，飞出一段距离就消失
		if (aimId == -1) {
			if (VInt3.Distance (initPosition, mover.position) >= distance) {
				mover.StopMove ();
				Destroy (gameObject);
			}
		} 
		// 这种情况是可以打中人，会一直跟随目标对象，直到打中为止
		else {
			if (aimPlayer == null) {
				aimPlayer = Global.entity [aimId].GetComponent<Player> ();
			}
			// 方向跟随目标
			transform.LookAt ((Vector3)aimPlayer.mover.position + new Vector3(0, (float)offsetY, 0));
			// 判断是否打中目标
			if (!isHit) {
				if (VInt3.Distance(mover.position, aimPlayer.mover.position) < (VInt)2.0f) {
					isHit = true;
					aimPlayer.Damage (aimId, 200);
					mover.StopMove ();
					Destroy (gameObject);
				}
			}
		}

		mover.StartMove ((VInt3)transform.forward, speed);
	}

}
