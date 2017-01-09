using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;
using System.Xml;
using System.IO;

namespace MatchingServer_CSharp.Classes
{
    class ConfigReader
    {
        //###########################################
        //             Fields/Properties
        //###########################################
        private static Logs logs;

        //Properties
        public bool IsInitialized { get; private set; } = false;



        //###########################################
        //              Public Methods
        //###########################################

        /// <summary>
        /// This method initializes the ConfigReader class by creating it's Logs class instance.
        /// </summary>
        public static void Initialize ()
        {
            logs = new Logs();
            logs.ReportMessage("ConfigReader initializing. . .");
        }


        /// <summary>
        /// This method retrieves the IPEndPoint of a connection entry in the network.xml configuration file.
        /// </summary>
        /// <param name="xmlNodeName">The name of the connection node (ConfigServer, ConnectionServer).</param>
        /// <param name="endPoint">An out parameter that holds the IP endpoint contained in the configuration file.</param>
        /// <returns>Returns true upon a successful read and false if the data could not be obtained.</returns>
        public static bool GetIPEndPoint (string xmlNodeName, out IPEndPoint endPoint)
        {
            endPoint = null;
            XmlTextReader xmlTextReader = null;

            try
            {
                xmlTextReader = new XmlTextReader("network.xml");
                
                if (!xmlTextReader.ReadToFollowing(xmlNodeName))
                {
                    logs.ReportError("GetIPEndPoint cannot find node <" + xmlNodeName + ">");
                    return false;
                }
                if (!xmlTextReader.ReadToDescendant("ip"))
                {
                    logs.ReportError("GetIPEndPoint cannot find child node <ip> within node <" + xmlNodeName + ">");
                    return false;
                }
                IPAddress address;
                xmlTextReader.Read();
                if (!IPAddress.TryParse (xmlTextReader.Value, out address))
                {
                    logs.ReportError("GetIPEndPoint cannot parse ip value within node <" + xmlNodeName + ">");
                    return false;
                }
                if (!xmlTextReader.ReadToFollowing("port"))
                {
                    logs.ReportError("GetIPEndPoint cannot find child node <port> within node <" + xmlNodeName + ">");
                    return false;
                }
                int port;
                xmlTextReader.Read();
                if (!int.TryParse(xmlTextReader.Value, out port))
                {
                    logs.ReportError("GetIPEndPoint cannot parse port value within node <" + xmlNodeName + ">");
                    return false;
                }

                endPoint = new IPEndPoint(address, port);
            }
            catch (FileNotFoundException e)
            {
                logs.ReportError("GetIPEndPoint cannot find XML-file ...\\network.xml");
                return false;
            }
            catch (XmlException e)
            {
                logs.ReportError("GetIPEndPoint: " + e.Message);
                return false;
            }
            catch (Exception e)
            {
                logs.ReportError("GetIPEndPoint: " + e.Message);
                return false;
            }

            return true;
        }


        public static bool GetPort (string xmlNodeName, out int portNumber)
        {
            portNumber = 8765;
            XmlTextReader xmlTextReader = null;

            try
            {
                xmlTextReader = new XmlTextReader("network.xml");

                if (!xmlTextReader.ReadToFollowing(xmlNodeName))
                {
                    logs.ReportError("GetIPEndPoint cannot find node <" + xmlNodeName + ">");
                    return false;
                }
                if (!xmlTextReader.ReadToDescendant("port"))
                {
                    logs.ReportError("GetIPEndPoint cannot find child node <ip> within node <" + xmlNodeName + ">");
                    return false;
                }
                xmlTextReader.Read();
                if (!int.TryParse(xmlTextReader.Value, out portNumber))
                {
                    logs.ReportError("GetIPEndPoint cannot parse port value within node <" + xmlNodeName + ">");
                    return false;
                }
            }
            catch (FileNotFoundException e)
            {
                logs.ReportError("GetIPEndPoint cannot find XML-file ...\\network.xml");
                return false;
            }
            catch (XmlException e)
            {
                logs.ReportError("GetIPEndPoint: " + e.Message);
                return false;
            }
            catch (Exception e)
            {
                logs.ReportError("GetIPEndPoint: " + e.Message);
                return false;
            }

            return true;
        }



        //###########################################
        //              Private Methods
        //###########################################

    }
}
