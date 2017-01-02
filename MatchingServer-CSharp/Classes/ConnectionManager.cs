using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchingServer_CSharp.Classes
{
    enum ConnectionType
    {
        None = 0,
        ConfigServer,
        MatchingServer,
        ConnectionServer,
        Client
    }


    class ConnectionManager
    {
        //###########################################
        //             Fields/Properties
        //###########################################
        private Logs logs;

        //Properties
        public bool IsInitialized { get; private set; } = false;


        //###########################################
        //              Public Methods
        //###########################################

        /// <summary>
        /// Creates a new connection of a specified type with a specified address.
        /// </summary>
        /// <param name="connectionType">The type of connection (ConfigServer, MatchingServer, ConnectionServer, Client).</param>
        /// <param name="address">The address of the peer to connect with.</param>
        /// <returns>The method returns true on success and false on failure.</returns>
        public bool CreateNewConnection(ConnectionType connectionType, IPAddress address)
        {
            return false;
        }


        /// <summary>
        /// To create listening sockets for a specified connection type.
        /// </summary>
        /// <param name="connectionType">The type of connection (ConfigServer, MatchingServer, ConnectionServer, Client).</param>
        /// <returns>The method returns true on success and false on failure to create and initialize the new listening socket.</returns>
        public bool CreateListeningSocket (ConnectionType connectionType)
        {
            return false;
        }


        /// <summary>
        /// Ask the connection server for an asynchronous accept call on particular listening socket of given type.
        /// </summary>
        /// <param name="connectionType">The type of connection (ConfigServer, MatchingServer, ConnectionServer, Client).</param>
        /// <param name="connectionID">An out parameter to retrieve the ID of the new connection.</param>
        public void AcceptNewConnection (ConnectionType connectionType, out string connectionID)
        {
            connectionID = "";
        }


        /// <summary>
        /// Takes the sendToID, looks up the socket and sends the message to that socket.
        /// </summary>
        /// <param name="connectionType">The type of connection (ConfigServer, MatchingServer, ConnectionServer, Client).</param>
        /// <param name="sendToID">ID of the peer to send to. This ID should exist with the context of the connectionType.</param>
        /// <param name="message">A byte array carrying the message.</param>
        /// <returns>The method returns true on success and false on failure.</returns>
        public bool SendMessage (ConnectionType connectionType, string sendToID, byte[] message)
        {
            return false;
        }


        /// <summary>
        /// Looks up the socket from the ID and awaits the receipt of a message on that socket. 
        /// The message is passed as an out parameter. Failure informs the caller that the connection with the ID is no longer viable.
        /// </summary>
        /// <param name="connectionType">The type of connection (ConfigServer, MatchingServer, ConnectionServer, Client).</param>
        /// <param name="receiveFromID">ID of the peer to receive from. This ID should exist with the context of the connectionType.</param>
        /// <param name="message">An out parameter byte array to store the received message.</param>
        /// <returns>The method returns true on success and false on failure.</returns>
        public bool ReceiveMessage (ConnectionType connectionType, string receiveFromID, out byte[] message)
        {
            message = null;
            return false;
        }


        /// <summary>
        /// This message sends messages to all connections on a particular ConnectionType. 
        /// The expected usage is to broadcast to all MatchingServers.
        /// </summary>
        /// <param name="connectionType">The type of connection (ConfigServer, MatchingServer, ConnectionServer, Client).</param>
        /// <param name="message">A byte array carrying the message.</param>
        /// <returns>The method returns true on success and false on failure.</returns>
        public bool BroadcastMessage (ConnectionType connectionType, byte[] message)
        {
            return false;
        }


        /// <summary>
        /// To check if a connection is still viable via a health check mechanism.
        /// </summary>
        /// <param name="connectionType">The type of connection (ConfigServer, MatchingServer, ConnectionServer, Client).</param>
        /// <param name="connectionID">ID of the peer to check health. This ID should exist with the context of the connectionType.</param>
        /// <returns>The method returns true if the connection is healthy and false the connection is unresponsive.</returns>
        public bool CheckConnectionHealth (ConnectionType connectionType, string connectionID)
        {
            return false;
        }


        /// <summary>
        /// Tell the ConnectionManager to shut down a connection that is not needed anymore.
        /// </summary>
        /// <param name="connectionType">The type of connection (ConfigServer, MatchingServer, ConnectionServer, Client).</param>
        /// <param name="connectionID">ID of the peer to disconnect with. This ID should exist with the context of the connectionType.</param>
        public void Disconnect (ConnectionType connectionType, string connectionID)
        {

        }

        //###########################################
        //              Private Methods
        //###########################################
    }
}
