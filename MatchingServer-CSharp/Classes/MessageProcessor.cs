using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Protocol;
using FlatBuffers;
using fb;

namespace MatchingServer_CSharp.Classes
{
    class MessageProcessor
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
        /// This method initializes a message processor instance by creating it's logging instance. This method should only be called once.
        /// </summary>
        public void Initialize ()
        {
            Debug.Assert(!IsInitialized, "MessageProcessor already initialized. Cannot initialize again.");

            logs = new Logs();

            IsInitialized = true;
        }


        public bool PackMessage (Header header, Command command, Status status, string data1, string data2, out byte[] packet)
        {
            Debug.Assert(IsInitialized, "MessageProcessor is not initialized. Cannot call PackMessage.");

            packet = null;

            // 1. Build body with FlatBuffer
            var messageBuilder = new FlatBufferBuilder(100);
            StringOffset data1Converted = messageBuilder.CreateString(data1);
            StringOffset data2Converted = messageBuilder.CreateString(data2);
            var body = Body.CreateBody(messageBuilder, command, status, data1Converted, data2Converted);
            messageBuilder.Finish(body.Value);
            byte[] message = messageBuilder.SizedByteArray();

            // 2. Update header's length value and then convert to bytes
            header.length = message.Length;
            byte[] headerBytes = StructureToByte(header);

            // 3. Combine into a packet
            packet = new byte[headerBytes.Length + message.Length];
            Array.Copy(headerBytes, packet, headerBytes.Length);
            Array.Copy(message, 0, packet, headerBytes.Length, message.Length);

            return true;
        }


        public bool UnPackMessage (byte[] message, out Packet packet)
        {
            Debug.Assert(IsInitialized, "MessageProcessor is not initialized. Cannot call UnPackMessage.");
            Debug.Assert(message != null, "Cannot call UnPackMessage if message is null!");
            Debug.Assert(message.Length >= 20, "Cannot call UnPackMessage if message is smaller than minimum header size!");

            packet = new Packet ();
            int headerSize = Marshal.SizeOf(packet.header);

            byte[] header = new byte[headerSize];
            Array.Copy(message, header, headerSize);
            packet.header = (Header)ByteToStructure(header, typeof(Header));

            byte[] body = new byte[packet.header.length];
            Array.Copy(message, headerSize, body, 0, packet.header.length);

            packet.body = Body.GetRootAsBody(new ByteBuffer(body));

            return true;
        }



        //###########################################
        //              Private Methods
        //###########################################


        /// <summary>
        /// This method is used by the MessageProcessor to convert a byte array containing a Header struct into a Header struct.
        /// </summary>
        /// <param name="data">A byte array containing the data.</param>
        /// <param name="type">The type of struct to convert to.</param>
        /// <returns>An object of Type type or null if the data was empty.</returns>
        private object ByteToStructure(byte[] data, Type type)
        {
            Debug.Assert(data.Length == Marshal.SizeOf(type), "Data passed to ByteToStructure of incorrect size.");

            IntPtr buff = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, buff, data.Length);
            object obj = Marshal.PtrToStructure(buff, type);
            Marshal.FreeHGlobal(buff);

            if (Marshal.SizeOf(obj) != data.Length)
            {
                return null;
            }

            return obj;
        }


        /// <summary>
        /// This method is used by the MessageProcessor to convert a Header struct to a byte array.
        /// </summary>
        /// <param name="obj">The object to be converted to byte form.</param>
        /// <returns>Returns the byte[] of the object.</returns>
        private byte[] StructureToByte(object obj)
        {
            Debug.Assert(obj != null, "Object passed to StructureToByte is null.");

            int datasize = Marshal.SizeOf(obj);
            IntPtr buff = Marshal.AllocHGlobal(datasize);
            Marshal.StructureToPtr(obj, buff, false);
            byte[] data = new byte[datasize];
            Marshal.Copy(buff, data, 0, datasize);
            Marshal.FreeHGlobal(buff);
            return data;
        }
    }
}
