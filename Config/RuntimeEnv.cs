﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using Cairo;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;

namespace Vintagestory.API.Config
{
    /// <summary>
    /// Operating System Enum
    /// </summary>
    public enum OS
    {
        Windows,
        Mac,
        Linux
    }

    /// <summary>
    /// Information about the runningtime environment
    /// </summary>
    public static class RuntimeEnv
    {
#if DEBUG
        private const bool debugAll = false;   // Dev team can use this to enable all these debuggers, if required
#else
        private const bool debugAll = false;
#endif
        /// <summary>
        /// If TEXTURE_DEBUG_DISPOSE is set, the initial value set here will be overridden
        /// </summary>
        public static bool DebugTextureDispose = debugAll;
        /// <summary>
        /// If VAO_DEBUG_DISPOSE is set, the initial value set here will be overridden
        /// </summary>
        public static bool DebugVAODispose = debugAll;
        /// <summary>
        /// Debug sound memory leaks. No ENV var
        /// </summary>
        public static bool DebugSoundDispose = debugAll;

        /// <summary>
        /// If true, will print the stack trace on some of the blockaccessor if something attempts to get or set blocks outside of its available chunks
        /// </summary>
        public static bool DebugOutOfRangeBlockAccess = debugAll;

        /// <summary>
        /// If true, will print allocation trace whenever a new task was enqueued to the thread pool
        /// </summary>
        public static bool DebugThreadPool = debugAll;

        

        public static int MainThreadId;
        public static int ServerMainThreadId;


        public static float GUIScale;

        /// <summary>
        /// The current operating system
        /// </summary>
        public static readonly OS OS;

        /// <summary>
        /// The Env variable which contains the OS specific search paths for libarires
        /// </summary>
        public static readonly string EnvSearchPathName;

        public static readonly string EnvPathSeperator;

        /// <summary>
        /// .dll for windows, .so for linux, .dylib for mac
        /// </summary>
        public static readonly string LibExtension;

        static RuntimeEnv()
        {
            if (System.IO.Path.DirectorySeparatorChar == '\\')
            {
                EnvPathSeperator = ";";
                OS = OS.Windows;
                EnvSearchPathName = "PATH";
                LibExtension = ".dll";
                return;
            }
			if (IsMac())
            {
				OS = OS.Mac;
				EnvSearchPathName = "DYLD_FRAMEWORK_PATH";
				LibExtension = ".dylib";
                EnvPathSeperator = ":";
                return;
            }

			OS = OS.Linux;
			EnvSearchPathName = "LD_LIBRARY_PATH";
			LibExtension = ".so";
            EnvPathSeperator = ":";
        }

		[DllImport("libc")]
		static extern int uname(IntPtr buf);

		static bool IsMac()
		{
			IntPtr buf = IntPtr.Zero;
			try
			{
				buf = Marshal.AllocHGlobal(8192);
				if (uname(buf) == 0)
				{
					string os = Marshal.PtrToStringAnsi(buf);
					if (os == "Darwin") return true;
				}
			}
			catch
			{
			}
			finally
			{
				if (buf != IntPtr.Zero) Marshal.FreeHGlobal(buf);
			}
			return false;
		}

        /// <summary>
        /// Whether the game is run using the Mono framework
        /// </summary>
        public static readonly bool IsMono = Type.GetType("Mono.Runtime") != null;

        /// <summary>
        /// Whether we are in a dev environment or not
        /// </summary>
        public static readonly bool IsDevEnvironment = !Directory.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets"));


        public static string GetLocalIpAddress()
        {
            try
            {
                // This seems to be the preferred / more reliable way of getting the ip
                // but it seems of of the methods are not implemented in mono so we fallback 
                // to a simple method if an exception is thrown

                UnicastIPAddressInformation mostSuitableIp = null;

                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var network in networkInterfaces)
                {
                    if (network.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    var properties = network.GetIPProperties();

                    if (properties.GatewayAddresses.Count == 0)
                    {
                        continue;
                    }

                    foreach (var address in properties.UnicastAddresses)
                    {
                        if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                        {
                            continue;
                        }

                        if (IPAddress.IsLoopback(address.Address))
                        {
                            continue;
                        }

                        if (!address.IsDnsEligible)
                        {
                            if (mostSuitableIp == null) mostSuitableIp = address;
                            continue;
                        }

                        // The best IP is the IP got from DHCP server
                        if (address.PrefixOrigin != PrefixOrigin.Dhcp)
                        {
                            if (mostSuitableIp == null || !mostSuitableIp.IsDnsEligible) mostSuitableIp = address;
                            continue;
                        }

                        return address.Address.ToString();
                    }
                }

                return mostSuitableIp != null ? mostSuitableIp.Address.ToString() : "";
            } catch (Exception)
            {
                try
                {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    var ipAddress = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                    return ipAddress.ToString();
                } catch (Exception)
                {
                    return "Unknown ip";
                }
                
            }
            
        }
    }
}
