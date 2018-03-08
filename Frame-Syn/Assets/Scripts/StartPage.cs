using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using SimpleJson;
using System.Threading;
using System;

public class StartPage : MonoBehaviour
{

	public const int Status_None = 0;
	public const int Status_Team = 1;
	public const int Status_Match = 2;
	public const int Status_Select = 3;
	public const int Status_Fight = 4;

	private Text log;
	private InputField host;
	private SimpleBox simpleBox;

	void Start ()
	{
		Screen.SetResolution (780, 439, false);

		log = GameObject.Find ("LogText").GetComponent<Text> ();
		host = GameObject.Find ("InputHost").GetComponent<InputField> ();
//		host.text = "180.153.140.230"; // 公司公网
//		host.text = "192.168.80.110"; // 公司局域网，左的电脑
//		host.text = "47.96.9.117"; // 金梧阿里云
		host.text = "114.55.230.240"; // 公司阿里云
		simpleBox = GameObject.Find ("SimpleBox").GetComponent<SimpleBox> ();
	}

	public void OnLoginClick (GameObject button)
	{
		int uid = 0;
		switch (button.name) {
		case "ButtonLogin1":
			uid = 1111;
			break;
		case "ButtonLogin2":
			uid = 2222;
			break;
		case "ButtonLogin3":
			uid = 3333;
			break;
		case "ButtonLogin4":
			uid = 4444;
			break;
		case "ButtonLogin5":
			uid = 5555;
			break;
		case "ButtonLogin6":
			uid = 6666;
			break;
		}
		log.text = "login, uid=" + uid + ", host=" + host.text + "\n";
		PomeloCli.Init (host.text, 3010, uid, data => {
			log.text += data.ToString () + "\n";
			int code = Convert.ToInt32 (data ["code"]);
			if (code == 200) {
				Global.uid = uid;
				Listen ();
				// 判断当前的状态，重连
				int status = Convert.ToInt32 (data ["status"]);
				StatusHandler (status);
			}
		});
	}

	void StatusHandler (int status)
	{
		switch (status) {
		case Status_Team:
		case Status_Match:
			log.text += "team reconnect\n";
			PomeloCli.Request ("center.matchHandler.getTeam", data => {
				log.text += data.ToString () + "\n";
				int code = Convert.ToInt32 (data ["code"]);
				if (code == 200) {
					Global.teamData = data;
					gotoTeam ();
				}
			});
			break;
		case Status_Select:
			log.text += "select reconnect\n";
			PomeloCli.Request ("center.selectHandler.getSelect", data => {
				log.text += data.ToString () + "\n";
				int code = Convert.ToInt32 (data ["code"]);
				if (code == 200) {
					bool ready = Convert.ToBoolean (data ["ready"]);
					if (ready) {
						Global.selectReadyData = (JsonObject)data ["data"];
						gotoSelectReady ();
					} else {
						Global.selectStartData = (JsonObject)data ["data"];
						gotoSelect ();
					}
				}
			});
			break;
		case Status_Fight:
			log.text += "fight reconnect\n";
			PomeloCli.Request ("fight.fightHandler.getFight", data => {
				log.text += data.ToString () + "\n";
				int code = Convert.ToInt32 (data ["code"]);
				if (code == 200) {
					Global.fightReadyData = data;
					gotoFight ();
				}
			});
			break;
		}
	}

	public void OnReset ()
	{
		log.text += "reset\n";
		JsonObject msg = new JsonObject ();
		PomeloCli.Notify ("center.matchHandler._clearDataAll", msg);
	}


	public void OnCreateTeamClick ()
	{
		log.text += "createTeam\n";
		JsonObject msg = new JsonObject ();
		msg ["elo"] = 0;
		PomeloCli.Request ("center.matchHandler.createTeam", msg, data => {
			log.text += data.ToString () + "\n";
		});
	}

	public void OnJuyuwangClick ()
	{
		host.text = "192.168.80.110";
	}

	public void OnGongwangClick ()
	{
		host.text = "114.55.230.240";
	}

	public void OnLogClearClick ()
	{
		log.text = "";
	}

	private void Listen ()
	{
		PomeloCli.On ("invite", data => {
			log.text += "@invite: " + data.ToString () + "\n";
			simpleBox.text = "来自好友【" + data ["uid"].ToString () + "】的邀请";
			simpleBox.uid = Convert.ToInt32 (data ["uid"]);
			simpleBox.tid = Convert.ToInt32 (data ["tid"]);
			simpleBox.isShow = true;
		});
		PomeloCli.On ("team", data => {
			log.text += "@team: " + data.ToString () + "\n";
			data ["time"] = 0;
			data ["match"] = false;
			Global.teamData = data;
			gotoTeam ();
		});
	}



	void gotoTeam ()
	{
		SceneManager.LoadScene (1);
	}

	void gotoSelectReady ()
	{
		SceneManager.LoadScene (2);
	}

	void gotoSelect ()
	{
		SceneManager.LoadScene (3);
	}

	void gotoFight ()
	{
		SceneManager.LoadScene (4);
	}

}
