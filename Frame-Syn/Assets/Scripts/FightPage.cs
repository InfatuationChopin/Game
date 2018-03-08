using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJson;
using UnityEngine.SceneManagement;

public class FightPage : MonoBehaviour
{
	// 存储进度条实体对象的字典，仅在加载游戏进度页面使用
	private Dictionary<int, Text> progressTextComponent = new Dictionary<int, Text> ();

	void Start ()
	{
		// 画布移动到初始位置
		GameObject canvas = GameObject.Find ("Canvas");
		canvas.transform.position = new Vector3 (0, 0, 0);
		// 准备页面，主要是加载进度展示
		ShowReady ();
		// 开始加载资源
		StartCoroutine (LoadData ());

		// 战斗时的回调监听
		PomeloCli.On ("frame", data => {
			Debug.Log("frame => " + data);
			// 加入帧指令集合
			LogicFrame.frameOrderList.Add (data);
		});
		// 战斗重连
		PomeloCli.On ("frameReconnect", data => {
			JsonArray record = (JsonArray) data["record"];
			foreach(JsonObject frameOrder in record) {
				// 加入帧指令集合
				LogicFrame.frameOrderList.Add (frameOrder);
			}
		});
		// 重连时的初始状态
		PomeloCli.On ("frameReconnectInit", data => {
			LogicFrame.frame = Convert.ToInt64(data["frame"]);
			JsonObject initData = (JsonObject)data["data"];
			JsonArray userDatas = (JsonArray)initData["userDatas"];
			foreach(JsonObject userData in userDatas) {
				int uid = Convert.ToInt32(userData["uid"]);
				int x = Convert.ToInt32(userData["x"]);
				int y = Convert.ToInt32(userData["y"]);
				int z = Convert.ToInt32(userData["z"]);
				int hp = Convert.ToInt32(userData["hp"]);

				// 初始化状态
				VInt3 initPostion = new VInt3(x, y, z);
				Global.entity[uid].transform.position = (Vector3)initPostion;
				Player player = Global.entity[uid].GetComponent<Player>();
				player.mover.position = initPostion;
				player.blood.currValue = hp;
				if (hp == 0) {
					int rebornTime = Convert.ToInt32(userData["rebornTime"]);
					int rebornFrame = Convert.ToInt32(userData["rebornFrame"]);
					player.Death(player.uid, rebornTime, rebornFrame);
				}
			}
		});
		// 进度条变化
		PomeloCli.On ("progress", (data) => {
			int uid = Convert.ToInt32 (data ["uid"]);
			int progress = Convert.ToInt32 (data ["progress"]);
			if (progressTextComponent.ContainsKey (uid)) {
				progressTextComponent [uid].text = uid + ": " + progress + "%";
			}
		});
		// 游戏真正开始
		PomeloCli.On ("fightStart", data => {
			Debug.Log("fightStart");
			// 隐藏进度条的面板
			GameObject.Find ("ProgressPanel").SetActive (false);
			// 相机跟随
			Global.entity [Global.uid].AddComponent<CameraMove> ();
			// 计算网络延迟
			gameObject.AddComponent<CountPing> ();
			// 人物控制组件
			gameObject.AddComponent<PlayerEasyTouch> ();
			// 逻辑帧控制脚本
			gameObject.AddComponent<LogicFrame> ();
		});
		// 监控游戏结束
		PomeloCli.On ("fightEnd", (data) => {
			Application.Quit ();
		});
	}

	void Update ()
	{
		if (Input.GetKeyDown (KeyCode.Escape)) {
			JsonObject msg = new JsonObject ();
			PomeloCli.Notify ("center.matchHandler._clearDataAll", msg);
		}
	}

	void ShowReady ()
	{
		// 获取玩家信息
		JsonArray users = Global.fightReadyData ["users"] as JsonArray;
		// 初始化队伍所有人的进度条控件
		GameObject _progressText = Resources.Load<GameObject> ("Prefabs/ProgressText");
		GameObject progressPanel = GameObject.Find ("ProgressPanel");
		for (int i = 0; i < users.Count; i++) {
			JsonObject user = (JsonObject)users [i];
			int uid = Convert.ToInt32 (user ["uid"]);
			int heroCode = Convert.ToInt32 (user ["heroCode"]);
			int team = Convert.ToInt32 (user ["team"]);
			int progress = Convert.ToInt32 (user ["progress"]);

			GameObject progressText = Instantiate (_progressText);
			Text textComponent = progressText.GetComponent<Text> ();
			if (uid == Global.uid) {
				textComponent.color = Color.red;
			}
			textComponent.text = uid + ": " + progress + "%";
			progressTextComponent.Add (uid, textComponent);
			progressText.transform.parent = progressPanel.transform;
			if (team == 1) {
				if (users.Count == 2) {
					progressText.transform.position = new Vector3 (0, 50, 0);
				} else if (users.Count == 6) {
					progressText.transform.position = new Vector3 (((i % 3) * 200) - 200, 50, 0);
				}
			} else if (team == 2) {
				if (users.Count == 2) {
					progressText.transform.position = new Vector3 (0, -150, 0);
				} else if (users.Count == 6) {
					progressText.transform.position = new Vector3 (((i % 3) * 200) - 200, -150, 0);
				}
			}
		}

		// 判断自己队伍的玩家和对方队伍的玩家
		for (int i = 0; i < users.Count; i++) {
			JsonObject user = (JsonObject)users [i];
			int uid = Convert.ToInt32 (user ["uid"]);
			int heroCode = Convert.ToInt32 (user ["heroCode"]);
			int team = Convert.ToInt32 (user ["team"]);
			if (uid == Global.uid) {
				Global.team = team;
				break;
			}
		}
		foreach (JsonObject user in users) {
			int uid = Convert.ToInt32 (user ["uid"]);
			int heroCode = Convert.ToInt32 (user ["heroCode"]);
			int team = Convert.ToInt32 (user ["team"]);
			if (team == Global.team) {
				Global.myTeamUid.Add (uid);
			} else {
				Global.otherTeamUid.Add (uid);
			}
		}
	}

