namespace BadmintonBooking.Domain.Constants;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Provider = "Provider";
    public const string Player = "Player";
}

public static class FacilityStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Suspended = "Suspended";
}

public static class CourtTypes
{
    public const string PVC = "PVC";
    public const string Wooden = "Wooden";
    public const string Acrylic = "Acrylic";
}

public static class CourtStatuses
{
    public const string Active = "Active";
    public const string Maintenance = "Maintenance";
}

public static class BookingStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";
}

