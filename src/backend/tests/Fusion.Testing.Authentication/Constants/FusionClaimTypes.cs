namespace Fusion.Testing.Authentication
{
    internal class FusionClaimTypes
    {
        public const string UserPrincipalName = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";

        public const string AzureUniquePersonId = "http://schemas.microsoft.com/identity/claims/objectidentifier";

        public const string ApplicationId = "appid";

        public const string Mail = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";

        public const string TransformationMarker = "http://schemas.fusion.equinor.com/identity/claims/transformationMarker";

        public const string FusionPeronId = "http://schemas.fusion.equinor.com/identity/claims/fusionpersonid";

        public const string LocalPersonId = "http://schemas.fusion.equinor.com/identity/claims/localpersonid";

        public const string Department = "http://schemas.fusion.equinor.com/identity/claims/department";

        public const string Company = "http://schemas.fusion.equinor.com/identity/claims/company";

        public const string CompanyId = "http://schemas.fusion.equinor.com/identity/claims/companyid";

        public const string JobTitle = "http://schemas.fusion.equinor.com/identity/claims/jobtitle";

        public const string AccountType = "http://schemas.fusion.equinor.com/identity/claims/accounttype";

        public const string AADApplicationName = "http://schemas.fusion.equinor.com/identity/claims/aadappname";

        public const string PositionDiscipline = "http://schemas.fusion.equinor.com/identity/claims/discipline";

        public const string FusionContract = "http://schemas.fusion.equinor.com/identity/claims/contract";

        public const string ProjectDomain = "http://schemas.fusion.equinor.com/identity/claims/projectdomain";

        public const string FusionProjectOrgChart = "http://schemas.fusion.equinor.com/identity/claims/orgprojectid";
    }
}
