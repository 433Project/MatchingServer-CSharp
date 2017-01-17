using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchingServer_CSharp.Classes
{
    class WaitingRoom
    {
        struct PlayerIdentification
        {
            string matchingServerID;
            string playerID;
        }

        enum MatchState
        {
            UnSet = 0,
            UnMatched = 1,
            MatchedButUnconfirmed,
            ConfirmedWithServer
        }

        struct PlayerInfo
        {
            public PlayerIdentification identification;
            public MatchState state;
            public float metric;
        }

        struct Match
        {
            PlayerIdentification playerOne;
            PlayerIdentification playerTwo;
        }

        struct MatchingServer
        {
            public OrderedDictionary playerQueue;      // <string, PlayerInfo>
            public float latency;

            public MatchingServer (float latencyValue)
            {
                playerQueue = new OrderedDictionary();
                latency = latencyValue;
            }
        }


        //###########################################
        //             Fields/Properties
        //###########################################
        private Logs logs;
        private Dictionary<int, MatchingServer> serverList = new Dictionary<int, MatchingServer>();
        private readonly object _locker = new object();

        //Properties
        public bool IsInitialized { get; private set; } = false;
        public int LocalServerIDCode { get; private set; } = -1;



        //###########################################
        //              Public Methods
        //###########################################

        
        /// <summary>
        /// This method initializes the waiting room by creating a log instance, setting the local server ID code, and creating the local server waiting queue.
        /// </summary>
        /// <param name="localServerIDCode">An integer value that holds the server's ID.</param>
        public void Initialize (int localServerIDCode)
        {
            Debug.Assert(!IsInitialized, "WaitingRoom already initialized. Cannot initialize again.");
            Debug.Assert(localServerIDCode > 0, "Invalid localServerIDCode passed to WaitingRoom. Must be greater than 0.");

            // 1. Create Logs instance
            logs = new Logs();
            logs.ReportMessage("WaitingRoom initializing. . .");


            // 2. Assign local server ID code
            LocalServerIDCode = localServerIDCode;


            // 3. Create new MatchingServer (waiting queue)
            MatchingServer localServer = new MatchingServer(0f);
            serverList.Add(localServerIDCode, localServer);

            IsInitialized = true;
        }


        /// <summary>
        /// This method adds a MS to the WaitingRoom, creating a new MatchingServer structure to be placed in the serverList.
        /// </summary>
        /// <param name="peerServerIDCode">An integer value that holds the peer server's ID.</param>
        /// <param name="initialLatency">A float value the contains the peer server's initial latency value.</param>
        public void AddMStoWaitingRoom (int peerServerIDCode, float initialLatency)
        {
            Debug.Assert(IsInitialized, "WaitingRoom not initialized. Cannot call AddMStoWaitingRoom.");
            Debug.Assert(peerServerIDCode > 0, "Invalid peerServerIDCode passed to WaitingRoom. Must be greater than 0.");
            Debug.Assert(initialLatency >= 0, "Invalid initialLatency passed to WaitingRoom. Must be greater than or equal to 0.");

            logs.ReportMessage("WaitingRoom.AddMStoWaitingRoom: ENTERING LOCK");
            lock (_locker)
            {
                MatchingServer peerServer = new MatchingServer(initialLatency);
                serverList.Add(peerServerIDCode, peerServer);
            }
            logs.ReportMessage("WaitingRoom.AddMStoWaitingRoom: EXITING LOCK");

#if DEBUG
            PrintWaitingRoomDiagnostic();
#endif
        }


        /// <summary>
        /// This method removes a MS entry from the WaitingRoom, including all waiting players if present.
        /// </summary>
        /// <param name="peerServerIDCode">An integer value that holds the peer server's ID.</param>
        public void RemovePeerMSfromWaitingRoom (int peerServerIDCode)
        {
            Debug.Assert(IsInitialized, "WaitingRoom not initialized. Cannot call RemovePeerMSfromWaitingRoom.");
            Debug.Assert(peerServerIDCode > 0, "Invalid peerServerIDCode passed to WaitingRoom. Must be greater than 0.");

            logs.ReportMessage("WaitingRoom.RemovePeerMSfromWaitingRoom: ENTERING LOCK");
            lock (_locker)
            {
                MatchingServer tempMatchingServer;
                if (serverList.TryGetValue(peerServerIDCode, out tempMatchingServer))
                {
                    tempMatchingServer.playerQueue.Clear();
                }
                serverList.Remove(peerServerIDCode);
            }
            logs.ReportMessage("WaitingRoom.RemovePeerMSfromWaitingRoom: EXITING LOCK");

#if DEBUG
            PrintWaitingRoomDiagnostic();
#endif
        }


        /// <summary>
        /// This method removes the local waiting room and releases the MatchingServer's players. 
        /// This method should only be used during server shutdown or other problems in which the MS cannot continue matching.
        /// </summary>
        public void RemoveLocalMSfromWaitingRoom ()
        {
            Debug.Assert(IsInitialized, "WaitingRoom not initialized. Cannot call RemoveLocalMSfromWaitingRoom.");
            Debug.Assert(LocalServerIDCode > 0, "Invalid stored LocalServerIDCode value when tested by RemoveLocalMSfromWaitingRoom. Must be greater than 0.");
            Debug.Assert(serverList.ContainsKey(LocalServerIDCode), "Cannot call . serverList does not contain stored value for LocalServerIDCode.");

            logs.ReportMessage("WaitingRoom.RemoveLocalMSfromWaitingRoom: ENTERING LOCK");
            lock (_locker)
            {
                MatchingServer tempMatchingServer;
                if (serverList.TryGetValue(LocalServerIDCode, out tempMatchingServer))
                {
                    tempMatchingServer.playerQueue.Clear();
                }
                serverList.Remove(LocalServerIDCode);
            }
            logs.ReportMessage("WaitingRoom.RemoveLocalMSfromWaitingRoom: EXITING LOCK");

#if DEBUG
            PrintWaitingRoomDiagnostic();
#endif
        }


        /// <summary>
        /// This method prints the entire serverList data to log. It's intended to be used for debug purposes only.
        /// </summary>
        public void PrintWaitingRoomDiagnostic ()
        {
            Debug.Assert(IsInitialized, "WaitingRoom not initialized. Cannot call PrintWaitingRoomDiagnostic.");

            logs.ReportMessage("WaitingRoom.PrintWaitingRoomDiagnostic: ENTERING LOCK");
            lock (_locker)
            {
                logs.ReportMessage("#### serverList contains [" + serverList.Count + "] entries.");
                int i = 1, j = 0;
                foreach (KeyValuePair<int, MatchingServer> pair in serverList)
                {
                    logs.ReportMessage("####   Server [" + i++ + "]:\t ServerCode [" + pair.Key + "]\t Entries [" + pair.Value.playerQueue.Count + "]");
                    OrderedDictionary tempMatchingServer = pair.Value.playerQueue;
                    j = 1;
                    foreach (KeyValuePair<object, object> player in tempMatchingServer)
                    {
                        PlayerInfo tempPlayerInfo = (PlayerInfo)player.Value;
                        logs.ReportMessage("####      Player [" + j++ + "]:\t PlayerCode [" + player.Key.ToString() + "]\t Metric [" + tempPlayerInfo.metric + "]\t Status [" + tempPlayerInfo.state + "]");
                    }
                }
            }
            logs.ReportMessage("WaitingRoom.PrintWaitingRoomDiagnostic: EXITING LOCK");
        }



        //###########################################
        //              Private Methods
        //###########################################


    }
}
