using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hero {

	public const int Type_Short_Range = 1;
	public const int Type_Long_Range = 2;

	public int code; // 英雄编码
	public VInt speed; // 移动速度
	public VInt atkSpeed; // 攻击速度
	public VInt atkRange; // 攻击范围
	public int hpMax; // 最大血量
	public VInt atkTime; // 攻击伤害时间点
	public AtkType atkType; // 攻击属于远程还是近程（决定是否产生飞行道具）

	public Hero(int code, VInt speed, VInt atkSpeed, VInt atkRange, int hpMax, VInt atkTime, AtkType atkType) {
		this.code = code;
		this.speed = speed;
		this.atkSpeed = atkSpeed;
		this.atkRange = atkRange;
		this.hpMax = hpMax;
		this.atkTime = atkTime;
		this.atkType = atkType;
	}

	public enum AtkType {
		Short_Range,
		Long_Range
	}

}
