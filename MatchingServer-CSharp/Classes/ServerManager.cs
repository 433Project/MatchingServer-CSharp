using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using FlatBuffers;
using fb;
using Protocol;

namespace MatchingServer_CSharp.Classes
{
    class ServerManager
    {
        //###########################################
        //             Fields/Properties
        //###########################################
        private Logs logs;
        private ConnectionManager connectionManager;
        private MessageProcessor messageProcessor;

        //Properties
        public bool IsInitialized { get; private set; } = false;
        public string LocalMatchingServerID { get; private set; }
        public int LocalMatchingServerIDCode { get; private set; }


        //###########################################
        //              Public Methods
        //###########################################


        /// <summary>
        /// Starts the matching server program. Creates a connection with the ConfigServer and obtains the server’s ID code.  
        /// It then starts four program loops: ConfigServerLoop, MatchingServerListenLoop, PlayerManagerLoop, and MatchingManagerLoop.
        /// </summary>
        /// <returns>This method returns true if the program was started without fail and false if a critical initialization component failed.</returns>
        public bool Initialize ()
        {
            Debug.Assert(!IsInitialized, "ServerManager already initialized. Cannot initialize again.");

            logs = new Logs();
            logs.ReportMessage("ServerManager initializing. . .");

            connectionManager = new ConnectionManager();
            connectionManager.Initialize();
            messageProcessor = new MessageProcessor();
            messageProcessor.Initialize();


            // 1. Connect with ConfigServer
            if (!ConnectWithConfigServer(1000))
            {
                return false;
            }


            // 2. Register with ConfigServer
            bool result = GetIDFromConfigServer();

            while (!result)     // If registration failed (socket issue, reattempt)
            {
                if (!ConnectWithConfigServer(1000))
                {
                    return false;
                }
                result = GetIDFromConfigServer();
            }


            // 3. Create ConfigServer async service loop
            StartConfigServerLoop();

            // 4. Create MatchingServer async service loop


            // 5. Initialize PlayerManager


            // 6. Initialize MatchingManager


            IsInitialized = true;
            return true;
        }



        //###########################################
        //              Private Methods
        //###########################################


        /// <summary>
        /// This method attempts to connect to the ConfigServer and upon failure, will retry numberOfAttempts number of times (or infinite if 0).
        /// </summary>
        /// <param name="numberOfAttempts">The number of attempts to try to connect (infinite if 0).</param>
        /// <returns>Returns false if it could not connect with the ConfigServer and true upon success.</returns>
        private bool ConnectWithConfigServer (uint numberOfAttempts)
        {
            Debug.Assert(connectionManager != null, "ConnectionManager is null. ServerManager cannot call ConnectWithConfigServer.");

            IPEndPoint configServerEndPoint = null;
            if (!ConfigReader.GetIPEndPoint("ConfigServer", out configServerEndPoint))
            {
                logs.ReportError("ServerManager.ConnectWithConfigServer: Cannot retrieve ConfigServer IPEndPoint");
                return false;
            }
            
            while (!connectionManager.ConnectWithConfigServerSync(configServerEndPoint))
            {
                switch (numberOfAttempts)
                {
                    case 0:         // Infinite tries
                        break;
                    case 1:
                        logs.ReportError("ServerManager.ConnectWithConfigServer: Could not connect with ConfigServer");
                        return false;
                    default:
                        --numberOfAttempts;
                        break;
                }
            }
            logs.ReportMessage("ServerManager.ConnectWithConfigServer: Successfully connected to ConfigServer IP: " + configServerEndPoint.Address.ToString() + ":" + configServerEndPoint.Port);

            return true;
        }


        /// <summary>
        /// This method synchronously obtains the local MatchingServer ID from the ConfigServer.
        /// </summary>
        /// <returns>Returns false if either sending the ID request or receiving the answer failed and true upon successful registration.</returns>
        private bool GetIDFromConfigServer ()
        {
            Debug.Assert(connectionManager != null, "ConnectionManager is null. ServerManager cannot use ConnectionManager.");

            byte[] message;
            messageProcessor.PackMessage(
                new Header(0, TerminalType.MatchingServer, 0, TerminalType.ConfigServer, 0),
                Command.MatchingServerIDRequest,
                Status.None,
                "",
                "",
                out message);

            if (!connectionManager.SendMessageToConfigServerSync(message))
            {
                logs.ReportError("GetIDFromConfigServer: Could not send MatchingServerIDRequest to ConfigServer");
                return false;
            }
            logs.ReportMessage("GetIDFromConfigServer: Sent MatchingServerIDRequest to ConfigServer");

            if (!connectionManager.ReceiveMessageFromConfigServerSync(out message))
            {
                logs.ReportError("GetIDFromConfigServer: Could not receive message from ConfigServer");
                return false;
            }
            logs.ReportMessage("GetIDFromConfigServer: Received message from ConfigServer");

            Packet packet;
            messageProcessor.UnPackMessage(message, out packet);
            logs.ReportPacket(packet);

            LocalMatchingServerID = packet.body.Data1;
            int matchingServerSourceCode;
            if (!int.TryParse(LocalMatchingServerID, out matchingServerSourceCode))
            {
                logs.ReportError("GetIDFromConfigServer: Invalid ID received from ConfigServer. Could not parse ID data: " + packet.body.Data1);
                matchingServerSourceCode = -1;
                return false;
            }
            LocalMatchingServerIDCode = matchingServerSourceCode;

            return true;
        }


