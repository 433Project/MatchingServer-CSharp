using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
            PlayerIdentification identification;
            MatchState state;
            float metric;
        }

        struct Match
        {
            PlayerIdentification playerOne;
            PlayerIdentification playerTwo;
        }

        struct MatchingServer
        {
            OrderedDictionary playerQueue;      // <string, PlayerInfo>
            float latency;

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
        private ConnectionManager connectionManager;
        private Dictionary<string, MatchingServer> serverList = new Dictionary<string, MatchingServer>();

        //Properties
        public bool IsInitialized { get; private set; } = false;



        //###########################################
        //              Public Methods
        //###########################################


        //###########################################
        //              Private Methods
        //###########################################


    }
}
