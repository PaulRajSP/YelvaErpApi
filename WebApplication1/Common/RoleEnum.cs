using System.Text.Json.Serialization;

namespace HoneyErpAPI.Common
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RoleEnum{Admin,Manager,Staff,Sales,Packer,Logistics,Production,Accountant}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EmployeeDocumentEnum{Aadhaar,PAN,VoterID,DrivingLicence,Passport,BankPassbook,EducationCertificate,ExperienceCertificate,Resume,Other}


}
