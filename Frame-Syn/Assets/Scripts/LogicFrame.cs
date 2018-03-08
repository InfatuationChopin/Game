using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using SimpleJson;
using System;
using System.Reflection;

public class LogicFrame : MonoBehaviour
{
	// 每一帧的时间间隔，也是服务器更新帧同步的时间间隔（在游戏过程中本地会有变化）
	public static VInt frameIntervalTime = (VInt)0.05f;
	// 客户端真实的每一帧的时间间隔（每一帧都可能变化）
	public static VInt frameIntervalTimeReal = frameIntervalTime;
	// 服务器返回的当前帧编号
	public static long frame = -1;
	// 帧指令集合
	public static ArrayList frameOrderList = new ArrayList ();

	// 控制逻辑帧
	private VInt lastFrameTime = (VInt)0.0f;
	private VInt nextFrameWaitTime = frameIntervalTime;
	private VInt lastLogicFrameTime = (VInt)0.0f;

	// 需要分发逻辑帧的脚本
	private static ArrayList methodList = new ArrayList ();
	private static ArrayList methodListTemp = new ArrayList ();
	private static ArrayList methodListDelete = new ArrayList ();
	// 保存当前帧的玩家指令
	private static Dictionary<int, JsonObject> playerOrder = new Dictionary<int, JsonObject> ();
	// 延迟调用的函数
	private static ArrayList methodDelayList = new ArrayList ();
	private static ArrayList methodDelayListTemp = new ArrayList ();

	void Update ()
	{
		VInt now = (VInt)Time.time;
		int frameCount = frameOrderList.Count;
		if (now - lastFrameTime >= nextFrameWaitTime && frameCount > 0) {
			lastFrameTime = now;
			// 循环体
			Loop ();
			// 调整循环速度，对抗网络延迟
			if (frameCount == 1) {
				nextFrameWaitTime = (VInt)0.05f;
			} else if (frameCount == 2) {
				nextFrameWaitTime = (VInt)0.038f;
			} else if (frameCount <= 4) {
				nextFrameWaitTime = (VInt)0.016f;
			} else if (frameCount <= 16) {
				nextFrameWaitTime = (VInt)0.0f;
			} else {
				for (int i = 0; i < 10; i++) {
					Loop ();
				}
				nextFrameWaitTime = (VInt)0.0f;
			}
			nextFrameWaitTime = IntMath.Max ((VInt)0, nextFrameWaitTime - (VInt)Time.deltaTime / (VInt)2.0f);
		}
	}

	void Loop ()
	{
		// 总是取第一条帧指令
		JsonObject frameOrder = (JsonObject)frameOrderList [0];
		// 记录当前执行的帧编号
		frame = Convert.ToInt64 (frameOrder ["frame"]);
		// 保存每个玩家的当前帧指令
		JsonArray datas = (JsonArray)frameOrder ["datas"];
		playerOrder = new Dictionary<int, JsonObject> ();
		foreach (JsonObject data in datas) {
			int uid = Convert.ToInt32 (data ["uid"]);
			JsonObject userData = (JsonObject)data ["data"];
			playerOrder.Add (uid, userData);
		}
		// 分发逻辑帧
		Dispatch ();
		// 延迟调用函数
		Invoke ();
		// 计算实际的帧间隔时间
		CountFrameIntervalTimeReal ();
		// 每 1000 帧上传一次客户端的状态（本帧结束时的状态）
		Upload ();
		// 删除取出的指令
		frameOrderList.RemoveAt (0);
	}

	void Dispatch ()
	{
		// 先删除注销过的脚本
		foreach (Method method in methodListDelete) {
			if (methodList.Contains(method)) {
				methodList.Remove (method);
			}
		}
		methodListDelete.Clear ();
		// 遍历集合，反射调用
		methodList.AddRange(methodListTemp);
		methodListTemp.RemoveRange (0, methodListTemp.Count);
		foreach (Method method in methodList) {
			if (method.methodInfo == null) {
				continue; 
			}
			method.methodInfo.Invoke (method.mono, null);
		}
	}

