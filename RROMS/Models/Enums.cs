namespace RROMS.Models;

public enum ReservationStatus
{
    Pending = 0,
    Confirmed = 1,
    Seated = 2,
    Completed = 3,
    Cancelled = 4,
    NoShow = 5
}

public enum OrderType
{
    DineIn = 0,
    Takeaway = 1
}

public enum OrderStatus
{
    Placed = 0,
    Preparing = 1,
    ReadyToServe = 2,
    Served = 3,
    Completed = 4,
    Cancelled = 5
}

public enum PaymentMethod
{
    Cash = 0,
    Card = 1,
    OnlinePayment = 2
}

public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3
}

public enum NotificationType
{
    Reservation = 0,
    Order = 1,
    Payment = 2,
    Promotion = 3,
    System = 4
}

/// <summary>
/// Central place for ASP.NET Core Identity role names used throughout the app.
/// </summary>
public static class Roles
{
    public const string Administrator = "Administrator";
    public const string Staff = "Staff";
    public const string Customer = "Customer";

    public static readonly string[] All = { Administrator, Staff, Customer };
}
