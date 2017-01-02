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

        private IPEndPoint configServerEndPoint, connectionServerEndPoint;
        private IPAddress configServerIP, connectionServerIP;
        private int configServerPort, connectionServerPort, matchingServerPort, clientPort;

        //Properties
        public bool IsInitialized { get; private set; } = false;



        //###########################################
        //              Public Methods
        //###########################################

        public static void Initialize ()
        {
            logs = new Logs();
            logs.ReportMessage("ConnectionManager initializing. . .");
        }

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
                if (!xmlTextReader.ReadToFollowing("ip"))
                {
                    logs.ReportError("GetIPEndPoint cannot find child node <ip> within node <" + xmlNodeName + ">");
                    return false;
                }
                IPAddress address;
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
        /// This method reads a configuration XML file to obtain critical IP/port information required for connections.
        /// </summary>
        /// <returns>Returns true on successful loading and false on failure.</returns>
        private bool LoadConfiguration()
        {
            XmlDocument xmlDocument = new XmlDocument();

            try
            {
                xmlDocument.Load("network.xml");


                //Get ConfigServer Settings
                XmlNode serverNode = xmlDocument.DocumentElement.SelectSingleNode("ConfigServer");
                if (serverNode == null)
                {
                    logs.ReportError("LoadConfiguration cannot find node <ConfigServer>");
                    return false;
                }
                XmlNode childNode = serverNode.SelectSingleNode("ip");
                if (childNode == null)
                {
                    logs.ReportError("LoadConfiguration cannot find child node <ip> within node <ConfigServer>");
                    return false;
                }
                if (!IPAddress.TryParse(childNode.Value, out configServerIP))
                {
                    logs.ReportError("LoadConfiguration cannot parse ConfigServer ip address");
                    return false;
                }
                childNode = serverNode.SelectSingleNode("port");
                if (childNode == null)
                {
                    logs.ReportError("LoadConfiguration cannot find child node <port> within node <ConfigServer>");
                    return false;
                }
                if (!int.TryParse(childNode.Value, out configServerPort))
                {
                    logs.ReportError("LoadConfiguration cannot parse ConfigServer port number");
                    return false;
                }


                //Get ConnectionServer Settings
                serverNode = xmlDocument.DocumentElement.SelectSingleNode("ConnectionServer");
                if (serverNode == null)
                {
                    logs.ReportError("LoadConfiguration cannot find node <ConnectionServer>");
                    return false;
                }
                childNode = serverNode.SelectSingleNode("ip");
                if (childNode == null)
                {
                    logs.ReportError("LoadConfiguration cannot find child node <ip> within node <ConnectionServer>");
                    return false;
                }
                if (!IPAddress.TryParse(childNode.Value, out connectionServerIP))
                {
                    logs.ReportError("LoadConfiguration cannot parse ConnectionServer ip address");
                    return false;
                }
                childNode = serverNode.SelectSingleNode("port");
                if (childNode == null)
                {
                    logs.ReportError("LoadConfiguration cannot find child node <port> within node <ConnectionServer>");
                    return false;
                }
                if (!int.TryParse(childNode.Value, out connectionServerPort))
                {
                    logs.ReportError("LoadConfiguration cannot parse ConnectionServer port number");
                    return false;
                }


                //Get MatchingServer Listening Port Settings
                serverNode = xmlDocument.DocumentElement.SelectSingleNode("MatchingServers");
                if (serverNode == null)
                {
                    logs.ReportError("LoadConfiguration cannot find node <MatchingServers>");
                    return false;
                }
                childNode = serverNode.SelectSingleNode("port");
                if (childNode == null)
                {
                    logs.ReportError("LoadConfiguration cannot find child node <port> within node <MatchingServers>");
                    return false;
                }
                if (!int.TryParse(childNode.Value, out matchingServerPort))
                {
                    logs.ReportError("LoadConfiguration cannot parse MatchingServers port number");
                    return false;
                }


                //Get Client Listening Port Settings
                serverNode = xmlDocument.DocumentElement.SelectSingleNode("Clients");
                if (serverNode == null)
                {
                    logs.ReportError("LoadConfiguration cannot find node <Clients>");
                    return false;
                }
                childNode = serverNode.SelectSingleNode("port");
                if (childNode == null)
                {
                    logs.ReportError("LoadConfiguration cannot find child node <port> within node <Clients>");
                    return false;
                }
                if (!int.TryParse(childNode.Value, out clientPort))
                {
                    logs.ReportError("LoadConfiguration cannot parse Clients port number");
                    return false;
                }
            }
            catch (FileNotFoundException e)
            {
                logs.ReportError("LoadConfiguration cannot find XML-file ...\\network.xml");
            }
            catch (XmlException e)
            {
                logs.ReportError("LoadConfiguration: " + e.Message);
            }
            catch (Exception e)
            {
                logs.ReportError("LoadConfiguration: " + e.Message);
            }

            return true;
        }

        //###########################################
        //              Private Methods
        //###########################################

    }
}
