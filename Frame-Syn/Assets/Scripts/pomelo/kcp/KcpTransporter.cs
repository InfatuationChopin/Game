using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Pomelo.DotNetClient
{

    public class KcpTransporter
    {
		internal static uint conv = 123;
		internal static int nodelay = 1;
		internal static int interval = 25;
		internal static int resend = 2;
		internal static int nc = 1;
		internal static int sndwnd = 32;
		internal static int rcvwnd = 32;
		internal static int mtu = 1400;

		private KCP kcp;
		private UdpClient socket;
		private IPEndPoint remoteIP;
        private Action<byte[]> messageProcesser;
		private Thread thread;
		private bool threadRun;
		internal Action onDisconnect = null;

		public KcpTransporter(UdpClient socket, IPEndPoint remoteIP, Action<byte[]> processer)
        {
            this.socket = socket;
			this.remoteIP = remoteIP;
            this.messageProcesser = processer;

			kcp = new KCP (conv, (byte[] buf, int size) =>
				{
					try{
						//UnityEngine.Debug.Log("usend: "+size+" >> "+print(buf, 0, size));
						this.socket.Send(buf, size);
					}catch(Exception e){
						//UnityEngine.Debug.LogError("ksend ==> "+e);
					}
				});
			kcp.NoDelay(nodelay, interval, resend, nc);
			kcp.WndSize(sndwnd, rcvwnd);
			kcp.SetMtu (mtu);

			thread = new Thread (check);
        }

        public void start()
        {
			recv ();
			threadRun = true;
			thread.Start ();
        }

		private void recv()
		{
			socket.BeginReceive (new AsyncCallback ((ret) => {
				try{
					byte[] data = socket.EndReceive(ret, ref remoteIP);
					//UnityEngine.Debug.Log("urecv: "+data.Length+" >> "+print(data));
					kcp.Input(data);
					for (int size = kcp.PeekSize(); size > 0; size = kcp.PeekSize()){
						byte[] buffer = new byte[size];
						if (kcp.Recv(buffer) > 0){
							//UnityEngine.Debug.Log("krecv: "+size+" >> "+print(buffer));
							this.messageProcesser.Invoke(buffer);
						}
					}
				}catch(SocketException e){
					close();
					if (this.onDisconnect != null){
						this.onDisconnect();
					}
				}catch(Exception e){
					//UnityEngine.Debug.LogError("krecv ==> "+e);
				}finally{
					if(threadRun){
						recv();
					}
				}
			}), null);
		}

		private void check()
		{
			while (threadRun) {
				uint now = (UInt32)(Convert.ToInt64(DateTime.UtcNow.Subtract(new DateTime (1970, 1, 1)).TotalMilliseconds) & 0xffffffff);
				kcp.Update (now);
				uint delay = kcp.Check (now) - now;
				Thread.Sleep ((int) delay);
			}
			//UnityEngine.Debug.Log("kupdate: end");
		}

        public void send(byte[] buffer)
        {
			//UnityEngine.Debug.Log("ksend: "+buffer.Length+" >> "+print(buffer));
			kcp.Send (buffer);
        }

        internal void close()
        {
			threadRun = false;
			//UnityEngine.Debug.Log("kclose: ");
        }


		//log
		public static string print(byte[] buff, int start=0, int size=-1)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			if (size < 0) {
				size = buff.Length;
			}
			for(int i=start; i<size; i++){
				char ch = (char)buff [i];
				sb.Append (ch=='\0' ?'_' :ch);
			}
			return sb.ToString ();
		}
    }


}