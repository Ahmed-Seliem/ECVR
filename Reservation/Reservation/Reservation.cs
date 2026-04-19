using Intalio.Case.Core.Objects;
using Intalio.Case.Core.Templates;
using System.Text;
using System.Text.Json;

namespace Reservation
{
    public class Reservation : ActivityTemplate
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
                var reservationUrl = GetPropertyValue(workflowItem, "ReservationURL");
                var workflowId = workflowItem.ActivityInstance.ActivityDefinition.WorkflowDefinition.WorkflowId;
                currentDocumentId = GetLongPropertyValue(workflowItem, "DocumentId");
                var formData = ResolveFormData(workflowItem, currentDocumentId);

                using var json = JsonDocument.Parse(formData);
                var root = json.RootElement;
                var submitRequest = BuildSubmitRequest(root, workflowId, currentDocumentId);
                var submitUrl = BuildUrl(reservationUrl, "/api/Reservation/submit");
                var requestBody = JsonSerializer.Serialize(submitRequest);

                WriteDebugArtifact(workflowItem, currentDocumentId, "SubmitUrl", submitUrl);
                WriteDebugArtifact(workflowItem, currentDocumentId, "SubmitRequest", requestBody);

                var responseBody = SendPostRequest(submitUrl, requestBody);
                WriteDebugArtifact(workflowItem, currentDocumentId, "SubmitResponse", responseBody);
            }
            catch (Exception ex)
            {
                WriteDebugArtifact(
                    workflowItem,
                    currentDocumentId,
                    "SubmitException",
                    $"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                Intalio.Core.ExceptionLogger.WriteEntry(
                    $"Exception in Reservation Code Activity: {ex.Message}\nStack Trace: {ex.StackTrace}");
                throw;
            }
        }

        private static string ResolveFormData(WorkflowItem workflowItem, long currentDocumentId)
        {
            var directFormData = GetPropertyValue(workflowItem, "FormData");
            if (!string.IsNullOrWhiteSpace(directFormData))
            {
                WriteDebugFormData(workflowItem, currentDocumentId, directFormData);
                return directFormData;
            }

            var document = new Intalio.Case.Portal.Core.DAL.Document().FindIncludeDocumentTypeIncludeForm(currentDocumentId);
            var formData = document.DocumentPortal.Form ?? "{}";
            WriteDebugFormData(workflowItem, currentDocumentId, formData);
            return formData;
        }

        private static void WriteDebugFormData(WorkflowItem workflowItem, long documentId, string formData)
        {
            WriteDebugArtifact(workflowItem, documentId, "FormData", formData);
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

        private static ReservationSubmitRequest BuildSubmitRequest(JsonElement root, long workflowId, long documentId)
        {
            var passengers = GetIntValue(root, "passengers") ?? 0;
            var normalizedGuests = passengers > 0 ? passengers : 1;

            return new ReservationSubmitRequest
            {
                EmployeeNumber = GetStringValue(root, "number") ?? string.Empty,
                EmployeeName = GetStringValue(root, "name") ?? string.Empty,
                PhoneNumber = GetStringValue(root, "phoneNumber") ?? string.Empty,
                UnitId = GetIntValue(root, "propertyWithFloor") ?? 0,
                WeekId = GetStringValue(root, "weeks") ?? string.Empty,
                NumberOfGuests = normalizedGuests,
                IsTransportationRequired = passengers > 0,
                PaymentReceiptNumber = GetStringValue(root, "paymentReceiptNumber") ?? string.Empty,
                InsuranceReceiptNumber = GetStringValue(root, "insuranceReceiptNumber") ?? string.Empty,
                Notes = BuildNotes(root),
                WorkflowId = workflowId,
                DocumentId = documentId
            };
        }

        private static string BuildNotes(JsonElement root)
        {
            var parts = new List<string>();

            var department = GetStringValue(root, "department");
            if (!string.IsNullOrWhiteSpace(department))
            {
                parts.Add($"Department: {department}");
            }

            var employeeDepartment = GetStringValue(root, "employeeDepartment");
            if (!string.IsNullOrWhiteSpace(employeeDepartment))
            {
                parts.Add($"EmployeeDepartment: {employeeDepartment}");
            }

            var cityId = GetIntValue(root, "city");
            if (cityId.HasValue && cityId.Value > 0)
            {
                parts.Add($"CityId: {cityId.Value}");
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
                throw new InvalidOperationException(
                    $"Reservation request failed with status {(int)response.StatusCode}. Response: {responseBody}");
            }

            return responseBody;
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

        private sealed class ReservationSubmitRequest
        {
            public string EmployeeNumber { get; set; } = string.Empty;
            public string EmployeeName { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public int UnitId { get; set; }
            public string WeekId { get; set; } = string.Empty;
            public int NumberOfGuests { get; set; }
            public bool IsTransportationRequired { get; set; }
            public string PaymentReceiptNumber { get; set; } = string.Empty;
            public string InsuranceReceiptNumber { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
            public long WorkflowId { get; set; }
            public long DocumentId { get; set; }
        }
    }
}
