using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatchingServer_CSharp.Classes;

namespace MatchingServer_CSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerManager serverManager = new ServerManager();

            ConfigReader.Initialize();
            ConfigReader.SetFixedMessageSizeFromFile();

            serverManager.Initialize();

            Console.ReadKey();
        }
    }
}
