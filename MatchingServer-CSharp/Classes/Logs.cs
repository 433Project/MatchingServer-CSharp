using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace MatchingServer_CSharp.Classes
{
    /// <summary>
    /// The Logs class implements a logging library and provides logging methods for other classes to use.
    /// </summary>
    class Logs
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Sends a message to be logged by our logger.
        /// </summary>
        /// <param name="message">A string containing the desired message.</param>
        public void ReportMessage (string message)
        {
            logger.Info(message);
        }

        /// <summary>
        /// Sends and error to be logged by our logger.
        /// </summary>
        /// <param name="errorMessage">A string containing the error message.</param>
        public void ReportError (string errorMessage)
        {
            logger.Error("ERROR: " + errorMessage);
        }
    }
}