	void Upload ()
	{
		if ((frame % 1000) == 999) {
			JsonObject msg = new JsonObject ();
			JsonObject data = new JsonObject (); 
			JsonArray userDatas = new JsonArray ();
			foreach (int uid in Global.entity.Keys) {
				JsonObject userData = new JsonObject ();
				Player player = Global.entity [uid].GetComponent<Player> ();
				userData ["uid"] = player.uid;
				userData ["x"] = player.mover.position.x;
				userData ["y"] = player.mover.position.y;
				userData ["z"] = player.mover.position.z;
				userData ["hp"] = player.blood.currValue;
				// 若玩家死亡且正在重生倒计时则上传相关数据
				if (player.isDeath) {
					foreach (MethodDelay methodDelay in methodDelayList) {
						if (methodDelay.method.methodInfo.Name == "RebornCountDown") {
							userData ["rebornTime"] = player.rebornTime;
							userData ["rebornFrame"] = methodDelay.frame - methodDelay.currFrame;
						}
					}
				}
				userDatas.Add (userData);
			}
			data ["userDatas"] = userDatas;
			msg ["frame"] = frame;
			msg ["data"] = data;
			PomeloCli.Notify ("fight.fightHandler.upload", msg);
		}
	}

	void CountFrameIntervalTimeReal ()
	{
		VInt time = (VInt)Time.time;
		if (lastLogicFrameTime != VInt.zero) {
			frameIntervalTimeReal = time - lastLogicFrameTime;
		}
		lastLogicFrameTime = time;
	}

	void Invoke ()
	{
		ArrayList methodDelayListDelte = new ArrayList ();
		methodDelayList.AddRange (methodDelayListTemp);
		methodDelayListTemp.RemoveRange (0, methodDelayListTemp.Count);
		// 遍历集合中的延时函数，达到条件后反射调用，加入删除集合
		foreach (MethodDelay methodDelay in methodDelayList) {
			if (methodDelay.method.methodInfo == null) {
				methodDelayListDelte.Add (methodDelay);
				continue; 
			}
			if (methodDelay.currFrame == methodDelay.frame) {
				methodDelay.method.methodInfo.Invoke (methodDelay.method.mono, methodDelay.parameters);
				methodDelayListDelte.Add (methodDelay);
			} else {
				methodDelay.currFrame++;
			}
		}
		// 统一删除调用过的延时函数
		foreach (MethodDelay methodDelay in methodDelayListDelte) {
			if (methodDelayList.Contains (methodDelay)) {
				methodDelayList.Remove (methodDelay);
			}
		}
	}

	public static JsonObject getFrameOrder (int uid)
	{
		if (playerOrder.ContainsKey (uid)) {
			return playerOrder [uid];
		} else {
			return null;
		}
	}

	public static void Register (MonoBehaviour mono)
	{
		Method method = new Method (mono, mono.GetType ().GetMethod ("FrameUpdate", BindingFlags.NonPublic | BindingFlags.Instance));
		if (methodList.Contains (method)) {
			return;
		}
		methodListTemp.Add (method);
	}

	public static void Unregister (MonoBehaviour mono)
	{
		foreach (Method method in methodList) {
			if (method.mono.Equals (mono)) {
				methodListDelete.Add (method);
				break;
			}
		}
	}

	public static void InvokeDelay (MonoBehaviour mono, float delay, string methodName, object[] parameters)
	{
		int frame = (int)((VInt)delay / frameIntervalTime).scalar;
		InvokeDelayByFrame (mono, frame, methodName, parameters);
	}

	public static void InvokeDelayByFrame (MonoBehaviour mono, int frame, string methodName, object[] parameters)
	{
		Method method = new Method (mono, mono.GetType ().GetMethod (methodName, BindingFlags.NonPublic | BindingFlags.Instance));
		methodDelayListTemp.Add (new MethodDelay (method, parameters, frame));
	}

	private class Method
	{
		public MonoBehaviour mono;
		public MethodInfo methodInfo;

		public Method (MonoBehaviour mono, MethodInfo methodInfo)
		{
			this.mono = mono;
			this.methodInfo = methodInfo;
		}
	}

	private class MethodDelay
	{
		public Method method;
		public object[] parameters;
		public int currFrame;
		public int frame;

		public MethodDelay (Method method, object[] parameters, int frame)
		{
			this.method = method;
			this.parameters = parameters;
			this.currFrame = 0;
			this.frame = frame;
		}

	}

}



