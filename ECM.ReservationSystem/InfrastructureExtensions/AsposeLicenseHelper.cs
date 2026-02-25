namespace ECM.ReservationSystem.InfrastructureExtensions;

public static class AsposeLicenseHelper
{
    public static void SetLicense(IConfiguration configuration)
    {
        var licensePath = configuration["Settings:AsposeLicenseFilePath"];

        if (string.IsNullOrWhiteSpace(licensePath))
        {
            return;
        }

        TrySetLicense("Aspose.Cells.License, Aspose.Cells", licensePath);
        TrySetLicense("Aspose.Words.License, Aspose.Words", licensePath);
    }

    private static void TrySetLicense(string licenseTypeName, string licensePath)
    {
        var licenseType = Type.GetType(licenseTypeName, throwOnError: false);

        if (licenseType is null)
        {
            return;
        }

        var instance = Activator.CreateInstance(licenseType);
        var setLicenseMethod = licenseType.GetMethod("SetLicense", new[] { typeof(string) });
        setLicenseMethod?.Invoke(instance, new object[] { licensePath });
    }
}
