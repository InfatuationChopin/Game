using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJson;
using UnityEngine.UI;
using System;

public class PlayerEasyTouch : MonoBehaviour {

	private VInt lastUploadTime = VInt.zero;
	private bool isAllowUpload = false;
	// 玩家自己的 Player 脚本对象
	private Player player;

	void Start ()
	{
		player = Global.entity [Global.uid].GetComponent<Player> ();
	}

	void Update()
	{
		// 控制发送移动指令的时间间隔
		VInt time = (VInt)Time.time;
		if (time - lastUploadTime < LogicFrame.frameIntervalTime / (VInt)1.5f) {
			return;
		}
		lastUploadTime = time;
		isAllowUpload = true;
	}

	void OnEnable ()
	{    
		EasyJoystick.On_JoystickMove += OnEasyTouchMove;  
		EasyJoystick.On_JoystickMoveEnd += OnEasyTouchEnd;  
		EasyButton.On_ButtonUp += OnEasyButtonUp;  
	}

	void OnEasyTouchMove (MovingJoystick move)
	{    
		if (move.joystickName != "EventTouch") {       
			return;  
		}  

		VInt time = (VInt)Time.time;
		if (time - lastUploadTime < LogicFrame.frameIntervalTime / (VInt)1.5f) {
			return;
		}
		lastUploadTime = time;

		// 玩家死亡状态不能移动
		if (player.isDeath) {
			return;
		}

		// 获取按键的移动向量
		VInt3 touchVector = (VInt3)new Vector3 (move.joystickAxis.x, 0, move.joystickAxis.y).normalized;

		// 发送移动指令
		JsonObject msg = new JsonObject ();
		JsonObject moveMsg = new JsonObject ();
		moveMsg ["type"] = Global.Frame_Move;
		moveMsg ["vx"] = touchVector.x;
		moveMsg ["vy"] = touchVector.y;
		moveMsg ["vz"] = touchVector.z;
		msg ["data"] = moveMsg;
		PomeloCli.Notify ("fight.fightHandler.frame", msg);
	}

	void OnEasyTouchEnd (MovingJoystick move)
	{
		if (move.joystickName != "EventTouch") {       
			return;  
		}  

		// 玩家死亡状态不能结束移动
		if (player.isDeath) {
			return;
		}

		// 发送结束移动指令
		JsonObject msg = new JsonObject ();
		JsonObject stopMoveMsg = new JsonObject ();
		stopMoveMsg ["type"] = Global.Frame_StopMove;
		msg ["data"] = stopMoveMsg;
		PomeloCli.Notify ("fight.fightHandler.frame", msg);
	}

	void OnEasyButtonUp (string buttonName)
	{
		// 死亡状态不能操作
		if (player.isDeath) {
			return;
		}

		if (buttonName == "Attack") {
			// 发送攻击的指令
			if (player.isAttack) {
				return;
			}
			JsonObject msg = new JsonObject ();
			JsonObject atkMsg = new JsonObject ();
			atkMsg ["type"] = Global.Frame_Attack;
			msg ["data"] = atkMsg;
			PomeloCli.Notify ("fight.fightHandler.frame", msg);
		} else if (buttonName == "Skill1" || buttonName == "Skill2" || buttonName == "Skill3") {
			// 发送技能的指令
			JsonObject msg = new JsonObject ();
			JsonObject skillMsg = new JsonObject ();
			skillMsg ["type"] = Global.Frame_Skill;
			switch (buttonName) {
			case "Skill1":
				skillMsg ["code"] = 1;
				break;
			case "Skill2":
				skillMsg ["code"] = 2;
				break;
			case "Skill3":
				skillMsg ["code"] = 3;
				break;
			}
			msg ["data"] = skillMsg;
			PomeloCli.Notify ("fight.fightHandler.frame", msg);
		}
	}
}
