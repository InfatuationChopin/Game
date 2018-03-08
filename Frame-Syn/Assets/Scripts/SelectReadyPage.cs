using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleJson;
using System;

public class SelectReadyPage : MonoBehaviour {

	private Text log;
	private Text btnPlayerInfo1;
	private Text btnPlayerInfo2;
	private Text btnPlayerInfo3;
	private Text btnPlayerInfo4;
	private Text btnPlayerInfo5;
	private Text btnPlayerInfo6;
	private Text btnCountDown;

	// 倒计时相关
	private const float countIntervalTime = 1.0f;
	private float lastCountTime = 0;
	private int time = 0;

	void Start () {
		log = GameObject.Find ("LogText").GetComponent<Text> ();
		btnPlayerInfo1 = GameObject.Find ("ButtonPlayerInfo1").GetComponentInChildren<Text> ();
		btnPlayerInfo2 = GameObject.Find ("ButtonPlayerInfo2").GetComponentInChildren<Text> ();
		btnPlayerInfo3 = GameObject.Find ("ButtonPlayerInfo3").GetComponentInChildren<Text> ();
		btnPlayerInfo4 = GameObject.Find ("ButtonPlayerInfo4").GetComponentInChildren<Text> ();
		btnPlayerInfo5 = GameObject.Find ("ButtonPlayerInfo5").GetComponentInChildren<Text> ();
		btnPlayerInfo6 = GameObject.Find ("ButtonPlayerInfo6").GetComponentInChildren<Text> ();
		btnCountDown = GameObject.Find ("ButtonEnterGameCountDown").GetComponentInChildren<Text> ();

		// 显示双方的信息
		time = Convert.ToInt32(Global.selectReadyData["time"]);
		ShowInfo ();
		// 事件监听
		Listen ();
	}
	
	void FixedUpdate () {
		if (Time.time - lastCountTime <= countIntervalTime) {
			return;
		}
		lastCountTime = Time.time;
		if (time > 0) {
			time--;
		} else {
			time = 0;
		}
		btnCountDown.text = "" + time;
	}

	public void OnEnterGameClick ()
	{
		log.text += "selectReady\n";
		PomeloCli.Request ("center.selectHandler.ready", data => {
			log.text += data.ToString () + "\n";
		});
	}


	public void OnLogClearClick ()
	{
		log.text = "";
	}

	public void OnResetClick ()
	{
		log.text += "reset\n";
		JsonObject msg = new JsonObject ();
		PomeloCli.Notify ("center.matchHandler._clearDataAll", msg);
	}

	void Listen ()
	{
		PomeloCli.On ("selectReadyAdd", data => {
			log.text += "@selectReadyAdd: " + data.ToString () + "\n";
			int uid = Convert.ToInt32 (data ["uid"]);
			Text[] textPlayer = new Text[] {
				btnPlayerInfo1,
				btnPlayerInfo2,
				btnPlayerInfo3,
				btnPlayerInfo4,
				btnPlayerInfo5,
				btnPlayerInfo6
			};
			foreach (Text text in textPlayer) {
				if (text.text == "" + uid) {
					text.color = Color.red;
				}
			}
		});
		PomeloCli.On ("selectStart", data => {
			log.text += "@selectStart: " + data.ToString () + "\n";
			btnCountDown.text = "";
			Global.selectStartData = data;
			gotoSelect ();
		});
		PomeloCli.On ("selectReadyCancel", data => {
			log.text += "@selectReadyCancel: " + data.ToString () + "\n";
			Application.Quit();
		});
	}

	void ShowInfo()
	{
		Text[] textTeamA = new Text[]{ btnPlayerInfo1, btnPlayerInfo2, btnPlayerInfo3 };
		Text[] textTeamB = new Text[]{ btnPlayerInfo4, btnPlayerInfo5, btnPlayerInfo6 };
		JsonArray teamA = (JsonArray)Global.selectReadyData ["teamA"];
		JsonArray teamB = (JsonArray)Global.selectReadyData ["teamB"];
		for (int i = 0; i < teamA.Count; i++) {
			JsonObject msg = (JsonObject)teamA [i];
			int uid = Convert.ToInt32 (msg ["uid"]);
			bool ready = Convert.ToBoolean (msg ["ready"]);
			textTeamA [i].text = "" + uid;
			if (ready) {
				textTeamA [i].color = Color.red;
			} else {
				textTeamA [i].color = Color.black;
			}
		}
		for (int i = 0; i < teamB.Count; i++) {
			JsonObject msg = (JsonObject)teamB [i];
			int uid = Convert.ToInt32 (msg ["uid"]);
			bool ready = Convert.ToBoolean (msg ["ready"]);
			textTeamB [i].text = "" + uid;
			if (ready) {
				textTeamB [i].color = Color.red;
			} else {
				textTeamB [i].color = Color.black;
			}
		}
	}


	void gotoSelect()
	{
		SceneManager.LoadScene (3);
	}

}
