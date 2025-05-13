﻿using System.Net.NetworkInformation;

namespace P2PShare.Libs
{
    public class InterfaceHandling
    {
        public static event EventHandler? InterfaceDown;

        public static List<NetworkInterface> GetUpInterfaces()
        {
            NetworkInterface?[] interfacesNullable = NetworkInterface.GetAllNetworkInterfaces();
            List<NetworkInterface> interfacesUp = new List<NetworkInterface>();

            if (interfacesNullable is null)
            {
                throw new Exception("No network interfaces found");
            }

            NetworkInterface[] interfaces = interfacesNullable.Where(ni => ni != null).Cast<NetworkInterface>().ToArray();

            for (int i = 0; i < interfaces.Length; i++)
            {
                NetworkInterface ni = interfaces[i];
                string description = ni.Description.ToLowerInvariant();
                string id = ni.Id.ToLowerInvariant();
                bool isVirtual =
                    description.Contains("virtual") ||
                    description.Contains("vmware") ||
                    description.Contains("hyper-v") ||
                    description.Contains("loopback") ||
                    description.Contains("tunnel") ||
                    description.Contains("pseudo") ||
                    id.Contains("virtual") ||
                    id.Contains("vmware") ||
                    id.Contains("hyper-v");

                if (
                    ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                    !isVirtual &&
                    ni.GetIPProperties().UnicastAddresses.Count > 0
                )
                {
                    interfacesUp.Add(ni);
                }
            }

            return interfacesUp;
        }

        public static async Task MonitorInterface(NetworkInterface @interface, Cancellation cancellation)
        {
            try
            {
                while (@interface.OperationalStatus == OperationalStatus.Up)
                {
                    if (cancellation.TokenSource is null || cancellation.TokenSource.Token.IsCancellationRequested)
                    {
                        return;
                    }

                    await Task.Delay(1000);
                }
            }
            catch
            {

            }

            OnInterfaceDown();
        }

        private static void OnInterfaceDown()
        {
            InterfaceDown?.Invoke(null, EventArgs.Empty);
        }
    }
}
