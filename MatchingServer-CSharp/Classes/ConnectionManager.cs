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
        public int MatchingServerPort { get; private set; } = 9876;

        //Sockets and Data structures
        Socket configServerSocket;
        Socket matchingServerListeningSocket;
        Socket connectionServerSocket;
        ConcurrentDictionary<string, Socket> matchingServerSocketList;
        ConcurrentDictionary<string, Socket> clientSocketList;

        //Private enums
        private enum ConnectionResultType
        {
            Undefined = 0,
            SocketException = 1,
            Success = 2,
            NonSocketException = 3
        }



        //###########################################
        //              Public Methods
        //###########################################
        //              General Methods
        //###########################################

        /// <summary>
        /// Initializes the ConnectionManager by creating a logger and reading a configuration file.
        /// </summary>
        /// <returns>Returns true upon successful initialization and false otherwise.</returns>
        public bool Initialize ()
        {
            Debug.Assert(!IsInitialized, "ConnectionManager already initialized. Cannot initialize again.");

            // 1. Create Logs instance
            logs = new Logs();
            logs.ReportMessage("ConnectionManager initializing. . .");


            // 2. Get Matching Server port number and store
            int portNumber;
            if (!ConfigReader.GetPort("MatchingServers", out portNumber))
            {
                return false;
            }
            MatchingServerPort = portNumber;


            // 3. Initialize the MatchingServerList
            matchingServerSocketList = new ConcurrentDictionary<string, Socket>();
            clientSocketList = new ConcurrentDictionary<string, Socket>();

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



        //###########################################
        //           ConfigServer Methods
        //###########################################

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
                ConnectionResultType result = ConnectionResultType.Undefined;
                await Task.Run(() =>
                {
                    try
                    {
                        newSocket.Connect(ipEndPoint);
                        result = ConnectionResultType.Success;
                    }
                    catch (SocketException e)
                    {
                        logs.ReportError("ConnectWithConfigServerAsync: SocketException during Socket.Connect() - IP: " + ipEndPoint.Address.ToString() + "  Port: " + ipEndPoint.Port + "  Message: " + e.Message);
                        result = ConnectionResultType.SocketException;
                    }
                    catch (Exception e)
                    {
                        logs.ReportError("ConnectWithConfigServerAsync: " + e.Message);
                        newSocket.Close();
                        result = ConnectionResultType.NonSocketException;
                    }
                });

                switch (result)
                {
                    case ConnectionResultType.NonSocketException:
                        return false;
                    case ConnectionResultType.SocketException:
                        await Task.Delay(5000);
                        continue;
                    case ConnectionResultType.Success:
                        configServerSocket = newSocket;
                        return true;
                    default:
                        return false;
                }
            }
        }


        /// <summary>
        /// This method sends a message synchronously to the configuration server. Upon error, closes the socket with the ConfigServer.
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
                    return -2;
                }
                catch (Exception e)
                {

                    logs.ReportError("ReceiveMessageFromConfigServerAsync: Exception during Socket.Receive() - Message: " + e.Message);
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



        //###########################################
        //          MatchingServer Methods
        //###########################################

        /// <summary>
        /// Creates a new connection with the connection server asynchronously as a background process.
        /// </summary>
        /// <param name="address">The address of the peer to connect with.</param>
        /// <returns>The method returns true on success and false on failure.</returns>
        async public Task<bool> ConnectWithMatchingServerAsync(string matchingServerID, string ip)
        {
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call ConnectWithMatchingServerAsync.");
            Debug.Assert(matchingServerID.Length >= 0, "ConnectionManager.ConnectWithMatchingServerAsync: passed matchingServerID string is empty.");
            Debug.Assert(ip.Length >= 0, "ConnectionManager.ConnectWithMatchingServerAsync: passed ip string is empty.");


            // 1. Parse ip address and calculate port
            IPAddress address;
            if (!IPAddress.TryParse(ip, out address))
            {
                logs.ReportError("ConnectWithMatchingServerAsync: Could not parse passed ip string.");
                return false;
            }
            int port;
            if (!int.TryParse(matchingServerID, out port))
            {
                logs.ReportError("ConnectWithMatchingServerAsync: Could not parse passed matchingServerID string.");
                return false;
            }
            port += MatchingServerPort;
            logs.ReportMessage("ConnectWithMatchingServerAsync: Connecting with MatchingServer (ID = " + matchingServerID + " IP = " + ip + ":" + port + "). . .");


            // 2. Create new socket
            Socket newSocket;
            if (!TryCreateTCPSocket(out newSocket))
            {
                return false;
            }
            

            // 3. Connect with desired ip end point
            ConnectionResultType result = ConnectionResultType.Undefined;
            await Task.Run(() =>
            {
                try
                {
                    newSocket.Connect(new IPEndPoint(address, port));
                    result = ConnectionResultType.Success;
                }
                catch (SocketException e)
                {
                    logs.ReportError("ConnectWithMatchingServerAsync: SocketException during Socket.Connect() - IP: " + address.ToString() + "  Port: " + MatchingServerPort + " Message: " + e.Message);
                    result = ConnectionResultType.SocketException;
                }
                catch (Exception e)
                {
                    logs.ReportError("ConnectWithMatchingServerAsync: " + e.Message);
                    result = ConnectionResultType.NonSocketException;
                }
            });

            if (result != ConnectionResultType.Success)
            {
                newSocket.Close();
                return false;
            }

            
            // 4. Check if the MS ID is already registered; if not, add it
            if (!matchingServerSocketList.TryAdd(matchingServerID, newSocket))
            {
                logs.ReportError("ConnectWithMatchingServerAsync: peer MatchingServer ID already registered in the matchingServerSocketList. Cannot attempt connection.");
                return false;
            }


            logs.ReportMessage("ServerManager.ConnectWithMatchingServerAsync: Connecting with MatchingServer (ID = " + matchingServerID + " IP = " + ip + ") successful!");
            return true;
        }


        /// <summary>
        /// This method creates/initializes the listening socket for accepting new MatchingServer connections.
        /// </summary>
        /// <returns>Returns true on successful creation and false on error.</returns>
        public bool CreateMatchingServerListeningSocket (string localMatchingServerID)
        {
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call CreateMatchingServerListeningSocket.");
            Debug.Assert(matchingServerListeningSocket == null, "CreateMatchingServerListeningSocket: matchingServerListeningSocket already initialized.");

            // 1. Parse ID value for port calculation
            int port;
            if (!int.TryParse(localMatchingServerID, out port))
            {
                logs.ReportError("CreateMatchingServerListeningSocket: Could not parse passed matchingServerID string.");
                return false;
            }
            port += MatchingServerPort;
            logs.ReportMessage("CreateMatchingServerListeningSocket: Local MatchingServer port set to: " + port + ".");


            // 2. Create new socket
            Socket newSocket;
            if (!TryCreateTCPSocket(out newSocket))
            {
                return false;
            }


            // 3. Bind and Listen
            if (!TrySocketListen(newSocket, port))
            {
                return false;
            }
            matchingServerListeningSocket = newSocket;


            return true;
        }


        /// <summary>
        /// This method accepts a connection from a new MS peer asynchronously and then asynchronously waits for the first message to arrive. 
        /// On successful arrival of a message, the ConnectionManager calls ServerManager.ExtractMatchingServerID in order to get the srcCode (ID) 
        /// for the new connection.
        /// </summary>
        /// <param name="firstMessage">A byte[] to hold the first message.</param>
        /// <returns>Returns true on a successful accept, receive, and registration or false on error.</returns>
        public async Task<bool> AcceptMatchingServerConnectionAsync (byte[] firstMessage)
        {
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call AcceptMatchingServerConnectionAsync.");
            Debug.Assert(matchingServerListeningSocket != null, "AcceptMatchingServerConnectionAsync: matchingServerListeningSocket not initialized.");

            
            // 1. Get the newly accepted socket
            ConnectionResultType result = ConnectionResultType.Undefined;
            Socket acceptedSocket = await Task.Run(() =>
           {
               try
               {
                   acceptedSocket = matchingServerListeningSocket.Accept();
                   result = ConnectionResultType.Success;
                   return acceptedSocket;
               }
               catch (SocketException e)
               {
                   logs.ReportError("AcceptMatchingServerConnectionAsync: SocketException during Socket.Accept(): " + e.Message);
                   result = ConnectionResultType.SocketException;
                   return null;
               }
               catch (Exception e)
               {
                   logs.ReportError("AcceptMatchingServerConnectionAsync: " + e.Message);
                   result = ConnectionResultType.NonSocketException;
                   return null;
               }
           });

            if (result != ConnectionResultType.Success)
            {
                return false;
            }


            // 2. Get the first message
            int bytesReceived = 0;

            bytesReceived = await Task.Run(() =>
            {
                try
                {
                    return acceptedSocket.Receive(firstMessage);
                }
                catch (SocketException e)
                {
                    logs.ReportError("AcceptMatchingServerConnectionAsync: SocketException during Socket.Receive() - Message: " + e.Message);
                    return -2;
                }
                catch (Exception e)
                {

                    logs.ReportError("AcceptMatchingServerConnectionAsync: Exception during Socket.Receive() - Message: " + e.Message);
                    return -2;
                }
            });

            logs.ReportMessage("AcceptMatchingServerConnectionAsync: Received " + bytesReceived + " bytes");

            if (bytesReceived <= 0)
            {
                logs.ReportError("AcceptMatchingServerConnectionAsync: Erroneous bytes received from ConfigServer; Removing MS Entry and closing Connection. . .");
                acceptedSocket.Close();
                return false;
            }


            // 3. Register the new socket
            string matchingServerID = ServerManager.ExtractMatchingServerID(firstMessage);
            Debug.Assert(matchingServerID != null, "AcceptMatchingServerConnectionAsync: Should not of received null matchingServerID.");

            if (!matchingServerSocketList.TryAdd(matchingServerID, acceptedSocket))
            {
                logs.ReportError("AcceptMatchingServerConnectionAsync: Could not add received matching server ID to socket list. . .");
                acceptedSocket.Close();
                return false;
            }

            return true;
        }


        /// <summary>
        /// This method sends a message synchronously to the configuration server. Upon error, closes the socket with the ConfigServer.
        /// </summary>
        /// <param name="message">A byte[] of the message to be sent.</param>
        /// <returns>Returns true on a successful send or false on error.</returns>
        async public Task<bool> SendMessageToMatchingServerAsync(string matchingServerID, byte[] message)
        {
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call SendMessageToConfigServerSync.");
            Debug.Assert(matchingServerID != null, "Cannot call SendMessageToMatchingServerAsync if matchingServerID is null!");
            Debug.Assert(message.Length >= 20, "Cannot call SendMessageToMatchingServerAsync if message is smaller than minimum header size!");

            Socket matchingServerSocket;

            if (!matchingServerSocketList.TryGetValue(matchingServerID, out matchingServerSocket))
            {
                logs.ReportError("SendMessageToMatchingServerAsync: Cannot find MS ID in socketList: " + matchingServerID);
                return false;
            }

            if(!await Task.Run(() =>
            {
                try
                {
                    int bytesSent = matchingServerSocket.Send(message);
                    logs.ReportMessage("SendMessageToMatchingServerAsync: Sent " + bytesSent + " bytes to MS #" + matchingServerID);
                }
                catch (SocketException e)
                {
                    logs.ReportError("SendMessageToMatchingServerAsync: SocketException during Socket.Send() - Message: " + e.Message);
                    return false;
                }
                catch (Exception e)
                {
                    logs.ReportError("SendMessageToMatchingServerAsync: Exception during Socket.Send() - Message: " + e.Message);
                    return false;
                }
                return true;
            }))
            {
                matchingServerSocketList.TryRemove(matchingServerID, out matchingServerSocket);
                if (matchingServerSocket != null)
                {
                    matchingServerSocket.Close();
                }
            }

            return true;
        }


        /// <summary>
        /// This method receives a message asynchronously from a specified MatchingServer. If the receive resulted in a problem (bytes received), the socket is closed and false is returned.
        /// </summary>
        /// <param name="message">A byte[] parameter of the message to be collected.</param>
        /// <returns>Returns true on a successful receive or false on error.</returns>
        async public Task<bool> ReceiveMessageFromMatchingServerAsync(string matchingServerID, byte[] message)
        {
            Debug.Assert(IsInitialized, "ConnectionManager not initialized. Cannot call ReceiveMessageFromConfigServerSync.");
            Debug.Assert(matchingServerID != null, "Cannot call SendMessageToMatchingServerAsync if matchingServerID is null!");

            Socket matchingServerSocket;

            if (!matchingServerSocketList.TryGetValue(matchingServerID, out matchingServerSocket))
            {
                logs.ReportError("ReceiveMessageFromMatchingServerAsync: Cannot find MS ID in socketList: " + matchingServerID);
                return false;
            }

            //message = new byte[100];
            int bytesReceived = 0;

            bytesReceived = await Task.Run(() =>
            {
                try
                {
                    return matchingServerSocket.Receive(message);
                }
                catch (SocketException e)
                {
                    logs.ReportError("ReceiveMessageFromMatchingServerAsync: SocketException during Socket.Receive() - Message: " + e.Message);
                    return -2;
                }
                catch (Exception e)
                {

                    logs.ReportError("ReceiveMessageFromMatchingServerAsync: Exception during Socket.Receive() - Message: " + e.Message);
                    return -2;
                }
            });

            logs.ReportMessage("ReceiveMessageFromMatchingServerAsync: Received " + bytesReceived + " bytes");

            if (bytesReceived <= 0)
            {
                logs.ReportError("ReceiveMessageFromMatchingServerAsync: Erroneous bytes received from ConfigServer; Removing MS Entry and closing Connection. . .");
                matchingServerSocketList.TryRemove(matchingServerID, out matchingServerSocket);
                if (matchingServerSocket != null)
                {
                    matchingServerSocket.Close();
                }
                return false;
            }

            return true;
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
        /// This method attempts to bind a socket to a given port and initiate listening on the socket.
        /// </summary>
        /// <param name="socket">The socket to configure.</param>
        /// <param name="port">The port to bind to.</param>
        /// <returns>Returns true on successful socket binding and listen initialization and false on error.</returns>
        private bool TrySocketListen(Socket socket, int port)
        {
            Debug.Assert(socket != null, "TrySocketListen: socket cannot be null.");
            Debug.Assert(port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort, "TrySocketListen: port value is out of bounds.");

            try
            {
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
                socket.Listen(10);
            }
            catch (SocketException e)
            {
                logs.ReportError("TrySocketListen: SocketException during Socket.Bind or Socket.Listen(...):" + e.Message);
                return false;
            }
            catch (Exception e)
            {
                logs.ReportError("TrySocketListen: " + e.Message);
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
