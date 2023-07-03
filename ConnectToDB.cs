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
        string query = "SELECT * FROM audiometadata";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                // Execute the query and retrieve the updated data
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    List<DatabaseRecord> updatedRecords = new List<DatabaseRecord>();

                    while (await reader.ReadAsync())
                    {
                        // Process the retrieved data
                        // Assuming the DatabaseRecord class represents the structure of your table
                        DatabaseRecord record = new DatabaseRecord
                        {
                            Aid = reader.GetInt32(0),
                            AName = reader.GetString(1),
                            ACreator = reader.GetString(2),
                            ADuration = reader.GetTimeSpan(3),
                            ALocation = reader.GetString(4)
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

    private static async Task SendUpdatesToClients(List<DatabaseRecord> records, IAsyncCollector<SignalRMessage> signalRMessages)
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
    public class DatabaseRecord
{
    public int Aid { get; set; }
    public string AName { get; set; }
    public string ACreator { get; set; }
    public TimeSpan ADuration { get; set; }
    public string ALocation { get; set; }
}

}