        /// <summary>
        /// This method makes a request to the ConfigServer for MatchingServer data, then initiates asynchronous receiving.
        /// </summary>
        private void StartConfigServerLoop ()
        {
            logs.ReportMessage("ServerManager.StartConfigServerLoop: Starting async loop. . .");
            // 1. Send request for matching server data
            byte[] message;
            messageProcessor.PackMessage(
                new Header(0, TerminalType.MatchingServer, LocalMatchingServerIDCode, TerminalType.ConfigServer, 0),
                Command.MatchingServerListRequest,
                Status.None,
                "",
                "",
                out message);
            if (!connectionManager.SendMessageToConfigServerSync(message))
            {
                logs.ReportError("SendMessageToConfigServerSync: Could not send MatchingServerListRequest to ConfigServer");
                Task.Run(ResetConnectionWithConfigServer);
                return;
            }
            logs.ReportMessage("SendMessageToConfigServerSync: Sent MatchingServerListRequest to ConfigServer");

            // 2. Start async loop
            Task.Run(ConfigServerLoop);
        }


        /// <summary>
        /// This method loops asynchronous receive calls to the ConfigServer in order to receive messages from the ConfigServer.
        /// </summary>
        async Task ConfigServerLoop ()
        {
            byte[] message = new byte[100];
            if(!(await connectionManager.ReceiveMessageFromConfigServerAsync(message))) {
                // Error with connection
                logs.ReportMessage("ServerManager.ConfigServerLoop: Attempting to reset connection with ConfigServer. . .");
                Task.Run(ResetConnectionWithConfigServer);
                return;
            }

            // Message Received
            Packet packet;
            if (!messageProcessor.UnPackMessage(message, out packet))
            {
                logs.ReportError("ConfigServerLoop: Unable to unpack message.");
            }
            logs.ReportPacket(packet);

            // Interpret message
            switch (packet.body.Cmd)
            {
                case Command.HealthCheckRequest:
                    logs.ReportMessage("ConfigServerLoop: Received Health Check Request from ConfigServer");
                    message = null;
                    messageProcessor.PackMessage(
                        new Header(0, TerminalType.MatchingServer, 0, TerminalType.ConfigServer, 0),
                        Command.HealthCheckResponse,
                        Status.None,
                        "",
                        "",
                        out message);
                    if (!connectionManager.SendMessageToConfigServerSync(message))
                    {
                        logs.ReportError("SendMessageToConfigServerSync: Could not send message to ConfigServer");
                        Task.Run(ResetConnectionWithConfigServer);
                        return;
                    }
                    logs.ReportMessage("ConfigServerLoop: Sent Received Health Check Response to ConfigServer");
                    break;
                default:
                    break;
            }

            // Wait for next message
            Task.Run(ConfigServerLoop);
        }


        /// <summary>
        /// This method retrieves the ConfigServer endpoint from file and initiates background reconnection with the ConfigServer.
        ///  Afterwards, the method restarts the ConfigServer loop.
        /// </summary>
        async Task ResetConnectionWithConfigServer ()
        {
            // 1. Get the ConfigServer end point from XML config
            IPEndPoint configServerEndPoint = null;
            if (!ConfigReader.GetIPEndPoint("ConfigServer", out configServerEndPoint))
            {
                logs.ReportError("ServerManager.ResetConnectionWithConfigServer: Cannot retrieve ConfigServer IPEndPoint");
                return;
            }

            // 2. Initiate a background reconnection attempt
            if (!(await connectionManager.ConnectWithConfigServerAsync(configServerEndPoint))) {
                logs.ReportError("ServerManager.ResetConnectionWithConfigServer: Cannot reconnect with ConfigServer");
                return;
            }

            // 3. Restart the ConfigServer loop
            logs.ReportMessage("ServerManager.ResetConnectionWithConfigServer: Reconnected with ConfigServer at " + configServerEndPoint.Address);
            StartConfigServerLoop();
        }


        /// <summary>
        /// Initiates listening for MatchingServers and then starts a loop with asynchronous accept calls to receive new connections from new MatchingServers.
        /// </summary>
        /// <returns>This function returns false if the listening socket creation failed, otherwise returns true.</returns>
        private bool StartMatchingServerListeningLoop ()
        {
            return false;
        }


        /// <summary>
        /// This method runs a loop with asynchronous accept calls to receive new MatchingServers
        /// </summary>
        async private void MatchingServerAcceptingLoop ()
        {

        }


        /// <summary>
        /// Loop asynchronous receive calls on a new MatchingServer connection in order to receive messages from that MatchingServer.
        /// </summary>
        async void StartNewMatchingServerLoop ()
        {

        }


        /// <summary>
        /// Calculate a fake latency score for a connection with a particular server and call WaitingRoom.SetServerLatency() to set it in the WaitingRoom.
        /// </summary>
        /// <returns>Returns a float value representing the latency value of the connection with a MatchingServer.</returns>
        private float CalculateLatency ()
        {
            return -1f;
        }
    }
}
