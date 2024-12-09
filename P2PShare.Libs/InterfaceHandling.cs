using System.Net.NetworkInformation;

namespace P2PShare.Libs
{
    public class InterfaceHandling
    {
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
            for (int j = 0; j < interfaces.Length; j++)
            {
                if (interfaces[j].OperationalStatus == OperationalStatus.Up)
                {
                    interfacesUp.Add(interfaces[j]);
                }
            }
            if (interfacesUp.Count == 0)
            {
                throw new Exception("No up network interfaces found");
            }

            return interfacesUp;
        }
    }
}
