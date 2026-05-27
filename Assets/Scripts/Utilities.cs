using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace UnityCEClient
{
    public class Utils
    {
        public static string GenerateId()
        {
            string adaptorType = "UNITY";
            IPAddress ip = GetSocketLocalIP();

            UnityEngine.Debug.Log(ip.ToString());

            // Combine IP bytes + port into a single uint to hash
            byte[] ipBytes = ip.GetAddressBytes(); // assumes IPv4
            uint seed = System.BitConverter.ToUInt32(ipBytes, 0);

            // FNV-32a hash
            uint hash = 2166136261u;
            for (int i = 0; i < 4; i++)
            {
                hash ^= (byte)(seed >> (i * 8));
                hash *= 16777619u;
            }

            // Encode as base-36, 6 chars
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            char[] result = new char[6];
            for (int i = 5; i >= 0; i--)
            {
                result[i] = chars[(int)(hash % 36)];
                hash /= 36;
            }

            string typeStr = adaptorType;

            string prefix = typeStr.Length >= 3
                ? typeStr[..3].ToUpper()
                : typeStr.ToUpper().PadRight(3, 'X');

            UnityEngine.Debug.Log($"Generated GUID: {prefix}-{new string(result)}");

            return $"{prefix}-{new string(result)}";
        }

        // Finds and return assigned IPv4 from local network interfaces (first found)
        public static IPAddress GetSocketLocalIP()
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    Console.WriteLine($"MAC Address: {nic.GetPhysicalAddress()}");
                    IPInterfaceProperties ipProps = nic.GetIPProperties();

                    foreach (var uni in ipProps.UnicastAddresses)
                    {
                        if (uni.Address.AddressFamily == AddressFamily.InterNetwork && !uni.Address.Equals(System.Net.IPAddress.Loopback))
                        {
                            // Found assigned IPv4 address, return that
                            return uni.Address;
                        }
                    }
                }
            }

            // No IPv4 found, so return this
            return IPAddress.Loopback;
        }
    }
}