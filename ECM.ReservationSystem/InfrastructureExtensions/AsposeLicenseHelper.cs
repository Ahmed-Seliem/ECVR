namespace ECM.ReservationSystem.InfrastructureExtensions;

public static class AsposeLicenseHelper
{
    public static void SetLicense(IConfiguration configuration)
    {
        var licensePath = configuration["AsposeLicenseFilePath"];

        if (string.IsNullOrWhiteSpace(licensePath))
        {
            return;
        }

        // Aspose.Cells
        var cellsLicense = new Aspose.Cells.License();
        cellsLicense.SetLicense(licensePath);

        // Aspose.Words
        var wordsLicense = new Aspose.Words.License();
        wordsLicense.SetLicense(licensePath);

    }
}
