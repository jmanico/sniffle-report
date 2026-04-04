namespace SniffleReport.Api.Models.Enums;

public enum AuditLogAction
{
    Create = 0,
    Update = 1,
    Delete = 2,
    StatusChange = 3,
    RoleChange = 4,
    Login = 5,
    FailedLogin = 6,
    FeedIngest = 7
}
