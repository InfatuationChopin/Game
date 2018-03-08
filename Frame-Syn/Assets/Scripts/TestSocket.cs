using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.UI;
using SimpleJson;

public class TestSocket : MonoBehaviour
{

	private Socket clientSocket;
	private byte[] sdata = new byte[1024];
	private Thread thread;
	private bool isConnect;

	private GameObject inputHost;
	private InputField host;

	private static float intervalUploadPosition = 1f / 15;
	private float lastUploadPositionTime = 0;

	private static float intervalUploadPosition2 = 1f;
	private float lastUploadPositionTime2 = 0;

	private Dictionary<int, long> times = new Dictionary<int, long> ();
	private int timeCount = 0;
	private float delayTime = 0;

	void Start ()
	{  
//		inputHost = GameObject.Find ("InputHost");
//		host = inputHost.GetComponent<InputField> ();
		thread = new Thread (new ThreadStart (ConnectServer));  
		thread.Start ();  
	}

	void Update ()
	{  
		if (!isConnect) {
			return;
		}

		if (Time.time - lastUploadPositionTime <= intervalUploadPosition) {
			return;
		}
		lastUploadPositionTime = Time.time;

		JsonObject msg = new JsonObject ();
		msg ["type"] = "move";
		msg ["x"] = 100;
		msg ["y"] = 100;
		msg ["z"] = 100;
		msg ["vx"] = 100;
		msg ["vy"] = 100;
		msg ["vz"] = 100;
		msg ["status"] = 0;
//		sendMsg (msg.ToString () + "#");

		if (Time.time - lastUploadPositionTime2 <= intervalUploadPosition2) {
			return;
		}
		lastUploadPositionTime2 = Time.time;

		msg = new JsonObject ();
		msg ["type"] = "time";
		msg ["timeCount"] = timeCount;
		times.Add (timeCount, Global.GetTimeStamp ());
		timeCount++;
		sendMsg (msg.ToString () + "#");
	}

	public void ConnectServer ()
	{  
		IPEndPoint ipep = new IPEndPoint (IPAddress.Parse ("47.96.9.117"), 5001);  
		clientSocket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);  
		try {  
			clientSocket.Connect (ipep);  
			isConnect = true;
			Debug.Log ("连接成功");
		} catch (SocketException ex) {  
			Debug.Log ("connect error: " + ex.Message);  
			return;  
		}  

		while (true) {  
			//接收服务器信息  
			int bufLen = 0;  
			try {  
				bufLen = clientSocket.Available;  
				clientSocket.Receive (sdata, 0, bufLen, SocketFlags.None);  
				if (bufLen == 0) {  
					continue;  
				}  
			} catch (Exception ex) {  
				Debug.Log ("Receive Error:" + ex.Message);  
				return;  
			}  
			string receiver = System.Text.Encoding.UTF8.GetString (sdata).Substring (0, bufLen); 
			string[] temp = receiver.Split ('#');
			foreach (string t in temp) {
				if (t == "") {
					continue;
				}
				JsonObject msg = SimpleJson.SimpleJson.DeserializeObject<JsonObject> (t);
				string type = msg ["type"].ToString ();
				if (type == "time") {
					int count = Convert.ToInt32 (msg ["timeCount"]);
					if (times.ContainsKey (count)) {
						delayTime = Global.GetTimeStamp () - times [count];
						times.Remove (count);
					}
				}
				Debug.Log ("客户端收到:" + t);  
			}
		}  
	}

	public void sendMsg (string sendStr)
	{  
		byte[] data = new byte[1024];  
		data = Encoding.UTF8.GetBytes (sendStr);
		clientSocket.Send (data, data.Length, SocketFlags.None);  
	}


	void OnGUI ()
	{  
		GUI.color = Color.green;
		GUI.Label (new Rect (100, 10, 100, 30), "ping: " + delayTime.ToString () + "ms");
	}

	void OnDestroy ()
	{  
		if (thread != null) {
			thread.Abort ();  
		}
		if (clientSocket != null) {  
			clientSocket.Close ();  
		}  
	}
}
