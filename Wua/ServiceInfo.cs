using System;

namespace SolviaWindowsUpdater.Wua
{
    /// <summary>
    /// Represents information about a Windows Update service.
    /// </summary>
    public class ServiceInfo
    {
        /// <summary>
        /// Service ID (GUID).
        /// </summary>
        public string ServiceId { get; set; }

        /// <summary>
        /// Display name of the service.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether this is a managed service.
        /// </summary>
        public bool IsManaged { get; set; }

        /// <summary>
        /// Whether this is the default service.
        /// </summary>
        public bool IsDefaultAUService { get; set; }

        /// <summary>
        /// Whether this service offers Windows updates.
        /// </summary>
        public bool OffersWindowsUpdates { get; set; }

        /// <summary>
        /// Whether the service is registered with AU.
        /// </summary>
        public bool IsRegisteredWithAU { get; set; }

        /// <summary>
        /// Whether this is a scan package service.
        /// </summary>
        public bool IsScanPackageService { get; set; }

        /// <summary>
        /// Whether the service can register with AU.
        /// </summary>
        public bool CanRegisterWithAU { get; set; }

        /// <summary>
        /// Service URL (if available).
        /// </summary>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Setup prefix for the service.
        /// </summary>
        public string SetupPrefix { get; set; }

        /// <summary>
        /// Content validation certificates.
        /// </summary>
        public string ContentValidationCert { get; set; }

        /// <summary>
        /// Expiration date for registration.
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Gets a formatted display string for the service type.
        /// </summary>
        public string ServiceType
        {
            get
            {
                if (IsScanPackageService) return "Offline Scan";
                if (IsDefaultAUService) return "Default AU";
                if (IsManaged) return "Managed";
                return "Standard";
            }
        }
    }
}
