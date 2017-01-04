using fb;

namespace Protocol
{
    struct Packet
    {
        public Header header;
        public Body body;
    }

    struct Header
    {
        public int length;
        public TerminalType srcType;    
        public int srcCode;   
        public TerminalType dstType;    
        public int dstCode;
        public Header(int length, TerminalType srcType, int srcCode, TerminalType dstType, int dstCode)
        {
            this.length = length;
            this.srcType = srcType;
            this.srcCode = srcCode;
            this.dstType = dstType;
            this.dstCode = dstCode;
        }    
    }

    enum TerminalType : int
    {
        None = 0,
        MatchingServer = 1,
        MatchingClient,
        RoomServer,
        PacketGenerator,
        MonitoringServer,
        ConfigServer,
        ConnectionServer
    }
}
