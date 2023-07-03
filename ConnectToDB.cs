using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;

public static class ConnectToDB
{
    [FunctionName("DatabaseUpdateFunction")]
    public static async Task Run(
        [TimerTrigger("0 */5 * * * *")] TimerInfo timer,
        [SignalR(HubName = "updates")] IAsyncCollector<SignalRMessage> signalRMessages,
        ILogger log)
    {
        // Connection string for the SQL Database
        string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

        // Query the database to check for updates
        string query = "SELECT * FROM playbackstats";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                // Execute the query and retrieve the updated data
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    List<PlaybackStatRecord> updatedRecords = new List<PlaybackStatRecord>();

                    while (await reader.ReadAsync())
                    {
                        // Process the retrieved data
                        // Assuming the PlaybackStatRecord class represents the structure of your table
                        PlaybackStatRecord record = new PlaybackStatRecord
                        {
                            Pid = reader.GetInt32(0),
                            AudioId = reader.GetInt32(1),
                            ListeningDuration = reader.GetTimeSpan(2),
                            TimeOfPlayback = reader.GetDateTime(3),
                            PlaybackLocationX = reader.GetFloat(4),
                            PlaybackLocationY = reader.GetFloat(5),
                            PlaybackLocationZ = reader.GetFloat(6)
                        };

                        updatedRecords.Add(record);
                    }

                    // Send the updated records to connected clients via SignalR
                    await SendUpdatesToClients(updatedRecords, signalRMessages);
                }
            }
        }

        log.LogInformation($"Database update function executed at: {DateTime.Now}");
    }

    private static async Task SendUpdatesToClients(List<PlaybackStatRecord> records, IAsyncCollector<SignalRMessage> signalRMessages)
    {
        // Create a message containing the updated records
        var message = new SignalRMessage
        {
            Target = "update",
            Arguments = new[] { records }
        };

        // Send the message to the "updates" hub in SignalR
        await signalRMessages.AddAsync(message);
    }

    public class PlaybackStatRecord
    {
        public int Pid { get; set; }
        public int AudioId { get; set; }
        public TimeSpan ListeningDuration { get; set; }
        public DateTime TimeOfPlayback { get; set; }
        public float PlaybackLocationX { get; set; }
        public float PlaybackLocationY { get; set; }
        public float PlaybackLocationZ { get; set; }
    }
}
