using System;

namespace Pomelo.DotNetClient
{
	public class KcpConfig
	{
		private KcpConfig ()
		{
		}

		public static void Conv (uint conv)
		{
			KcpTransporter.conv = conv;
		}

		public static void Nodelay (int nodelay, int interval, int resend, int nc)
		{
			KcpTransporter.nodelay = nodelay;
			KcpTransporter.interval = interval;
			KcpTransporter.resend = resend;
			KcpTransporter.nc = nc;
		}

		public static void Wndsize (int sndwnd, int rcvwnd)
		{
			KcpTransporter.sndwnd = sndwnd;
			KcpTransporter.rcvwnd = rcvwnd;
		}

		public static void Setmtu (int mtu)
		{
			KcpTransporter.mtu = mtu;
		}
	}
}

