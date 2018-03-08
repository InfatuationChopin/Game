using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using SimpleJson;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
	// 人物的状态，用于切换动画
	private const int State_Normal = 0;
	private const int State_Run = 1;
	private const int State_Attack = 2;
	private const int State_Skill = 3;
	private const int State_Die = 4;

	// 用户 ID（登录账户 ID）
	public int uid;
	// 人物的移动速度
	public VInt speedNormal;
	// 当前帧的人物移动的真实速度
	public VInt speedReal;
	// 人物的攻击速度
	public VInt atkSpeedNormal;
	// 当前帧人物的真实的攻击速度
	public VInt atkSpeedReal;
	// 人物的攻击范围
	public VInt atkRange;
	// 人物的攻击出手时间点
	public VInt atkTime;
	// 人物的攻击属于近程还是远程
	public Hero.AtkType atkType;
	// 人物的动画系统
	public Animation animation;
	// 英雄编号
	public int heroCode;
	// 人物的血条脚本对象
	public Blood blood;
	// 是否正在攻击中（是否正在攻击动画中，打中别人或者放出飞行道具后即为攻击结束）
	public bool isAttack;
	// 是否是死亡状态
	public bool isDeath;

	// 移动相关
	public LogicFrameMove mover;
	// 攻击相关
	private int combo = 0;
	private static VInt comboIntervalTime = (VInt)2.0f;
	private VInt lastComboTime = VInt.zero;
	// 重生倒计时相关
	public int rebornTime = 0;

	// 日志相关
	private static Text log;
	private static string temp1;
	private static string temp2;

	void Start ()
	{
		mover = gameObject.AddComponent<LogicFrameMove> ();
		mover.SetAnimation (animation, "idle", "run");

		log = GameObject.Find ("LogText").GetComponent<Text> ();
		LogicFrame.Register (this);
	}

	void OnDestroy ()
	{
		LogicFrame.Unregister (this);
	}

	void Update ()
	{
		// 控制连击次数
		VInt time = (VInt)Time.time;
		if (time - lastComboTime < comboIntervalTime) {
			return;
		}
		lastComboTime = time;
		combo = 0;
	}

	void FrameUpdate ()
	{
		// 取当前帧的指令，并进行相应的计算
		JsonObject frameOrder = LogicFrame.getFrameOrder (uid);
		if (frameOrder != null) {
			int type = Convert.ToInt32 (frameOrder ["type"]);
			if (type == Global.Frame_Move) {  // 移动
				int vx = Convert.ToInt32 (frameOrder ["vx"]);
				int vy = Convert.ToInt32 (frameOrder ["vy"]);
				int vz = Convert.ToInt32 (frameOrder ["vz"]);
				mover.StartMove (new VInt3(vx, vy, vz), speedReal);
			} else if (type == Global.Frame_StopMove) { // 停止移动
				mover.StopMove ();
			} else if (type == Global.Frame_Attack) { // 攻击
				OnAttack ();
			} else if (type == Global.Frame_Skill) { // 技能
				int skillCode = Convert.ToInt32 (frameOrder ["code"]);
				OnSkill (skillCode);
			}
		}
			
		foreach(int uid in Global.entity.Keys) {
			Player player = Global.entity [uid].GetComponent<Player> ();
			player.blood.textInfo.text = player.mover.position.ToStringF3();
		}
		string text = "";
		text += LogicFrame.frameOrderList.Count + ", " + LogicFrame.frame + ", " + LogicFrame.frameIntervalTimeReal;
		log.text = text;
	}

	void OnAttack ()
	{
		isAttack = true;
		// 判断要攻击的是哪一个队伍
		ArrayList aimList = new ArrayList ();
		foreach (int uid in Global.myTeamUid) {
			if (this.uid == uid) {
				aimList = Global.otherTeamUid;
				break;
			}
		}
		foreach (int uid in Global.otherTeamUid) {
			if (this.uid == uid) {
				aimList = Global.myTeamUid;
				break;
			}
		}
		// 被攻击者 id，如果为 -1 则表示超出攻击范围
		int aimId = -1;
		// 如果当前距离和敌方所有玩家大于攻击距离则在原地攻击。否则攻击对方距离最近的非状态的玩家
		int count = 0;
		VInt minDistance = VInt.zero;;
		foreach (int otherUid in aimList) {
			Player player = Global.entity [otherUid].GetComponent<Player> ();
			VInt distance = VInt3.Distance (mover.position, player.mover.position);
			if (distance > atkRange) {
				count++;
			} else {
				if (!player.isDeath) {
					if (distance < minDistance || minDistance == VInt.zero) {
						minDistance = distance;
						aimId = otherUid;
					}
				} else {
					count++;
				}
			}
		}
		if (count == Global.otherTeamUid.Count) {
			aimId = -1;
		}

		// 转向被攻击者
		if (aimId != -1) {
			transform.LookAt ((Vector3)Global.entity [aimId].GetComponent<Player> ().mover.position);
		}
		// 攻击动画，控制攻击速度
		String animName = "attack" + (combo + 1);
		VInt animationSpeed = (VInt)animation [animName].length / ((VInt)1.0f / atkSpeedReal);
		animation [animName].speed = (float)animationSpeed;
		animation.Play (animName);
		// 攻击特效
		String prefabPath = "Prefabs/hero" + heroCode + "/Fx_hero" + heroCode + "_attack_0" + (combo + 1);
		GameObject _attack = Resources.Load<GameObject> (prefabPath);
		GameObject attack = Instantiate (_attack, (Vector3)mover.position, transform.rotation);
		// 3 秒后销毁特效
		Destroy (attack, 3.0f);

		// 攻击动画进行一段时间后会进行出击
		float delay = (float)(atkTime / animationSpeed);
		LogicFrame.InvokeDelay (this, delay, "AttackHit", new object[]{aimId});

		// 控制连击次数
		if (combo < 3) {
			combo++;
		} else {
			combo = 0;
		}
		lastComboTime = (VInt)Time.time;
	}

	void AttackHit (int aimId)
	{
		isAttack = false;
		// 近程英雄打中别人了，远程英雄产生攻击的飞行道具
		switch (atkType) {
		case Hero.AtkType.Short_Range:
			if (aimId != -1) {
				Damage (aimId, 200);
			}
			break;
		case Hero.AtkType.Long_Range:
			string prefabPath = "Prefabs/hero" + heroCode + "/Fx_hero" + heroCode + "_attack_prop";
			GameObject _attackProp = Resources.Load<GameObject> (prefabPath);
			VInt offsetY = (VInt)1.2f;
			GameObject attackProp = Instantiate (_attackProp, (Vector3)mover.position + new Vector3 (0, (float)offsetY, 0), transform.rotation);
			Projectile projectile = attackProp.AddComponent<Projectile> ();
			projectile.uid = uid;
			projectile.aimId = aimId;
			projectile.speed = (VInt)20.0f;
			projectile.offsetY = offsetY;
			projectile.distance = atkRange;
			break;
		}
	}

	void OnSkill (int skillCode)
	{
		switch (skillCode) {
		// 闪现
		case 1:
			VInt3 targetPosition = mover.position + mover.dir * (VInt)5.0f;
			mover.position = targetPosition;
			transform.position = (Vector3)targetPosition;
			break;
		// 移动速度增加
		case 2:
			speedReal = speedNormal * (VInt)2.0f;
			LogicFrame.InvokeDelay (this, 5.0f, "SpeedNormal", null);
			break;
		// 攻击速度增加
		case 3:
			atkSpeedReal = atkSpeedNormal * (VInt)2.0f;
			LogicFrame.InvokeDelay (this, 5.0f, "AttackSpeedNormal", null);
			break;
		}
	}

	void SpeedNormal()
	{
		speedReal = speedNormal;
	}

	void AttackSpeedNormal()
	{
		atkSpeedReal = atkSpeedNormal;
	}

	public void Damage (int aimId, int damageHp)
	{
		// 被伤害的人
		Player aimPlayer = Global.entity [aimId].GetComponent<Player> ();
		// 扣血 
		aimPlayer.blood.currValue -= damageHp;
		// 死亡
		if (aimPlayer.blood.currValue <= 0) {
			aimPlayer.blood.currValue = 0;
			Death (aimId, 10, 0);
		}
	}

	public void Death(int aimId, int time, int frame)
	{
		Player aimPlayer = Global.entity [aimId].GetComponent<Player> ();
		aimPlayer.isDeath = true;
		aimPlayer.mover.StopMove ();
		aimPlayer.animation.Play ("death");
		aimPlayer.combo = 0;
		// 先停顿指定的帧数，再开始重生倒计时
		LogicFrame.InvokeDelayByFrame(aimPlayer, frame, "RebornCountDown", new object[]{time, aimId});
	}

	void RebornCountDown (int time, int aimId)
	{
		Reborn (time, aimId);
		if (time > 0) {
			rebornTime = time - 1;
			LogicFrame.InvokeDelay (this, 1.0f, "RebornCountDown", new object[]{rebornTime, aimId});
		}
	}

	void Reborn (int time, int aimId)
	{
		Player aimPlayer = Global.entity [aimId].GetComponent<Player> ();
		aimPlayer.blood.textReborn.text = time + "";
		// 重生完成
		if (time == 0) {
			aimPlayer.isDeath = false;
			aimPlayer.animation.Play ("idle");
			aimPlayer.blood.textReborn.text = "";
			aimPlayer.blood.currValue = blood.maxValue;
		}
	}

	//	void OnSkill (JsonObject msg)
	//	{
	//		// 攻击者 id
	//		int id = Convert.ToInt32 (msg ["id"]);
	//		if (id != uid) {
	//			return;
	//		}
	//
	//		int skillId = Convert.ToInt32 (msg ["skillId"]);
	//		int code = Convert.ToInt32 (msg ["code"]);
	//		int skillCode = code - heroCode * 10;
	//		string type = msg ["type"].ToString ();
	//		// 创建技能产生的飞行道具
	//		GameObject _skill = null;
	//		GameObject skill = null;
	//		if (type == "create") {
	//			JsonObject args = (JsonObject)msg ["args"];
	//			if (heroCode == 3008) {
	//				switch (skillCode) {
	//				case 1:
	//					_skill = Resources.Load<GameObject> ("Prefabs/hero3008/Fx_hero3008_skill_01_gong_01");
	//					skill = Instantiate (_skill, transform.position + new Vector3 (0, 1.2f, 0), transform.rotation);
	//					Skill skillComponent = skill.AddComponent<Skill> ();
	//					skillComponent.distance = 9;
	//					skillComponent.vector = transform.forward.normalized;
	//					Global.entity.Add (skillId, skill);
	//					break;
	//				case 2:
	//					_skill = Resources.Load<GameObject> ("Prefabs/hero3008/Fx_hero3008_skill_02_dimian");
	//					skill = Instantiate (_skill, transform.position + transform.forward.normalized * 5, transform.rotation);
	//					// 3 秒后销毁对象
	//					Destroy (skill, 3.0f);
	//					break;
	//				}
	//			}
	//
	//		} else if (type == "damage") {
	//			// 技能打中
	//			JsonObject args = (JsonObject)msg ["args"];
	//			JsonArray arr = (JsonArray)args ["arr"];
	//			foreach (JsonObject jo in arr) {
	//				int aimId = Convert.ToInt32 (jo ["aimId"]);
	//				int aimHp = Convert.ToInt32 (jo ["aimHp"]);
	//				// 被打中的人
	//				GameObject aim = Global.entity [aimId];
	//				// 扣血
	//				aim.GetComponent<Player> ().blood.currValue = aimHp;
	//				// 死亡
	//				if (aimHp == 0) {
	//					Player player = aim.GetComponent<Player> ();
	//					player.isDeath = true;
	//					player.animation.Play ("death");
	//					player.moveVector = Vector3.zero;
	//					player.moveTarget = aim.transform.position;
	//					player.redressVector = Vector3.zero;
	//				}
	//				// 打中别人销毁飞行道具
	//				if (Global.entity.ContainsKey (skillId)) {
	//					Destroy (Global.entity [skillId]);
	//					Global.entity.Remove (skillId);
	//				}
	//			}
	//		}
	//	}
}
