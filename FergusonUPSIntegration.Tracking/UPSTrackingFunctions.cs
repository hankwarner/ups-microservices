using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Dapper;
using FergusonUPSIntregrationCore.Models;
using TrackingNumbers.Controllers;
using TeamsHelper;

namespace TrackingNumberIntegration
{
    public class UPSTrackingFunctions
    {
        public static IConfiguration _config { get; set; }
        public string errorLogsUrl = Environment.GetEnvironmentVariable("DEV_TEAMS_URL");

        public UPSTrackingFunctions(IConfiguration config)
        {
            _config = config;
        }


        /// <summary>
        ///     When a new CSV file is added to ups-tracking-numbers blob with the column name trackingNum, this function will add all tracking 
        ///     numbers from the file to an Azure SQL database, including origin and destination address, current status and location of the package.
        /// </summary>
        /// <param name="blob">The blob stream that was triggered.</param>
        /// <param name="fileName">Name of the file that was added to the blob.</param>
        /// <param name="log">Fuction logger (provided by Azure Function).</param>
        [FunctionName("AddNewTrackingNumbers")]
        public void AddNewTrackingNumbers(
            [BlobTrigger("ups-tracking-numbers/{fileName}.csv", Connection = "BLOB_CONN")] Stream blob, 
            string fileName, ILogger log)
        {
            try
            {
                if (!fileName.ToLower().Contains("tracking")) return;

                var fileController = new FileController(log);

                var trackingRecords = fileController.GetTrackingNumbersFromFile(blob);

                var upsController = new UPSController(log);

                upsController.AddTrackingNumbersToDB(trackingRecords);
            }
            catch(Exception ex)
            {
                var title = "Error in AddNewTrackingNumbers";
                var text = $"Error message: {ex.Message}. Stacktrace: {ex.StackTrace}";
                var color = "red";
                var teamsMessage = new TeamsMessage(title, text, color, errorLogsUrl);
                teamsMessage.LogToMicrosoftTeams(teamsMessage);
                log.LogError(ex, title);
            }
        }


        /// <summary>
        ///     Queries the UPS Status Updates table for packages that are in transit, meaning they have not been delivered, returned or contain
        ///     a UPS exception. For each package in transit, calls the UPS Track API to get the most recent status of the package. Attempts to 
        ///     insert a new line to the Status Updates table. An SQL exception will be thrown if the line already exists.
        /// </summary>
        /// <param name="timer">Triggers the function on time increvals.</param>
        /// <param name="log">Fuction logger (provided by Azure Function).</param>
        [FunctionName("UpdateTrackingNumbersInTransit")]
        public void UpdateTrackingNumbersInTransit([TimerTrigger("0 */60 * * * *")] TimerInfo timer, ILogger log)
        {
            try
            {
                var upsController = new UPSController(log);

                var trackingNumbersInTransit = upsController.GetTrackingNumbersInTransit();

                if (trackingNumbersInTransit.Count() == 0)
                {
                    log.LogInformation("No tracking numbers currently in transit.");
                    return;
                }

                upsController.UpdateCurrentStatusOfTrackingNumbers(trackingNumbersInTransit);
            }
            catch(Exception ex)
            {
                var title = "Error in UpdateTrackingNumbersInTransit";
                var text = $"Error message: {ex.Message}. Stacktrace: {ex.StackTrace}";
                var color = "red";
                var teamsMessage = new TeamsMessage(title, text, color, errorLogsUrl);
                teamsMessage.LogToMicrosoftTeams(teamsMessage);
                log.LogError(ex, title);
            }
        }
    }
}
