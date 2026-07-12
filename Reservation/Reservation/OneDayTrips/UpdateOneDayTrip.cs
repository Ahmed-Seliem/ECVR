using Intalio.Case.Core.Objects;
using Intalio.Case.Core.Templates;
using System.Text;
using System.Text.Json;

namespace Reservation.OneDayTrips
{
    // Updates a One-Day-Trip booking status by DocumentId. Deploy as:
    // Reservation.OneDayTrips.UpdateOneDayTrip, Reservation, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
    public class UpdateOneDayTrip : ActivityTemplate
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public override void Complete(WorkflowItem workflowItem)
        {
            ProcessWorkflowItem(workflowItem);
        }

        public override void Execute(WorkflowItem workflowItem)
        {
            ProcessWorkflowItem(workflowItem);
        }

        private static void ProcessWorkflowItem(WorkflowItem workflowItem)
        {
            var currentDocumentId = 0L;
            try
            {
                currentDocumentId = GetLongPropertyValue(workflowItem, "DocumentId");
                var reservationStatus = GetLongPropertyValue(workflowItem, "ReservationStatus");
                var reservationUrl = GetPropertyValue(workflowItem, "ReservationURL");

                WriteDebugArtifact(workflowItem, currentDocumentId, "UpdateInput",
                    $"DocumentId={currentDocumentId}; ReservationStatus={reservationStatus}; ReservationURL={reservationUrl}");

                if (currentDocumentId <= 0)
                {
                    throw new InvalidOperationException("DocumentId workflow property is required.");
                }

                // Reuse the same WF status property as the legacy flow:
                // 5 = Approved -> booking Confirmed (2), 4 = Cancelled -> booking Cancelled (3)
                if (reservationStatus is not 4 and not 5)
                {
                    throw new InvalidOperationException("ReservationStatus must be 4 (Cancelled) or 5 (Approved).");
                }

                var bookingStatus = reservationStatus == 5 ? 2 : 3;

                var request = new UpdateTripBookingRequest
                {
                    DocumentId = currentDocumentId,
                    BookingStatus = bookingStatus
                };

                var updateUrl = BuildUrl(reservationUrl, "/api/OneDayTrip/update-booking");
                var requestBody = JsonSerializer.Serialize(request);

                WriteDebugArtifact(workflowItem, currentDocumentId, "UpdateUrl", updateUrl);
                WriteDebugArtifact(workflowItem, currentDocumentId, "UpdateRequest", requestBody);

                var responseBody = SendPostRequest(updateUrl, requestBody);
                WriteDebugArtifact(workflowItem, currentDocumentId, "UpdateResponse", responseBody);
            }
            catch (Exception ex)
            {
                WriteDebugArtifact(
                    workflowItem,
                    currentDocumentId,
                    "UpdateException",
                    $"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                Intalio.Core.ExceptionLogger.WriteEntry(
                    $"Exception in UpdateOneDayTrip Code Activity: {ex.Message}\nStack Trace: {ex.StackTrace}");
                throw;
            }
        }

        private static void WriteDebugArtifact(WorkflowItem workflowItem, long documentId, string artifactName, string content)
        {
            var folderPath = GetPropertyValue(workflowItem, "Filepath");
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, $"{artifactName}_{documentId}.txt");
            File.WriteAllText(filePath, content);
        }

        private static long GetLongPropertyValue(WorkflowItem workflowItem, string key)
        {
            var rawValue = GetPropertyValue(workflowItem, key);
            return long.TryParse(rawValue, out var parsedValue) ? parsedValue : 0;
        }

        private static string GetPropertyValue(WorkflowItem workflowItem, string key)
        {
            return workflowItem.Properties[key]?.Value?.ToString() ?? string.Empty;
        }

        private static string SendPostRequest(string url, string requestBody)
        {
            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            using var response = HttpClient.PostAsync(url, content).GetAwaiter().GetResult();
            var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                var apiMessage = TryExtractApiMessage(responseBody);
                throw new InvalidOperationException(
                    !string.IsNullOrWhiteSpace(apiMessage)
                        ? apiMessage
                        : $"Update one-day-trip booking failed with status {(int)response.StatusCode}. Response: {responseBody}");
            }

            return responseBody;
        }

        private static string? TryExtractApiMessage(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return null;
            }

            try
            {
                using var json = JsonDocument.Parse(responseBody);
                if (json.RootElement.TryGetProperty("message", out var messageElement) &&
                    messageElement.ValueKind == JsonValueKind.String)
                {
                    return messageElement.GetString();
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private static string BuildUrl(string reservationUrl, string path)
        {
            if (string.IsNullOrWhiteSpace(reservationUrl))
            {
                throw new InvalidOperationException("ReservationUrl workflow property is required.");
            }

            return reservationUrl.TrimEnd('/') + path;
        }

        private sealed class UpdateTripBookingRequest
        {
            public long DocumentId { get; set; }
            public int BookingStatus { get; set; }
        }
    }
}
