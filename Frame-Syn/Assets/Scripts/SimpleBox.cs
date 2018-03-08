using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJson;

public class SimpleBox : MonoBehaviour
{
	public string text;
	public bool isShow = false;
	public int uid;
	public int tid;

	void Start ()
	{
	}

	void Update ()
	{
		
	}

	void OnGUI ()
	{
		if (!isShow) {
			return;
		}
		GUI.Box (new Rect (Screen.width / 2, Screen.height / 4, Screen.width / 2, Screen.height / 2), "好友邀请");  
		GUI.Label (new Rect (Screen.width / 2, Screen.height / 3,  Screen.width / 2, Screen.height / 2), text);  
		if (GUI.Button (new Rect (Screen.width / 2, Screen.height * 0.65f,  Screen.width / 10, Screen.height / 16), "接受")) {  
			isShow = false;
			SendInviteReply (1);
		}  
		if (GUI.Button (new Rect (Screen.width * 0.7f, Screen.height * 0.65f,  Screen.width / 10, Screen.height / 16), "拒绝")) {  
			isShow = false;
			SendInviteReply (2);
		}  
	}

	void SendInviteReply(int reply)
	{		
		JsonObject msg = new JsonObject ();
		msg ["uid"] = uid;
		msg ["tid"] = tid;
		msg ["reply"] = reply;
		msg ["elo"] = 0;
		PomeloCli.Notify ("center.matchHandler.inviteReply", msg);
	}

}
