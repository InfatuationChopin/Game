using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleJson;
using System;

public class SelectPage : MonoBehaviour
{
	private Text log;
	private Text btnTeamPlayer1;
	private Text btnTeamPlayer2;
	private Text btnTeamPlayer3;
	private Text btnCountDown;

	// 倒计时相关
	private const float countIntervalTime = 1.0f;
	private float lastCountTime = 0;
	private int time = 0;

	// 队伍信息
	private ArrayList teamInfo = new ArrayList ();

	public class TeamInfo
	{
		public int uid;
		public bool confirm;
		public int heroCode;

		public TeamInfo (int uid, bool confirm, int heroCode)
		{
			this.uid = uid;
			this.confirm = confirm;
			this.heroCode = heroCode;
		}
	}

	void Start ()
	{
		log = GameObject.Find ("LogText").GetComponent<Text> ();
		btnTeamPlayer1 = GameObject.Find ("ButtonTeamPlayer1").GetComponentInChildren<Text> ();
		btnTeamPlayer2 = GameObject.Find ("ButtonTeamPlayer2").GetComponentInChildren<Text> ();
		btnTeamPlayer3 = GameObject.Find ("ButtonTeamPlayer3").GetComponentInChildren<Text> ();
		btnCountDown = GameObject.Find ("ButtonCountDown").GetComponentInChildren<Text> ();

		// 队伍信息初始化
		time = Convert.ToInt32(Global.selectStartData["time"]);
		JsonArray team = (JsonArray)Global.selectStartData ["team"];
		foreach (JsonObject msg in team) {
			int uid = Convert.ToInt32 (msg ["uid"]);
			bool confirm = Convert.ToBoolean (msg ["confirm"]);
			int heroCode = Convert.ToInt32 (msg ["heroCode"]);
			teamInfo.Add (new TeamInfo (uid, confirm, heroCode));
		}
		ShowTeamInfo ();
		// 事件监听
		Listen ();
	}

	void FixedUpdate ()
	{
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

	public void OnSelectHeroClick (GameObject button)
	{
		int heroCode = 0;
		switch (button.name) {
		case "ButtonHero3007":
			heroCode = 3007;
			break;
		case "ButtonHero3008":
			heroCode = 3008;
			break;
		}
		log.text += "selectHero " + heroCode + "\n";
		JsonObject msg = new JsonObject ();
		msg ["heroCode"] = heroCode;
		PomeloCli.Request ("center.selectHandler.select", msg, data => {
			log.text += data.ToString () + "\n";
		});
	}

	public void OnConfirmClick ()
	{
		log.text += "confirm\n";
		PomeloCli.Request ("center.selectHandler.confirm", data => {
			log.text += data.ToString () + "\n";
		});
	}

	public void OnCancelClick ()
	{
		log.text += "cancel\n";
		PomeloCli.Request ("center.selectHandler.cancel", data => {
			log.text += data.ToString () + "\n";
		});
	}

	public void OnResetClick ()
	{
		log.text += "reset\n";
		JsonObject msg = new JsonObject ();
		PomeloCli.Notify ("center.matchHandler._clearDataAll", msg);
	}

	public void OnLogClearClick ()
	{
		log.text = "";
	}

	void Listen ()
	{
		PomeloCli.On ("selectHero", data => {
			log.text += "@selectHero: " + data.ToString () + "\n";
			int uid = Convert.ToInt32 (data ["uid"]);
			int heroCode = Convert.ToInt32 (data ["heroCode"]);
			foreach (TeamInfo team in teamInfo) {
				if (team.uid == uid) {
					team.heroCode = heroCode;
					break;
				}
			}
			ShowTeamInfo ();
		});
		PomeloCli.On ("selectConfirm", data => {
			log.text += "@selectConfirm: " + data.ToString () + "\n";
			int uid = Convert.ToInt32 (data ["uid"]);
			foreach (TeamInfo team in teamInfo) {
				if (team.uid == uid) {
					team.confirm = true;
					break;
				}
			}
			ShowTeamInfo ();
		});
		PomeloCli.On ("selectCancel", data => {
			log.text += "@selectCancel: " + data.ToString () + "\n";
			int uid = Convert.ToInt32 (data ["uid"]);
			foreach (TeamInfo team in teamInfo) {
				if (team.uid == uid) {
					team.confirm = false;
					break;
				}
			}
			ShowTeamInfo ();
		});
		PomeloCli.On ("fightReady", (data) => {
			log.text += "@fightReady: " + data.ToString () + "\n";
			Global.fightReadyData = data;
			gotoFight ();
		});
	}

	void ShowTeamInfo ()
	{
		Text[] textTeam = new Text[]{ btnTeamPlayer1, btnTeamPlayer2, btnTeamPlayer3 };
		for (int i = 0; i < teamInfo.Count; i++) {
			TeamInfo team = (TeamInfo)teamInfo [i];
			if (team.confirm) {
				textTeam [i].text = team.uid + "\n" + team.heroCode + "\n已确认";
			} else {
				textTeam [i].text = team.uid + "\n" + team.heroCode;
			}
			if (Global.uid == team.uid) {
				textTeam [i].color = Color.red;
			} else {
				textTeam [i].color = Color.black;
			}
		}
	}

	void gotoFight ()
	{
		SceneManager.LoadScene (4);
	}

}
