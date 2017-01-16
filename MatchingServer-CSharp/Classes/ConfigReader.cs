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
        public static int FixedMessageSize { get; private set; } = 0;



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


        /// <summary>
        /// This method retrieves the port number of a connection entry in the network.xml configuration file.
        /// </summary>
        /// <param name="xmlNodeName">The name of the connection node (ConfigServer, ConnectionServer).</param>
        /// <param name="portNumber">An out parameter that holds the port number contained in the configuration file.</param>
        /// <returns>Returns true upon a successful read and false if the data could not be obtained.</returns>
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


        /// <summary>
        /// This method using an XmlTextReader to retrieve a specified value from config.xml.
        /// </summary>
        /// <param name="xmlNodeName">The name of the settings value under the "GeneralSettings" node (ConfigServerConnectAttempts, FixedMessageSize).</param>
        /// <param name="numericalValue">An out parameter to hold the integer value stored in the XML file.</param>
        /// <returns>Returns true upon successful retrieval and false on error.</returns>
        public static bool GetSettingsIntValue(string xmlNodeName, out int numericalValue)
        {
            numericalValue = -1;
            XmlTextReader xmlTextReader = null;

            try
            {
                xmlTextReader = new XmlTextReader("config.xml");

                if (!xmlTextReader.ReadToFollowing("GeneralSettings"))
                {
                    logs.ReportError("GetSettingsIntValue cannot find node <GeneralSettings>");
                    return false;
                }
                if (!xmlTextReader.ReadToDescendant(xmlNodeName))
                {
                    logs.ReportError("GetSettingsIntValue cannot find node <" + xmlNodeName + ">");
                    return false;
                }
                if (!int.TryParse(xmlTextReader.GetAttribute("value"), out numericalValue))
                {
                    logs.ReportError("GetSettingsIntValue cannot parse numerical value within node <" + xmlNodeName + ">");
                    return false;
                }
            }
            catch (FileNotFoundException e)
            {
                logs.ReportError("GetSettingsIntValue cannot find XML-file ...\\config.xml");
                return false;
            }
            catch (XmlException e)
            {
                logs.ReportError("GetSettingsIntValue: " + e.Message);
                return false;
            }
            catch (Exception e)
            {
                logs.ReportError("GetSettingsIntValue: " + e.Message);
                return false;
            }

            return true;
        }


        /// <summary>
        /// This method loads the FixedMessageSize from the config.xml file, under the GeneralSettings node.
        /// </summary>
        /// <returns>Returns true if a value was successfully obtained or false otherwise.</returns>
        public static bool SetFixedMessageSizeFromFile ()
        {
            int size;
            if (!GetSettingsIntValue("FixedMessageSize", out size))
            {
                return false;
            }
            FixedMessageSize = size;
            logs.ReportMessage("FixedMessageSize set to " + FixedMessageSize);
            return true;
        }

        //###########################################
        //              Private Methods
        //###########################################

    }
}
