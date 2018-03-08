using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJson;
using Pomelo.DotNetClient;
using System;

public class CountPing : MonoBehaviour
{
	private static VInt uploadIntervalMillis = (VInt)1.0f;
	private VInt lastUploadTime = (VInt)0;
	private long delayTime = 0;

	void Start ()
	{
	}

	void Update ()
	{
		if ((VInt)Time.time - lastUploadTime < uploadIntervalMillis) {
			return;
		}
		lastUploadTime = (VInt)Time.time;

		// 获取服务器时间，并计算时间差
		JsonObject msg = new JsonObject ();
		PomeloCli.Request ("fight.fightHandler.timee", msg, (data) => {
			delayTime = Convert.ToInt64 (data ["ping"]);
		});
	}

	void OnGUI()
	{
		GUI.color = Color.red;
		GUI.Label(new Rect(10, 10, 100, 20), "ping: " + delayTime.ToString() + "ms");
	}

}
