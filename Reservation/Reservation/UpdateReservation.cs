using Intalio.Case.Core.Objects;
using Intalio.Case.Core.Templates;
using System.Text;
using System.Text.Json;

namespace Reservation
{
    public class UpdateReservation : ActivityTemplate
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
            try
            {
                var currentDocumentId = GetLongPropertyValue(workflowItem, "DocumentId");
                var status = GetLongPropertyValue(workflowItem, "ReservationStatus");
                var reservationUrl = GetPropertyValue(workflowItem, "ReservationURL");

                if (currentDocumentId <= 0)
                {
                    throw new InvalidOperationException("DocumentId workflow property is required.");
                }

                if (status is not 4 and not 5)
                {
                    throw new InvalidOperationException("ReservationStatus must be 4 (Cancelled) or 5 (Approved).");
                }

                var request = new UpdateReservationStatusRequest
                {
                    DocumentId = currentDocumentId,
                    ReservationStatus = status,
                    PaymentReceiptNumber = GetFirstPropertyValue(
                        workflowItem,
                        "PaymentReceiptNumber",
                        "paymentReceiptNumber"),
                    InsuranceReceiptNumber = GetFirstPropertyValue(
                        workflowItem,
                        "InsuranceReceiptNumber",
                        "insuranceReceiptNumber"),
                    Notes = GetPropertyValue(workflowItem, "Notes")
                };

                SendPostRequest(BuildUrl(reservationUrl, "/api/Reservation/update-reservation"), request);
            }
            catch (Exception ex)
            {
                Intalio.Core.ExceptionLogger.WriteEntry(
                    $"Exception in UpdateReservation Code Activity: {ex.Message}\nStack Trace: {ex.StackTrace}");
                throw;
            }
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

        private static string GetFirstPropertyValue(WorkflowItem workflowItem, params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = GetPropertyValue(workflowItem, key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static void SendPostRequest<TPayload>(string url, TPayload payload)
        {
            var requestBody = JsonSerializer.Serialize(payload);
            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            using var response = HttpClient.PostAsync(url, content).GetAwaiter().GetResult();
            var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                var apiMessage = TryExtractApiMessage(responseBody);
                throw new InvalidOperationException(
                    !string.IsNullOrWhiteSpace(apiMessage)
                        ? apiMessage
                        : $"Update reservation request failed with status {(int)response.StatusCode}. Response: {responseBody}");
            }
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

        private sealed class UpdateReservationStatusRequest
        {
            public long DocumentId { get; set; }
            public long ReservationStatus { get; set; }
            public string PaymentReceiptNumber { get; set; } = string.Empty;
            public string InsuranceReceiptNumber { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
        }
    }
}
