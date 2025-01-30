using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

public class UseDevCertAnnotation : IResourceAnnotation
{
    public UseDevCertAnnotation(CertificateFileFormat certificateFileFormat, string certFileEnv, string? certPasswordOrKeyEnv)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certFileEnv);
        if (certificateFileFormat is CertificateFileFormat.PfxWithPassword && string.IsNullOrWhiteSpace(certPasswordOrKeyEnv))
        {
            throw new ArgumentException("The environment variable name for the certificate password must be provided when exporting a PFX certificate with a password.", nameof(certPasswordOrKeyEnv));
        }
        if (certificateFileFormat is CertificateFileFormat.Pem && string.IsNullOrWhiteSpace(certPasswordOrKeyEnv))
        {
            throw new ArgumentException("The environment variable name for the certificate key file must be provided when exporting a PEM certificate.", nameof(certPasswordOrKeyEnv));
        }

        CertificateFileFormat = certificateFileFormat;
        CertFileEnv = certFileEnv;
        CertPasswordOrKeyEnv = certPasswordOrKeyEnv;
    }

    public CertificateFileFormat CertificateFileFormat { get; }

    public string CertFileEnv { get; }

    public string? CertPasswordOrKeyEnv { get; }
}
