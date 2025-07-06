namespace Microsoft.Vault.Explorer.Common
{
    using System;

    public class ApplicationDeployment
    {
        private static ApplicationDeployment currentDeployment;
        private static bool currentDeploymentInitialized;

        private static bool isNetworkDeployed;
        private static bool isNetworkDeployedInitialized;

        public static bool IsNetworkDeployed
        {
            get
            {
                if (!isNetworkDeployedInitialized)
                {
                    bool.TryParse(Environment.GetEnvironmentVariable("ClickOnce_IsNetworkDeployed"), out isNetworkDeployed);
                    isNetworkDeployedInitialized = true;
                }

                return isNetworkDeployed;
            }
        }

        public static ApplicationDeployment CurrentDeployment
        {
            get
            {
                if (!currentDeploymentInitialized)
                {
                    currentDeployment = IsNetworkDeployed ? new ApplicationDeployment() : null;
                    currentDeploymentInitialized = true;
                }

                return currentDeployment;
            }
        }

        public Uri ActivationUri
        {
            get
            {
                Uri.TryCreate(Environment.GetEnvironmentVariable("ClickOnce_ActivationUri"), UriKind.Absolute, out Uri val);
                return val;
            }
        }

        public Version CurrentVersion
        {
            get
            {
                Version.TryParse(Environment.GetEnvironmentVariable("ClickOnce_CurrentVersion"), out Version val);
                return val;
            }
        }

        public string DataDirectory
        {
            get { return Environment.GetEnvironmentVariable("ClickOnce_DataDirectory"); }
        }

        public bool IsFirstRun
        {
            get
            {
                bool.TryParse(Environment.GetEnvironmentVariable("ClickOnce_IsFirstRun"), out bool val);
                return val;
            }
        }

        public DateTime TimeOfLastUpdateCheck
        {
            get
            {
                DateTime.TryParse(Environment.GetEnvironmentVariable("ClickOnce_TimeOfLastUpdateCheck"), out DateTime value);
                return value;
            }
        }

        public string UpdatedApplicationFullName
        {
            get { return Environment.GetEnvironmentVariable("ClickOnce_UpdatedApplicationFullName"); }
        }

        public Version UpdatedVersion
        {
            get
            {
                Version.TryParse(Environment.GetEnvironmentVariable("ClickOnce_UpdatedVersion"), out Version val);
                return val;
            }
        }

        public Uri UpdateLocation
        {
            get
            {
                Uri.TryCreate(Environment.GetEnvironmentVariable("ClickOnce_UpdateLocation"), UriKind.Absolute, out Uri val);
                return val;
            }
        }

        public Version LauncherVersion
        {
            get
            {
                Version.TryParse(Environment.GetEnvironmentVariable("ClickOnce_LauncherVersion"), out Version val);
                return val;
            }
        }

        private ApplicationDeployment()
        {
            // As an alternative solution, we could initialize all properties here
        }
    }
}