using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;

public static class DatabaseUpdateFunction
{
    [FunctionName("DatabaseUpdateFunction")]
    public static void Run(
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
            connection.Open();

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                // Execute the query and retrieve the updated data
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    List<DatabaseRecord> updatedRecords = new List<DatabaseRecord>();

                    while (reader.Read())
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
                    SendUpdatesToClients(updatedRecords, signalRMessages).GetAwaiter().GetResult();
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
}
