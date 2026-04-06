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
            throw new NotImplementedException();
        }

        public override void Execute(WorkflowItem workflowItem)
        {
            try
            {
                var reservationUrl = workflowItem.Properties["ReservationURL"].Value?.ToString() ?? string.Empty;
                var documentId = Convert.ToInt64(workflowItem.Properties["DocumentId"].Value);
                var workFlowId = workflowItem.ActivityInstance.ActivityDefinition.WorkflowDefinition.WorkflowId;

                string? filePath = null;
                if (workflowItem.Properties["Filepath"]?.Value != null)
                {
                    var folderPath = workflowItem.Properties["Filepath"].Value.ToString();
                    if (!string.IsNullOrWhiteSpace(folderPath))
                    {
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        filePath = Path.Combine(folderPath, $"FormData_{documentId}.txt");
                    }
                }

                var document = new Intalio.Case.Portal.Core.DAL.Document().FindIncludeDocumentTypeIncludeForm(documentId);
                var formData = document.DocumentPortal.Form;

                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    File.WriteAllText(filePath, formData);
                }

                var submission = JsonSerializer.Deserialize<ReservationWorkflowSubmission>(
                    formData,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (submission == null)
                {
                    throw new InvalidOperationException("Workflow form data could not be parsed.");
                }

                var payload = new ReservationSubmitRequest
                {
                    EmployeeNumber = submission.Number ?? string.Empty,
                    EmployeeName = submission.Name ?? string.Empty,
                    UnitId = submission.PropertyWithFloor,
                    WeekId = submission.Weeks ?? string.Empty,
                    NumberOfGuests = submission.Passengers > 0 ? submission.Passengers : 1,
                    IsTransportationRequired = submission.Passengers > 0,
                    Notes = BuildNotes(submission),
                    WorkflowId = workFlowId,
                    DocumentId = documentId
                };

                var submitUrl = BuildSubmitUrl(reservationUrl);
                var requestBody = JsonSerializer.Serialize(payload);
                using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                using var response = HttpClient.PostAsync(submitUrl, content).GetAwaiter().GetResult();
                var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Reservation submit failed with status {(int)response.StatusCode}. Response: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                Intalio.Core.ExceptionLogger.WriteEntry(
                    $"Exception in Reservation Code Activity: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }

        private static string BuildSubmitUrl(string reservationUrl)
        {
            if (string.IsNullOrWhiteSpace(reservationUrl))
            {
                throw new InvalidOperationException("ReservationUrl workflow property is required.");
            }

            return reservationUrl.TrimEnd('/') + "/api/Reservation/submit";
        }

        private static string BuildNotes(ReservationWorkflowSubmission submission)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(submission.Department))
            {
                parts.Add($"Department: {submission.Department}");
            }

            if (!string.IsNullOrWhiteSpace(submission.EmployeeDepartment))
            {
                parts.Add($"EmployeeDepartment: {submission.EmployeeDepartment}");
            }

            if (submission.City > 0)
            {
                parts.Add($"CityId: {submission.City}");
            }

            if (submission.Price.HasValue)
            {
                parts.Add($"SubmittedTotal: {submission.Price.Value}");
            }

            return string.Join(" | ", parts);
        }

        private sealed class ReservationWorkflowSubmission
        {
            public string? Name { get; set; }
            public string? Number { get; set; }
            public string? Department { get; set; }
            public string? EmployeeDepartment { get; set; }
            public int City { get; set; }
            public string? Weeks { get; set; }
            public int PropertyWithFloor { get; set; }
            public int Passengers { get; set; }
            public decimal? Price { get; set; }
            public decimal? UnitPrice { get; set; }
            public decimal? InsurancePrice { get; set; }
            public decimal? TransportationPrice { get; set; }
        }

        private sealed class ReservationSubmitRequest
        {
            public string EmployeeNumber { get; set; } = string.Empty;
            public string EmployeeName { get; set; } = string.Empty;
            public int UnitId { get; set; }
            public string WeekId { get; set; } = string.Empty;
            public int NumberOfGuests { get; set; }
            public bool IsTransportationRequired { get; set; }
            public string Notes { get; set; } = string.Empty;
            public long WorkflowId { get; set; }
            public long DocumentId { get; set; }
        }
    }
}
