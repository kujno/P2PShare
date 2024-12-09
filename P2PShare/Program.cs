using P2PShare.CLI;

namespace P2PShare
{
    class Program
    {
        static void Main()
        {
            CLIHelp.SetDesign();

            CLIFileTransport.SharingLoop();

            CLIHelp.Goodbye();

            Console.ReadKey();
        }
    }
}