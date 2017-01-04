﻿using System;
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
            IPEndPoint configServerEndPoint = null;
            if (!ConfigReader.GetIPEndPoint("ConfigServer", out configServerEndPoint))
            {
                logs.ReportError("ServerManager.Initialize: Cannot retrieve ConfigServer IPEndPoint");
                return false;
            }

            int i = 0;
            while (!connectionManager.CreateNewConnection(ConnectionType.ConfigServer, configServerEndPoint))
            {
                if (i++ == 1000)
                {
                    logs.ReportError("ServerManager.Initialize: Could not connect with ConfigServer");
                    return false;
                }
            }
            logs.ReportMessage("ServerManager.Initialize: Successfully connected to ConfigServer IP: " + configServerEndPoint.Address.ToString() + ":" + configServerEndPoint.Port);


            // 2. Register with ConfigServer
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
                logs.ReportError("SendMessageToConfigServerSync: Could not send message to ConfigServer");
            }
            logs.ReportMessage("SendMessageToConfigServerSync: Sent message to ConfigServer");

            if (!connectionManager.ReceiveMessageFromConfigServerSync(out message))
            {
                logs.ReportError("ReceiveMessageFromConfigServerSync: Could not receive message from ConfigServer");
            }
            logs.ReportMessage("ReceiveMessageFromConfigServerSync: Received message from ConfigServer");

            Packet packet;
            messageProcessor.UnPackMessage(message, out packet);
            logs.ReportMessage("Message Data: [Length] " + packet.header.length
                + " [SrcType] " + packet.header.srcType
                + " [SrcCode] " + packet.header.srcCode
                + " [DstType] " + packet.header.dstType
                + " [DstCode] " + packet.header.dstCode
                + " [Command] " + packet.body.Cmd
                + " [Status] " + packet.body.status
                + " [Data1] " + packet.body.Data1
                + " [Data2] " + packet.body.Data2
                );




            // 3. Create ConfigServer async service loop


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
        /// Loop asynchronous receive calls to the ConfigServer in order to receive messages from the ConfigServer.
        /// </summary>
        async private void StartConfigServerLoop ()
        {

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
