using System;

namespace DataReviver
{
    public enum UserRole
    {
        Admin,
        Investigator,
        Analyst,
        ReadOnly
    }

    public class UserSession
    {
        public string Username { get; set; }
        public UserRole Role { get; set; }
        public DateTime LoginTime { get; set; }
        public string FullName { get; set; }

        public UserSession(string username, UserRole role, string fullName = "")
        {
            Username = username;
            Role = role;
            FullName = string.IsNullOrEmpty(fullName) ? username : fullName;
            LoginTime = DateTime.Now;
        }

        // Permission methods
        public bool CanCreateCases => Role == UserRole.Admin || Role == UserRole.Investigator;
        public bool CanDeleteCases => Role == UserRole.Admin;
        public bool CanModifyEvidence => Role == UserRole.Admin || Role == UserRole.Investigator;
        public bool CanExportData => Role == UserRole.Admin || Role == UserRole.Investigator || Role == UserRole.Analyst;
        public bool CanViewSensitiveData => Role == UserRole.Admin || Role == UserRole.Investigator;
        public bool CanManageUsers => Role == UserRole.Admin;
        public bool CanAccessForensicTools => Role != UserRole.ReadOnly;
        public bool CanRecoverFiles => Role == UserRole.Admin || Role == UserRole.Investigator;

        public string GetRoleDisplayName()
        {
            switch (Role)
            {
                case UserRole.Admin:
                    return "Administrator";
                case UserRole.Investigator:
                    return "Lead Investigator";
                case UserRole.Analyst:
                    return "Forensic Analyst";
                case UserRole.ReadOnly:
                    return "Read-Only User";
                default:
                    return "Unknown";
            }
        }

        public string GetPermissionSummary()
        {
            switch (Role)
            {
                case UserRole.Admin:
                    return "Full system access, user management, case creation/deletion";
                case UserRole.Investigator:
                    return "Case management, evidence handling, file recovery";
                case UserRole.Analyst:
                    return "Data analysis, report generation, limited evidence access";
                case UserRole.ReadOnly:
                    return "View-only access to existing cases and reports";
                default:
                    return "No permissions defined";
            }
        }
    }
}