	IEnumerator LoadData ()
	{
		// 获取玩家信息
		JsonArray users = Global.fightReadyData ["users"] as JsonArray;
		JsonObject msg = new JsonObject ();

		// 加载静态资源
		yield return new WaitForSeconds (0.2f);
		SendProgress (0);
		GameObject _hero3007 = Resources.Load<GameObject> ("Prefabs/hero3007");
		yield return new WaitForSeconds (0.2f);
		SendProgress (15);
		GameObject _hero3008 = Resources.Load<GameObject> ("Prefabs/hero3008");
		yield return new WaitForSeconds (0.2f);
		SendProgress (30);
		GameObject _map = Resources.Load<GameObject> ("Prefabs/Map");
		yield return new WaitForSeconds (0.2f);
		SendProgress (45);
		GameObject _easyTouch = Resources.Load<GameObject> ("Prefabs/Touch");
		yield return new WaitForSeconds (0.2f);
		SendProgress (60);

		// 加载所有人物
		for (int i = 0; i < users.Count; i++) {
			JsonObject user = (JsonObject)users [i];
			int uid = Convert.ToInt32 (user ["uid"]);
			int heroCode = Convert.ToInt32 (user ["heroCode"]);
			int team = Convert.ToInt32 (user ["team"]);

			// 创建人物实例
			GameObject et = new GameObject ();
			GameObject etc = null;
			if (heroCode == 3007) {
				etc = Instantiate (_hero3007);
			} else if (heroCode == 3008) {
				etc = Instantiate (_hero3008);
			}

			// 添加 Player 脚本并初始化一些变量
			Player p = et.AddComponent<Player> ();
			p.uid = uid;
			p.speedNormal = Global.hero [heroCode].speed;
			p.speedReal = p.speedNormal;
			p.atkSpeedNormal = Global.hero [heroCode].atkSpeed;
			p.atkSpeedReal = p.atkSpeedNormal;
			p.atkRange = Global.hero [heroCode].atkRange;
			p.atkTime = Global.hero [heroCode].atkTime;
			p.atkType = Global.hero [heroCode].atkType;
			p.animation = etc.GetComponent<Animation> ();
			p.heroCode = heroCode;
			p.isDeath = false;
			p.isAttack = false;

			// 人物血条的创建
			GameObject _blood = Resources.Load<GameObject> ("Prefabs/Blood");
			GameObject blood = Instantiate (_blood);
			if (Global.team == team) {
				Image[] images = blood.GetComponentsInChildren<Image> ();
				foreach (Image image in images) {
					if (image.gameObject.name == "Fill") {
						image.color = Color.green;
					}
				}
			}
			Canvas canvas = blood.GetComponent<Canvas> ();
			canvas.worldCamera = Camera.main;
			Blood bloodComponent = blood.AddComponent<Blood> ();
			bloodComponent.host = et;
			bloodComponent.currValue = Global.hero [heroCode].hpMax;
			bloodComponent.maxValue = Global.hero [heroCode].hpMax;
			p.blood = bloodComponent;

			// 人物位置的设定
			etc.transform.parent = et.transform;
			float x = i < 3 ? 52 : 58;
			float y = 0;
			float z = (i % 3) + 49;
			if (users.Count == 2) {
				x = i == 0 ? 52 : 58;
				z = 50;
			}
			et.transform.position = new Vector3 (x, y, z);

			// 将创建的实体对象加入字典
			Global.entity.Add (uid, et);
		}

		yield return new WaitForSeconds (0.2f);
		SendProgress (80);

		// 加载地图和 EasyTouch 组件
		Instantiate (_map);
		Instantiate (_easyTouch);
		yield return new WaitForSeconds (0.2f);
		SendProgress (100);
	}

	void SendProgress (int progress)
	{
		JsonObject msg = new JsonObject ();
		msg ["progress"] = progress;
		PomeloCli.Notify ("fight.fightHandler.progress", msg);
	}

}
