using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleJson;
using System;


public class TeamPage : MonoBehaviour
{
	private Text log;
	private Text btnMatch;
	private Text btnTeamPlayer1;
	private Text btnTeamPlayer2;
	private Text btnTeamPlayer3;

	// 顺计时相关
	private const float countIntervalTime = 1.0f;
	private float lastCountTime = 0;
	private int matchTime = 0;
	private bool isMatch = false;

	// 队伍信息
	private ArrayList teamInfo = new ArrayList ();
	private int leaderUid = 0;

	void Start ()
	{
		log = GameObject.Find ("LogText").GetComponent<Text> ();
		btnMatch = GameObject.Find ("ButtonMatch").GetComponentInChildren<Text> ();
		btnTeamPlayer1 = GameObject.Find ("ButtonTeamPlayer1").GetComponentInChildren<Text> ();
		btnTeamPlayer2 = GameObject.Find ("ButtonTeamPlayer2").GetComponentInChildren<Text> ();
		btnTeamPlayer3 = GameObject.Find ("ButtonTeamPlayer3").GetComponentInChildren<Text> ();

		// 队伍信息初始化
		matchTime = Convert.ToInt32 (Global.teamData["time"]);
		isMatch = Convert.ToBoolean (Global.teamData["match"]);
		leaderUid = Convert.ToInt32 (Global.teamData ["leader"]);
		JsonArray teamInfoData = (JsonArray)Global.teamData ["team"];
		for (int i = 0; i < teamInfoData.Count; i++) {
			int uid = Convert.ToInt32 (teamInfoData [i]);
			teamInfo.Add (uid);
		}
		ShowTeamInfo ();
		// 事件监听
		Listen ();
	}

	void FixedUpdate ()
	{
		if (!isMatch) {
			matchTime = 0;
			return;
		}
		if (Time.time - lastCountTime <= countIntervalTime) {
			return;
		}
		lastCountTime = Time.time;
		matchTime++;
		btnMatch.text = "" + matchTime;
	}


	public void OnInviteClick (GameObject button)
	{
		int uid = 0;
		switch (button.name) {
		case "ButtonInvite1":
			uid = 1111;
			break;
		case "ButtonInvite2":
			uid = 2222;
			break;
		case "ButtonInvite3":
			uid = 3333;
			break;
		case "ButtonInvite4":
			uid = 4444;
			break;
		case "ButtonInvite5":
			uid = 5555;
			break;
		case "ButtonInvite6":
			uid = 6666;
			break;
		}
		log.text += "invite, uid=" + uid + "\n";
		JsonObject msg = new JsonObject ();
		msg ["uid"] = uid;
		PomeloCli.Request ("center.matchHandler.invite", msg, data => {
			log.text += data.ToString () + "\n";
		});
	}

	public void OnMatchClick ()
	{
		log.text += "match\n";
		PomeloCli.Request ("center.matchHandler.match", data => {
			log.text += data.ToString () + "\n";
		});
	}

	public void OnMatchCancelClick ()
	{
		log.text += "match cancel\n";
		PomeloCli.Request ("center.matchHandler.cancelMatch", data => {
			log.text += data.ToString () + "\n";
		});
	}

	public void OnLeaveClick ()
	{
		log.text += "leave\n";
		PomeloCli.Request ("center.matchHandler.leaveTeam", data => {
			log.text += data.ToString () + "\n";
		});
	}

	public void OnKickClick (GameObject button)
	{
		int i = 0;
		switch (button.name) {
		case "ButtonTeamPlayer1":
			i = 0;
			break;
		case "ButtonTeamPlayer2":
			i = 1;
			break;
		case "ButtonTeamPlayer3":
			i = 2;
			break;
		}
		if (i < teamInfo.Count) {
			int uid = Convert.ToInt32 (teamInfo [i]);
			log.text += "kick, uid=" + uid + "\n";
			JsonObject msg = new JsonObject ();
			msg ["uid"] = uid;
			PomeloCli.Request ("center.matchHandler.kick", msg, data => {
				log.text += data.ToString () + "\n";
			});
		}
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
		PomeloCli.On ("inviteReply", data => {
			log.text += "@inviteReply: " + data.ToString () + "\n";
		});
		PomeloCli.On ("teamJoin", data => {
			log.text += "@teamJoin: " + data.ToString () + "\n";
			int uid = Convert.ToInt32 (data ["uid"]);
			if (!teamInfo.Contains (uid)) {
				teamInfo.Add (uid);
			}
			ShowTeamInfo ();
		});
		PomeloCli.On ("teamLeave", data => {
			log.text += "@teamLeave: " + data.ToString () + "\n";
			int uid = Convert.ToInt32 (data ["uid"]);
			if (teamInfo.Contains (uid)) {
				teamInfo.Remove (uid);
			}
			ShowTeamInfo ();
			if (uid == Global.uid) {
				Application.Quit ();
			}
		});
		PomeloCli.On ("teamLeader", data => {
			log.text += "@teamLeader: " + data.ToString () + "\n";
			int uid = Convert.ToInt32 (data ["uid"]);
			leaderUid = uid;
			ShowTeamInfo ();
		});
		PomeloCli.On ("teamKick", data => {
			log.text += "@teamKick: " + data.ToString () + "\n";
			int uid = Convert.ToInt32 (data ["uid"]);
			if (teamInfo.Contains (uid)) {
				teamInfo.Remove (uid);
			}
			ShowTeamInfo ();
			if (uid == Global.uid) {
				Application.Quit ();
			}
		});
		PomeloCli.On ("match", data => {
			log.text += "@match: " + data.ToString () + "\n";
			btnMatch.text = "" + matchTime;
			isMatch = true;
		});
		PomeloCli.On ("matchCancel", data => {
			log.text += "@matchCancel: " + data.ToString () + "\n";
			btnMatch.text = "匹配";
			isMatch = false;
		});
		PomeloCli.On ("selectReady", data => {
			log.text += "@selectReady: " + data.ToString () + "\n";
			btnMatch.text = "匹配";
			isMatch = false;
			Global.selectReadyData = data;
			gotoSelectReady();
		});
	}

	void ShowTeamInfo ()
	{
		Text[] textTeam = new Text[]{ btnTeamPlayer1, btnTeamPlayer2, btnTeamPlayer3 };
		for (int i = 0; i < teamInfo.Count; i++) {
			int uid = Convert.ToInt32 (teamInfo [i]);
			if (uid == Global.uid) {
				textTeam [i].color = Color.red; 
			} else {
				textTeam [i].color = Color.black; 
			}
			string text = "";
			if (uid == leaderUid) {
				text = uid + "(房主)";
			} else {
				text = uid + "";
			}
			if (Global.uid == leaderUid) {
				text += "\n点击踢人";
			}
			textTeam [i].text = text;
		}
		for (int i = teamInfo.Count; i < textTeam.Length; i++) {
			textTeam [i].text = "空";
			textTeam [i].color = Color.black;
		}
	}

	void gotoSelectReady()
	{
		SceneManager.LoadScene (2);
	}

}
