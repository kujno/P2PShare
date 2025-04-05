using System.Net.NetworkInformation;

namespace P2PShare.Libs
{
    public class InterfaceHandling
    {
        public static event EventHandler? InterfaceDown;

        public static List<NetworkInterface> GetUpInterfaces()
        {
            NetworkInterface?[] interfacesNullable = NetworkInterface.GetAllNetworkInterfaces();
            List<NetworkInterface> interfacesUp = new List<NetworkInterface>();

            // interfaces check
            if (interfacesNullable is null)
            {
                throw new Exception("No network interfaces found");
            }

            NetworkInterface[] interfaces = interfacesNullable.Where(ni => ni != null).Cast<NetworkInterface>().ToArray();

            // up interfaces check
            for (int i = 0; i < interfaces.Length; i++)
            {
                if (interfaces[i].OperationalStatus == OperationalStatus.Up && !interfaces[i].Name.ToLower().Contains("loopback"))
                {
                    interfacesUp.Add(interfaces[i]);
                }
            }
            if (interfacesUp.Count == 0)
            {
                throw new Exception("No up network interfaces found");
            }

            return interfacesUp;
        }

        public static async Task MonitorInterface(NetworkInterface @interface, CancellationToken cancellationToken)
        {
            try
            {
                while (@interface.OperationalStatus == OperationalStatus.Up)
                {
                    if (cancellationToken.IsCancellationRequested)
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
