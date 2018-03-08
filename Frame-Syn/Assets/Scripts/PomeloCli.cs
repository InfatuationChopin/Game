using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pomelo.DotNetClient;
using SimpleJson;
using System;

public class PomeloCli : MonoBehaviour
{
	private static PomeloClient cli = null;
	private static List<MsgAction> actions = new List<MsgAction> ();
	private static List<MsgAction> actionsExec = new List<MsgAction> ();

	void Start ()
	{
		DontDestroyOnLoad (this);
	}

	public static void Init (string host, int port, int uid, Action<JsonObject> act)
	{
		cli = new PomeloClient ();
		cli.initClient (host, port, () => {
			cli.connect ((hs) => {
				JsonObject msg = new JsonObject ();
				msg ["uid"] = uid;
				cli.request ("gate.gateHandler.queryEntry", msg, data => {
					cli.disconnect ();
					Entry (Convert.ToString (data ["host"]), Convert.ToInt32 (data ["port"]), uid, act, data);
				});
			});
		});
	}

	private static void Entry (string host, int port, int uid, Action<JsonObject> act, JsonObject actData)
	{
		cli.initClient (host, port, () => {
			cli.connect ((hs) => {
				JsonObject msg = new JsonObject ();
				msg ["uid"] = uid;
				cli.request ("connector.entryHandler.entry", msg, data => {
					lock (actions) {
						actions.Add (new MsgAction (act, data));
					}
				});
			});
		});
	}

	public static void Notify (string route, JsonObject msg)
	{
		if (cli == null)
			return;
		cli.notify (route, msg);
	}

	public static void Request (string route, Action<JsonObject> act)
	{
		if (cli == null)
			return;
		long startTime = Global.GetTimeStamp ();
		cli.request (route, (data) => {
			long endTime = Global.GetTimeStamp ();
			data ["ping"] = endTime - startTime;
			lock (actions) {
				actions.Add (new MsgAction (act, data));
			}
		});
	}

	public static void Request (string route, JsonObject msg, Action<JsonObject> act)
	{
		if (cli == null)
			return;
		long startTime = Global.GetTimeStamp ();
		cli.request (route, msg, (data) => {
			long endTime = Global.GetTimeStamp ();
			data ["ping"] = endTime - startTime;
			lock (actions) {
				actions.Add (new MsgAction (act, data));
			}
		});
	}

	public static void On (string ev, Action<JsonObject> act)
	{
		if (cli == null)
			return;
		cli.on (ev, data => {
			lock (actions) {
				actions.Add (new MsgAction (act, data));
			}
		});
	}

	public static void On2 (string ev, Action<JsonObject> act)
	{
		if (cli == null)
			return;
		cli.on (ev, data => {
			try {
				act.Invoke(data);
			} catch (Exception e) {
				Debug.LogError ("error2 => " + e);
			}
		});
	}


	public static void disconnect ()
	{
		if (cli == null)
			return;
		cli.disconnect ();
	}

	void Update ()
	{
		lock (actions) {
			actionsExec.AddRange (actions);
			actions.Clear ();
		}
		foreach (MsgAction act in actionsExec) {
			try {
				act.act.Invoke (act.obj);
			} catch (Exception e) {
				Debug.LogError ("error => " + e);
			}
		}
		actionsExec.Clear ();
	}

	private class MsgAction
	{
		public Action<JsonObject> act;
		public JsonObject obj;

		public MsgAction (Action<JsonObject> act, JsonObject obj)
		{
			this.act = act;
			this.obj = obj;
		}
	}
}
