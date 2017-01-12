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
        private static MessageProcessor messageProcessor;

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


            // 4. Create MatchingServer async listening loop
            if (!StartMatchingServerListeningLoop())
            {

            }


            // 5. Initialize PlayerManager


            // 6. Initialize MatchingManager


            IsInitialized = true;
            return true;
        }



        /// <summary>
        /// This method provides a way for another class, such as the ConnectionManager, to determine the ID of a new connection by reading the srcCode (source code) of the first message received.
        /// </summary>
        /// <param name="message">The message containing the data.</param>
        /// <returns></returns>
        public static string ExtractMatchingServerID (byte[] message)
        {
            Debug.Assert(message.Length >= 20, "Message passed to ExtractMatchingServerID is of incorrect length.");

            Packet packet;
            messageProcessor.UnPackMessage(message, out packet);

            return packet.header.srcCode.ToString();
        }



        //###########################################
        //              Private Methods
        //###########################################
        //              ConfigServer
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
                logs.ReportError("ServerManager.ConfigServerLoop: Attempting to reset connection with ConfigServer. . .");
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

                case Command.MatchingServerListResponse:
                    logs.ReportMessage("ConfigServerLoop: Received MatchingServerListResponse from ConfigServer: ID: " + packet.body.Data1 + " IP: " + packet.body.Data2);
                    StartMatchingServerConnection(packet.body.Data1, packet.body.Data2);
                    break;

                case Command.MatchingServerIDVerifyResponse:
                    logs.ReportMessage("ConfigServerLoop: Received MatchingServerIDVerifyResponse from ConfigServer: ID: " + packet.body.Data1 + " Status: " + packet.body.status);
                    int peerMatchingServerCode;
                    if (!int.TryParse(packet.body.Data1, out peerMatchingServerCode))
                    {
                        peerMatchingServerCode = -1;
                    }
                    if (packet.body.status != Status.Success)
                    {
                        logs.ReportError("ConfigServerLoop: Disconnecting unverified MatchingServer ID: " + packet.body.Data1);
                        message = null;
                        messageProcessor.PackMessage(
                            new Header(0, TerminalType.MatchingServer, LocalMatchingServerIDCode, TerminalType.MatchingServer, peerMatchingServerCode),
                            Command.MatchingServerIDTransmitResponse,
                            Status.Fail,
                            "",
                            "",
                            out message);
                        Task denyMatchingServer = connectionManager.SendMessageToMatchingServerAsync(packet.body.Data1, message)
                            .ContinueWith(antecedent =>
                            {
                                // If the send message failed, don't bother disconnecting since SendMessageToMatchingServerAsync already does that on failure
                                if (antecedent.Result)
                                {
                                    connectionManager.DisconnectMatchingServer(packet.body.Data1);
                                }
                            });

                        break;
                    }

                    // A. Inform the MatchingServer their transmission was successful
                    message = null;
                    messageProcessor.PackMessage(
                            new Header(0, TerminalType.MatchingServer, LocalMatchingServerIDCode, TerminalType.MatchingServer, peerMatchingServerCode),
                            Command.MatchingServerIDTransmitResponse,
                            Status.Success,
                            "",
                            "",
                            out message);
                    Task verifiedMatchingServer = connectionManager.SendMessageToMatchingServerAsync(packet.body.Data1, message)
                        .ContinueWith(antecedent =>
                       {
                           if (antecedent.Result)
                           {
                                // B. Start MatchingServer receive loop for that server

                            }
                       });
                    verifiedMatchingServer.Start();
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



        //###########################################
        //          MatchingServer Methods
        //###########################################


        /// <summary>
        /// This method attempts to start a connection with another MatchingServer, then start a corresponding async receive loop.
        /// </summary>
        /// <param name="matchingServerID">The ID of the peer MatchingServer.</param>
        /// <param name="ip">The IP address of the peer MatchingServer.</param>
        private void StartMatchingServerConnection (string matchingServerID, string ip)
        {
            Task.Run(async () =>
            {
                // 1. Attempt to make a connection
                if (!await connectionManager.ConnectWithMatchingServerAsync(matchingServerID, ip))
                {
                    return;
                }


                // 2. Send the local MS ID to the peer MS
                byte[] message = new byte[100];
                messageProcessor.PackMessage(
                        new Header(0, TerminalType.MatchingServer, LocalMatchingServerIDCode, TerminalType.MatchingServer, 0),
                        Command.MatchingServerIDTransmit,
                        Status.None,
                        "",
                        "",
                        out message);
                await connectionManager.SendMessageToMatchingServerAsync(matchingServerID, message);


                // 3. Start a Async MS Receive loop
                MatchingServerLoop(matchingServerID);
            });
        }


        /// <summary>
        /// Initiates listening for MatchingServers and then starts a loop with asynchronous accept calls to receive new connections from new MatchingServers.
        /// </summary>
        /// <returns>This function returns false if the listening socket creation failed, otherwise returns true.</returns>
        private bool StartMatchingServerListeningLoop ()
        {
            if (!connectionManager.CreateMatchingServerListeningSocket(LocalMatchingServerID))
            {
                logs.ReportError("ServerManager.StartMatchingServerListeningLoop: Could not initialize a MatchingServer listening socket.");
                return false;
            }

            logs.ReportMessage("ServerManager.StartMatchingServerListeningLoop: MatchingServer listening socket initialized successfully.");
            MatchingServerAcceptingLoop();
            return true;
        }


        /// <summary>
        /// This method runs a loop with asynchronous accept calls to receive new MatchingServers
        /// </summary>
        async private Task MatchingServerAcceptingLoop ()
        {
            byte[] message = new byte[100];
            if (!await connectionManager.AcceptMatchingServerConnectionAsync(message))
            {
                // An accept call or first messsage received failed
                MatchingServerAcceptingLoop();
                return;
            }
            VerifyNewMatchingServer(message);

            MatchingServerAcceptingLoop();
        }


        /// <summary>
        /// This method takes the first message from a peer MS and verifies the identify with the ConfigServer before creating a receive message loop. 
        /// Failure to verify results in calling ConnectionManager to end the connection.
        /// </summary>
        /// <param name="message">A byte[] holding the first message.</param>
        async private Task VerifyNewMatchingServer (byte[] message)
        {
            await Task.Run(() => 
            {
                // 1. Get message details
                Packet packet;
                messageProcessor.UnPackMessage(message, out packet);
                logs.ReportPacket(packet);
                string peerMatchingServerID = packet.header.srcCode.ToString();


                // 2. Send verification message to ConfigServer
                logs.ReportMessage("ServerManager.VerifyNewMatchingServer: Verifying MS #" + peerMatchingServerID + " with ConfigServer.");
                byte[] verificationMessage = new byte[100];
                messageProcessor.PackMessage(
                            new Header(0, TerminalType.MatchingServer, LocalMatchingServerIDCode, TerminalType.ConfigServer, 0),
                            Command.MatchingServerIDVerify,
                            Status.None,
                            peerMatchingServerID,
                            "",
                            out verificationMessage);
                if (!connectionManager.SendMessageToConfigServerSync(verificationMessage))
                {
                    logs.ReportError("ServerManager.VerifyNewMatchingServer: Verifying failed due to connection problem with ConfigServer. Shutting down connection with MS #" + peerMatchingServerID);
                    connectionManager.DisconnectMatchingServer(peerMatchingServerID);
                }
            });
        }


        /// <summary>
        /// Loop asynchronous receive calls on a new MatchingServer connection in order to receive messages from that MatchingServer.
        /// </summary>
        /// <param name="matchingServerID">A string identifier for the MatchingServer.</param>
        async Task MatchingServerLoop(string matchingServerID)
        {
            byte[] message = new byte[100];
            if (!(await connectionManager.ReceiveMessageFromMatchingServerAsync(matchingServerID, message)))
            {
                // Error with connection - end loop
                logs.ReportError("ServerManager.MatchingServerLoop: Closed connection with MatchingServer #" + matchingServerID);
                return;
            }

            // Message Received
            Packet packet;
            if (!messageProcessor.UnPackMessage(message, out packet))
            {
                logs.ReportError("MatchingServerLoop: Unable to unpack message.");
            }
            logs.ReportPacket(packet);

            // Interpret message
            switch (packet.body.Cmd)
            {
                case Command.HealthCheckRequest:
                    logs.ReportMessage("MatchingServerLoop: Received Health Check Request from MatchingServer");
                    message = null;
                    messageProcessor.PackMessage(
                        new Header(0, TerminalType.MatchingServer, LocalMatchingServerIDCode, TerminalType.MatchingServer, int.Parse(matchingServerID)),
                        Command.HealthCheckResponse,
                        Status.None,
                        "",
                        "",
                        out message);
                    connectionManager.SendMessageToMatchingServerAsync(matchingServerID, message);
                    logs.ReportMessage("MatchingServerLoop: Sent Received Health Check Response to MatchingServer #" + matchingServerID);
                    break;

                case Command.MatchingServerIDTransmitResponse:
                    logs.ReportMessage("MatchingServerLoop: Received MatchingServerIDTransmitResponse from MatchingServer: ID: " + packet.header.srcCode + " Status: " + packet.body.status);
                    if (packet.body.status != Status.Success)
                    {
                        logs.ReportError("MatchingServerLoop: Peer MatchingServer #" + packet.body.Data1 + " denied verification. Closing connection. . .");
                        connectionManager.DisconnectMatchingServer(packet.body.Data1);
                    }
                    break;

                default:
                    break;
            }

            // Wait for next message
            Task.Run(() => MatchingServerLoop (matchingServerID));
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
