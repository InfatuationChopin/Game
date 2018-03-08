using UnityEngine;
using System.Collections;
using SimpleJson;
using System.Collections.Generic;
using System;

public class Global : MonoBehaviour
{
	// 战斗中帧同步的消息类型
	public const int Frame_None = -1;
	public const int Frame_Move = 0;
	public const int Frame_StopMove = 1;
	public const int Frame_Attack = 2;
	public const int Frame_Skill = 3;

	// 玩家 id 相关
	public static int uid = 0;
	public static int team = 0;
	public static ArrayList myTeamUid = new ArrayList ();
	public static ArrayList otherTeamUid = new ArrayList ();

	// 队伍在房间时的数据
	public static JsonObject teamData;
	// 匹配成功后的双方数据
	public static JsonObject selectReadyData;
	// 选择英雄时自己队伍的数据
	public static JsonObject selectStartData;
	// 战斗准备时的数据
	public static JsonObject fightReadyData;
	// 所有游戏对象实体的字典
	public static Dictionary<int, GameObject> entity = new Dictionary<int, GameObject> ();
	// 英雄的属性字典
	public static Dictionary<int, Hero> hero = new Dictionary<int, Hero> ();

	void Start ()
	{
		hero.Add (3007, 
			new Hero (
				3007, // code
				(VInt)4.5f, // speed
				(VInt)1.5f, // atkSpeed
				(VInt)3.5f, // atkRange
				2000, // hpMax
				(VInt)0.5f, // atkTime
				Hero.AtkType.Short_Range // atkType
			));
		hero.Add (3008, 
			new Hero (
				3008, 
				(VInt)4.5f, 
				(VInt)1.0f, 
				(VInt)7.0f, 
				2000, 
				(VInt)0.5f, 
				Hero.AtkType.Long_Range));
	}

	public static long GetTimeStamp ()
	{
		TimeSpan ts = DateTime.UtcNow - new DateTime (1970, 1, 1, 0, 0, 0, 0);  
		return Convert.ToInt64 (ts.TotalMilliseconds);  
	}

}
