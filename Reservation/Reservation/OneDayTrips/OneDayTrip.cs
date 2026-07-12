using Intalio.Case.Core.Objects;
using Intalio.Case.Core.Templates;
using System.Text;
using System.Text.Json;

namespace Reservation.OneDayTrips
{
    // Submits a One-Day-Trip booking to the ECM API. Deploy as:
    // Reservation.OneDayTrips.OneDayTrip, Reservation, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
    public class OneDayTrip : ActivityTemplate
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public override void Complete(WorkflowItem workflowItem)
        {
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
                var reservationUrl = GetPropertyValue(workflowItem, "ReservationURL");
                var workflowId = workflowItem.ActivityInstance.ActivityDefinition.WorkflowDefinition.WorkflowId;
                currentDocumentId = GetLongPropertyValue(workflowItem, "DocumentId");
                var formData = ResolveFormData(workflowItem, currentDocumentId);

                using var json = JsonDocument.Parse(formData);
                var root = json.RootElement;
                var submitRequest = BuildSubmitRequest(root, workflowId, currentDocumentId);
                var submitUrl = BuildUrl(reservationUrl, "/api/OneDayTrip/bookings");
                var requestBody = JsonSerializer.Serialize(submitRequest);

                WriteDebugArtifact(workflowItem, currentDocumentId, "SubmitUrl", submitUrl);
                WriteDebugArtifact(workflowItem, currentDocumentId, "SubmitRequest", requestBody);

                var responseBody = SendPostRequest(submitUrl, requestBody);
                WriteDebugArtifact(workflowItem, currentDocumentId, "SubmitResponse", responseBody);
            }
            catch (Exception ex)
            {
                if (ex is TripBookingFailedException submitFailure)
                {
                    WriteDebugArtifact(workflowItem, currentDocumentId, "SubmitResponse", submitFailure.ResponseBody);
                }

                WriteDebugArtifact(
                    workflowItem,
                    currentDocumentId,
                    "SubmitException",
                    $"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                Intalio.Core.ExceptionLogger.WriteEntry(
                    $"Exception in OneDayTrip Code Activity: {ex.Message}\nStack Trace: {ex.StackTrace}");
                throw;
            }
        }

        private static string ResolveFormData(WorkflowItem workflowItem, long currentDocumentId)
        {
            var directFormData = GetPropertyValue(workflowItem, "FormData");
            if (!string.IsNullOrWhiteSpace(directFormData))
            {
                WriteDebugArtifact(workflowItem, currentDocumentId, "FormData", directFormData);
                return directFormData;
            }

            var document = new Intalio.Case.Portal.Core.DAL.Document().FindIncludeDocumentTypeIncludeForm(currentDocumentId);
            var formData = document.DocumentPortal.Form ?? "{}";
            WriteDebugArtifact(workflowItem, currentDocumentId, "FormData", formData);
            return formData;
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

        private static TripBookingSubmitRequest BuildSubmitRequest(JsonElement root, long workflowId, long documentId)
        {
            return new TripBookingSubmitRequest
            {
                TripId = GetIntValue(root, "trip") ?? 0,
                EmployeeNumber = GetStringValue(root, "number") ?? string.Empty,
                EmployeeName = GetStringValue(root, "name") ?? string.Empty,
                Sector = ResolveSector(root),
                PhoneNumber = GetStringValue(root, "phoneNumber") ?? string.Empty,
                AdultsCount = GetIntValue(root, "adultsCount") ?? 0,
                ChildrenCount = GetIntValue(root, "childrenCount") ?? 0,
                CompanionsCount = GetIntValue(root, "companionsCount") ?? 0,
                Notes = BuildNotes(root),
                WorkflowId = workflowId,
                DocumentId = documentId
            };
        }

        private static string ResolveSector(JsonElement root)
        {
            return GetStringValue(root, "department")
                   ?? GetStringValue(root, "employeeDepartment")
                   ?? GetStringValue(root, "sector")
                   ?? string.Empty;
        }

        private static string BuildNotes(JsonElement root)
        {
            var parts = new List<string>();

            var department = GetStringValue(root, "department");
            if (!string.IsNullOrWhiteSpace(department))
            {
                parts.Add($"Department: {department}");
            }

            var total = GetDecimalValue(root, "price");
            if (total.HasValue)
            {
                parts.Add($"SubmittedTotal: {total.Value}");
            }

            return string.Join(" | ", parts);
        }

        private static string SendPostRequest(string url, string requestBody)
        {
            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            using var response = HttpClient.PostAsync(url, content).GetAwaiter().GetResult();
            var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                throw new TripBookingFailedException(
                    BuildFailureMessage((int)response.StatusCode, responseBody),
                    responseBody);
            }

            return responseBody;
        }

        private static string BuildFailureMessage(int statusCode, string responseBody)
        {
            var apiMessage = TryExtractApiMessage(responseBody);
            return !string.IsNullOrWhiteSpace(apiMessage)
                ? apiMessage
                : $"One-day-trip booking failed with status {statusCode}. Response: {responseBody}";
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

        private static string GetPropertyValue(WorkflowItem workflowItem, string key)
        {
            return workflowItem.Properties[key]?.Value?.ToString() ?? string.Empty;
        }

        private static long GetLongPropertyValue(WorkflowItem workflowItem, string key)
        {
            var rawValue = GetPropertyValue(workflowItem, key);
            return long.TryParse(rawValue, out var parsedValue) ? parsedValue : 0;
        }

        private static string? GetStringValue(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        private static int? GetIntValue(JsonElement root, string propertyName)
        {
            var rawValue = GetStringValue(root, propertyName);
            return int.TryParse(rawValue, out var parsedValue) ? parsedValue : null;
        }

        private static decimal? GetDecimalValue(JsonElement root, string propertyName)
        {
            var rawValue = GetStringValue(root, propertyName);
            return decimal.TryParse(rawValue, out var parsedValue) ? parsedValue : null;
        }

        private sealed class TripBookingSubmitRequest
        {
            public int TripId { get; set; }
            public string EmployeeNumber { get; set; } = string.Empty;
            public string EmployeeName { get; set; } = string.Empty;
            public string Sector { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public int AdultsCount { get; set; }
            public int ChildrenCount { get; set; }
            public int CompanionsCount { get; set; }
            public string Notes { get; set; } = string.Empty;
            public long WorkflowId { get; set; }
            public long DocumentId { get; set; }
        }

        private sealed class TripBookingFailedException : InvalidOperationException
        {
            public TripBookingFailedException(string message, string responseBody)
                : base(message)
            {
                ResponseBody = responseBody;
            }

            public string ResponseBody { get; }
        }
    }
}
