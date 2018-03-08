using SimpleJson;
using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Pomelo.DotNetClient
{
	/// <summary>
	/// network state enum
	/// </summary>
//	public enum NetWorkState
//	{
//		[Description("initial state")]
//		CLOSED,
//
//		[Description("connecting server")]
//		CONNECTING,
//
//		[Description("server connected")]
//		CONNECTED,
//
//		[Description("disconnected with server")]
//		DISCONNECTED,
//
//		[Description("connect timeout")]
//		TIMEOUT,
//
//		[Description("netwrok error")]
//		ERROR
//	}

	public class KcpPomeloClient : IDisposable
	{
		/// <summary>
		/// netwrok changed event
		/// </summary>
		public event Action<NetWorkState> NetWorkStateChangedEvent;


		private NetWorkState netWorkState = NetWorkState.CLOSED;   //current network state

		private EventManager eventManager;
		private UdpClient socket;
		private KcpProtocol protocol;
		private bool disposed = false;
		private uint reqId = 1;

		public KcpPomeloClient()
		{
		}

		/// <summary>
		/// initialize pomelo client
		/// </summary>
		/// <param name="host">server name or server ip (www.xxx.com/127.0.0.1/::1/localhost etc.)</param>
		/// <param name="port">server port</param>
		/// <param name="callback">socket successfully connected callback(in network thread)</param>
		public void initClient(string host, int port, Action callback = null)
		{
			eventManager = new EventManager();
			NetWorkChanged(NetWorkState.CONNECTING);

			IPEndPoint ie = new IPEndPoint(IPAddress.Parse(host), port);
			socket = new UdpClient (host, port);
			socket.Connect (ie);

			this.protocol = new KcpProtocol(this, this.socket, ie);
			NetWorkChanged(NetWorkState.CONNECTED);

			if (callback != null)
			{
				callback();
			}
		}

		/// <summary>
		/// 网络状态变化
		/// </summary>
		/// <param name="state"></param>
		private void NetWorkChanged(NetWorkState state)
		{
			netWorkState = state;

			if (NetWorkStateChangedEvent != null)
			{
				NetWorkStateChangedEvent(state);
			}
		}

		public void connect()
		{
			connect(null, null);
		}

		public void connect(JsonObject user)
		{
			connect(user, null);
		}

		public void connect(Action<JsonObject> handshakeCallback)
		{
			connect(null, handshakeCallback);
		}

		public bool connect(JsonObject user, Action<JsonObject> handshakeCallback)
		{
			try
			{
				protocol.start(user, handshakeCallback);
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				return false;
			}
		}

		private JsonObject emptyMsg = new JsonObject();
		public void request(string route, Action<JsonObject> action)
		{
			this.request(route, emptyMsg, action);
		}

		public void request(string route, JsonObject msg, Action<JsonObject> action)
		{
			this.eventManager.AddCallBack(reqId, action);
			protocol.send(route, reqId, msg);

			reqId++;
		}

		public void notify(string route, JsonObject msg)
		{
			protocol.send(route, msg);
		}

		public void on(string eventName, Action<JsonObject> action)
		{
			eventManager.AddOnEvent(eventName, action);
		}

		internal void processMessage(Message msg)
		{
			if (msg.type == MessageType.MSG_RESPONSE)
			{
				//msg.data["__route"] = msg.route;
				//msg.data["__type"] = "resp";
				eventManager.InvokeCallBack(msg.id, msg.data);
			}
			else if (msg.type == MessageType.MSG_PUSH)
			{
				//msg.data["__route"] = msg.route;
				//msg.data["__type"] = "push";
				eventManager.InvokeOnEvent(msg.route, msg.data);
			}
		}

		public void disconnect()
		{
			Dispose();
			NetWorkChanged(NetWorkState.DISCONNECTED);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code
		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed)
				return;

			if (disposing)
			{
				// free managed resources
				if (this.protocol != null)
				{
					this.protocol.close();
				}

				if (this.eventManager != null)
				{
					this.eventManager.Dispose();
				}

				try
				{
					this.socket.Close();
					this.socket = null;
				}
				catch (Exception)
				{
					//todo : 有待确定这里是否会出现异常，这里是参考之前官方github上pull request。emptyMsg
				}

				this.disposed = true;
			}
		}
	}
}