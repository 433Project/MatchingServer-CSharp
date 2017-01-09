using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

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
        //Fields
        private Logs logs;

        //Properties
        public bool IsInitialized { get; private set; } = false;

        //Sockets and Data structures
        Socket configServerSocket;
        Socket connectionServerSocket;
        ConcurrentDictionary<string, Socket> matchingServerSocketList;
        ConcurrentDictionary<string, Socket> clientSocketList;



        //###########################################
        //              Public Methods
        //###########################################

        /// <summary>
        /// Initializes the ConnectionManager by creating a logger and reading a configuration file.
        /// </summary>
        /// <returns>Returns true upon successful initialization and false otherwise.</returns>
        public bool Initialize ()
        {
            Debug.Assert(!IsInitialized, "ConnectionManager already initialized. Cannot initialize again.");

            logs = new Logs();
            logs.ReportMessage("ConnectionManager initializing. . .");

            IsInitialized = true;
            return true;
        }


        /// <summary>
        /// Creates a new connection of a specified type with a specified address.
        /// </summary>
        /// <param name="connectionType">The type of connection (ConfigServer, MatchingServer, ConnectionServer, Client).</param>
        /// <param name="address">The address of the peer to connect with.</param>
        /// <returns>The method returns true on success and false on failure.</returns>
        public bool CreateNewConnection(ConnectionType connectionType, IPEndPoint ipEndPoint)
        {
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call CreateNewConnection.");

            // 1. Create new socket
            Socket newSocket;
            if (!TryCreateTCPSocket(out newSocket))
            {
                return false;
            }


            // 2. Connect with desired ip end point
            if (!ConnectSocketToAddress(newSocket, ipEndPoint))
            {
                newSocket.Close();
                return false;
            }


            // 3. Store socket according to connectionType
            switch (connectionType)
            {
                case ConnectionType.ConfigServer:
                    Debug.Assert(configServerSocket == null, "ConnectionManager.CreateNewConnection: Already has configured ConfigServer socket.");
                    configServerSocket = newSocket;
                    break;

                case ConnectionType.ConnectionServer:
                    Debug.Assert(connectionServerSocket == null, "ConnectionManager.CreateNewConnection: Already has configured ConnectionServer socket.");
                    connectionServerSocket = newSocket;
                    break;

                case ConnectionType.MatchingServer:
                    //matchingServerSocketList.TryAdd(newSocket)



                    break;

                case ConnectionType.Client:




                    break;

                default:
                    logs.ReportError("CreateNewConnection: Invalid ConnectionType specified.");
                    newSocket.Close();
                    return false;

            }

            return true;
        }


        /// <summary>
        /// Creates a new connection with the connection server synchronously.
        /// </summary>
        /// <param name="address">The address of the peer to connect with.</param>
        /// <returns>The method returns true on success and false on failure.</returns>
        public bool ConnectWithConfigServerSync(IPEndPoint ipEndPoint)
        {
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call CreateNewConnection.");
            Debug.Assert(configServerSocket == null, "ConnectionManager.CreateNewConnection: Already has configured ConfigServer socket.");

            // 1. Create new socket
            Socket newSocket;
            if (!TryCreateTCPSocket (out newSocket))
            {
                return false;
            }


            // 2. Connect with desired ip end point
            if (!ConnectSocketToAddress(newSocket, ipEndPoint))
            {
                newSocket.Close();
                return false;
            }


            configServerSocket = newSocket;
            return true;
        }


        /// <summary>
        /// Creates a new connection with the connection server asynchronously as a background process.
        /// </summary>
        /// <param name="address">The address of the peer to connect with.</param>
        /// <returns>The method returns true on success and false on failure.</returns>
        async public Task<bool> ConnectWithConfigServerAsync (IPEndPoint ipEndPoint)
        {
            //Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call ConnectWithConfigServerAsync.");
            //Debug.Assert(configServerSocket == null, "ConnectionManager.ConnectWithConfigServerAsync: Already has configured ConfigServer socket.");

            // 1. Create new socket
            Socket newSocket;
            if (!TryCreateTCPSocket(out newSocket))
            {
                return false;
            }

            // 2. Connect with desired ip end point
            while (true)
            {
                int result = 0;
                await Task.Run(() =>
                {
                    try
                    {
                        newSocket.Connect(ipEndPoint);
                        result = 2;
                    }
                    catch (SocketException e)
                    {
                        logs.ReportError("CreateNewConnection: SocketException during Socket.Connect() - IP: " + ipEndPoint.Address.ToString() + "  Port: " + ipEndPoint.Port + "  Message: " + e.Message);
                        result = 1;
                    }
                    catch (Exception e)
                    {
                        logs.ReportError("CreateNewConnection: " + e.Message);
                        newSocket.Close();
                        result = 0;
                    }
                });

                switch (result)
                {
                    case 0:
                        return false;
                    case 1:
                        await Task.Delay(5000);
                        continue;
                    case 2:
                        configServerSocket = newSocket;
                        return true;
                    default:
                        return false;
                }
            }
        }


        /// <summary>
        /// To create listening sockets for a specified connection type.
        /// </summary>
        /// <param name="connectionType">The type of connection (ConfigServer, MatchingServer, ConnectionServer, Client).</param>
        /// <returns>The method returns true on success and false on failure to create and initialize the new listening socket.</returns>
        public bool CreateListeningSocket (ConnectionType connectionType)
        {
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call CreateListeningSocket.");

            return false;
        }


        /// <summary>
        /// Ask the connection server for an asynchronous accept call on particular listening socket of given type.
        /// </summary>
        /// <param name="connectionType">The type of connection (ConfigServer, MatchingServer, ConnectionServer, Client).</param>
        /// <param name="connectionID">An out parameter to retrieve the ID of the new connection.</param>
        public void AcceptNewConnection (ConnectionType connectionType, out string connectionID)
        {
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call AcceptNewConnection.");

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
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call SendMessage.");
            Debug.Assert(message.Length > 0, "Empty message sent to ConnectionManager. Cannot call SendMessage.");

            switch (connectionType)
            {
                case ConnectionType.ConfigServer:
                    return SendMessageToConfigServerSync(message);

                case ConnectionType.MatchingServer:
                    break;

                case ConnectionType.ConnectionServer:
                    break;

                case ConnectionType.Client:
                    break;

                default:
                    logs.ReportError("SendMessage: Invalid ConnectionType specified.");
                    return false;

            }

            return false;
        }


        /// <summary>
        /// This method sends a message synchronously to the configuration server.
        /// </summary>
        /// <param name="message">A byte[] of the message to be sent.</param>
        /// <returns>Returns true on a successful send or false on error.</returns>
        public bool SendMessageToConfigServerSync(byte[] message)
        {
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call SendMessageToConfigServerSync.");
            Debug.Assert(configServerSocket != null, "Cannot call SendMessageToConfigServerSync if configServerSocket is null!");
            Debug.Assert(message.Length >= 20, "Cannot call SendMessageToConfigServerSync if message is smaller than minimum header size!");

            try
            {
                int bytesSent = configServerSocket.Send(message);
                logs.ReportMessage("SendMessageToConfigServerSync: Sent " + bytesSent + " bytes");
            }
            catch (SocketException e)
            {
                logs.ReportError("SendMessageToConfigServerSync: SocketException during Socket.Send() - Message: " + e.Message);
                configServerSocket.Close();
                return false;
            }
            catch (Exception e)
            {
                logs.ReportError("SendMessageToConfigServerSync: Exception during Socket.Send() - Message: " + e.Message);
                configServerSocket.Close();
                return false;
            }

            return true;
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
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call ReceiveMessage.");
            Debug.Assert(receiveFromID != null, "ConnectionManager.ReceiveMessage: receiveFromID is null");

            message = null;




            return false;
        }


        /// <summary>
        /// This method receives a message synchronously from the configuration server.
        /// </summary>
        /// <param name="message">A byte[] out parameter of the message to be collected.</param>
        /// <returns>Returns true on a successful receive or false on error.</returns>
        public bool ReceiveMessageFromConfigServerSync(out byte[] message)
        {
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call ReceiveMessageFromConfigServerSync.");
            Debug.Assert(configServerSocket != null, "Cannot call ReceiveMessageFromConfigServerSync if configServerSocket is null!");

            message = new byte[100];

            try
            {
                int bytesReceived = configServerSocket.Receive(message);
                logs.ReportMessage("ReceiveMessageFromConfigServerSync: Received " + bytesReceived + " bytes");

                if (bytesReceived <= 0)
                {
                    logs.ReportError("ReceiveMessageFromConfigServerSync: Erroneous bytes received from ConfigServer; Closing Connection. . .");
                    configServerSocket.Close();
                    return false;
                }
            }
            catch (SocketException e)
            {
                logs.ReportError("ReceiveMessageFromConfigServerSync: SocketException during Socket.Receive() - Message: " + e.Message);
                configServerSocket.Close();
                return false;
            }
            catch (Exception e)
            {
                logs.ReportError("ReceiveMessageFromConfigServerSync: Exception during Socket.Receive() - Message: " + e.Message);
                configServerSocket.Close();
                return false;
            }

            return true;
        }


        /// <summary>
        /// This method receives a message asynchronously from the configuration server. If the receive resulted in a problem (bytes received), the socket is closed and false is returned.
        /// </summary>
        /// <param name="message">A byte[] parameter of the message to be collected.</param>
        /// <returns>Returns true on a successful receive or false on error.</returns>
        async public Task<bool> ReceiveMessageFromConfigServerAsync(byte[] message)
        {
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call ReceiveMessageFromConfigServerSync.");
            Debug.Assert(configServerSocket != null, "Cannot call ReceiveMessageFromConfigServerSync if configServerSocket is null!");

            //message = new byte[100];
            int bytesReceived = 0;

            bytesReceived = await Task.Run(() =>
            {
                try
                {
                    return configServerSocket.Receive(message);
                }
                catch (SocketException e)
                {
                    logs.ReportError("ReceiveMessageFromConfigServerAsync: SocketException during Socket.Receive() - Message: " + e.Message);
                    configServerSocket.Close();
                    return -2;
                }
                catch (Exception e)
                {

                    logs.ReportError("ReceiveMessageFromConfigServerAsync: Exception during Socket.Receive() - Message: " + e.Message);
                    configServerSocket.Close();
                    return -2;
                }
            });

            logs.ReportMessage("ReceiveMessageFromConfigServerAsync: Received " + bytesReceived + " bytes");

            if (bytesReceived <= 0)
            {
                logs.ReportError("ReceiveMessageFromConfigServerAsync: Erroneous bytes received from ConfigServer; Closing Connection. . .");
                configServerSocket.Close();
                return false;
            }

            return true;
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
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call BroadcastMessage.");

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
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call CheckConnectionHealth.");

            return false;
        }


        /// <summary>
        /// Tell the ConnectionManager to shut down a connection that is not needed anymore.
        /// </summary>
        /// <param name="connectionType">The type of connection (ConfigServer, MatchingServer, ConnectionServer, Client).</param>
        /// <param name="connectionID">ID of the peer to disconnect with. This ID should exist with the context of the connectionType.</param>
        public void Disconnect (ConnectionType connectionType, string connectionID)
        {
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call Disconnect.");

        }


        /// <summary>
        /// This method calls close on all sockets in use.
        /// </summary>
        public void CompleteShutdown ()
        {

        }



        //###########################################
        //              Private Methods
        //###########################################
        
        /// <summary>
        /// This method attempts to create a new socket configured for TCP and protects against socket creation failure.
        /// </summary>
        /// <param name="newSocket">A out parameter for the caller to receive the newly configured socket.</param>
        /// <returns>Returns true on successful socket creation and false on error.</returns>
        private bool TryCreateTCPSocket (out Socket newSocket)
        {
            newSocket = null;
            try
            {
                newSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (SocketException e)
            {
                logs.ReportError("TryCreateTCPSocket: SocketException during new Socket(...):" + e.Message);
                return false;
            }
            catch (Exception e)
            {
                logs.ReportError("TryCreateTCPSocket: " + e.Message);
                return false;
            }
            return true;
        }


        /// <summary>
        /// This method connects a socket to a given IP EndPoint. Upon error, it does not close the socket. The caller must chose how to react during the error condition.
        /// </summary>
        /// <param name="socket">The socket to use in the connection.</param>
        /// <param name="addressEndPoint">The end point of the peer to connect with.</param>
        /// <returns>Returns true on a successful connection and false on error.</returns>
        private bool ConnectSocketToAddress (Socket socket, IPEndPoint addressEndPoint)
        {
            Debug.Assert(socket != null, "ConnectSocketToAddress: socket cannot be null.");
            Debug.Assert(addressEndPoint != null, "ConnectSocketToAddress: addressEndPoint cannot be null.");

            try
            {
                socket.Connect(addressEndPoint);
            }
            catch (SocketException e)
            {
                logs.ReportError("ConnectSocketToAddress: SocketException during Socket.Connect() - IP: " + addressEndPoint.Address.ToString() + "  Port: " + addressEndPoint.Port + "  Message: " + e.Message);
                return false;
            }
            catch (Exception e)
            {
                logs.ReportError("ConnectSocketToAddress: " + e.Message);
                return false;
            }
            return true;
        }
    }
}
