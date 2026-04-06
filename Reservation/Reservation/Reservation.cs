using Intalio.Case.Core.Objects;
using Intalio.Case.Core.Templates;
using Intalio.Case.Portal.Core.DAL;

namespace Reservation
{
    public class Reservation : ActivityTemplate
    {
        public override void Complete(WorkflowItem workflowItem)
        {
            throw new NotImplementedException();
        }

        public override void Execute(WorkflowItem workflowItem)
        {
            try
            {
                string ReservationUrl = workflowItem.Properties["ReservationUrl"].Value.ToString();
                long documentId = Convert.ToInt64(workflowItem.Properties["DocumentId"].Value);
                long workFlowId = workflowItem.ActivityInstance.ActivityDefinition.WorkflowDefinition.WorkflowId;
                int documentTypeBaseId = 0;

                //get document type base id
                using (CasePortalContext context = new CasePortalContext())
                {
                    documentTypeBaseId = context.Document.Where(i => i.Id == documentId).FirstOrDefault().DocumentTypeBaseId;
                }


                string Filepath = workflowItem.Properties["Filepath"].Value.ToString();
                var formData = string.Empty;
                Intalio.Case.Portal.Core.DAL.Document document = new Intalio.Case.Portal.Core.DAL.Document().FindIncludeDocumentTypeIncludeForm(documentId);
                formData = document.DocumentPortal.Form;



                string folderPath = Filepath; // change this to your desired location
                string filePath = Path.Combine(folderPath, $"FormData_{documentId}.txt");

                // Ensure folder exists
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Write form data to file
                File.WriteAllText(filePath, formData);

            }
            catch (Exception ex)
            {
                Intalio.Core.ExceptionLogger.WriteEntry(
                    $"Exception in SaveDataToFile Code Activity: {ex.Message}\nStack Trace: {ex.StackTrace}");
                return;
            }
        }
    }
}

