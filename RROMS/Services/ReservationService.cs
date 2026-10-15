using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Services;

public interface IReservationService
{
    Task<List<RestaurantTable>> GetAvailableTablesAsync(DateTime date, TimeSpan time, int partySize, int? diningAreaId = null);
    Task<(bool Success, string Message, Reservation? Reservation)> CreateReservationAsync(
        string customerId, DateTime date, TimeSpan time, int partySize, string? specialRequests, int? diningAreaId, int? preferredTableId = null);
    Task<(bool Success, string Message)> UpdateReservationAsync(int reservationId, string customerId, DateTime date, TimeSpan time, int partySize, string? specialRequests, bool isStaffOrAdmin);
    Task<(bool Success, string Message)> CancelReservationAsync(int reservationId, string requestingUserId, bool isStaffOrAdmin, string? reason);
    Task<(bool Success, string Message)> ConfirmReservationAsync(int reservationId, string staffId, int? tableId);
    Task<(bool Success, string Message)> UpdateStatusAsync(int reservationId, ReservationStatus status, string staffId);
    Task<List<Reservation>> GetCustomerReservationsAsync(string customerId);
    Task<List<Reservation>> GetReservationsForDateAsync(DateTime date);
    Task<Reservation?> GetByIdAsync(int id);
}

public class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public ReservationService(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    private async Task<int> GetDurationMinutesAsync()
    {
        var setting = await _context.RestaurantSettings.FirstOrDefaultAsync();
        return setting?.ReservationDurationMinutes ?? 90;
    }

    public async Task<List<RestaurantTable>> GetAvailableTablesAsync(DateTime date, TimeSpan time, int partySize, int? diningAreaId = null)
    {
        var duration = await GetDurationMinutesAsync();
        var requestedStart = date.Date.Add(time);
        var requestedEnd = requestedStart.AddMinutes(duration);

        // Restaurant must be open for the entire requested window.
        var hours = await _context.OperatingHours.FirstOrDefaultAsync(h => h.DayOfWeek == requestedStart.DayOfWeek);
        var bookingEndsAt = time.Add(TimeSpan.FromMinutes(duration));
        if (hours == null || hours.IsClosed || time < hours.OpenTime || bookingEndsAt > hours.CloseTime)
            return new List<RestaurantTable>();

        var candidateTablesQuery = _context.RestaurantTables
            .Include(t => t.DiningArea)
            .Where(t => t.IsActive && t.Capacity >= partySize);

        if (diningAreaId.HasValue)
            candidateTablesQuery = candidateTablesQuery.Where(t => t.DiningAreaId == diningAreaId.Value);

        var candidateTables = await candidateTablesQuery.OrderBy(t => t.Capacity).ToListAsync();
        if (candidateTables.Count == 0)
            return candidateTables;

        // Pull same-day active reservations for these tables and filter overlap in memory
        // (keeps the LINQ-to-SQL translation simple and avoids TimeSpan-arithmetic pitfalls).
        var tableIds = candidateTables.Select(t => t.Id).ToList();
        var dayStart = date.Date.AddDays(-1); // small buffer in case a booking started the evening before (not expected, but safe)
        var dayEnd = date.Date.AddDays(1);

        var sameWindowReservations = await _context.Reservations
            .Where(r => r.TableId != null
                        && tableIds.Contains(r.TableId!.Value)
                        && r.ReservationDateTime >= dayStart
                        && r.ReservationDateTime < dayEnd
                        && (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Seated))
            .ToListAsync();

        var busyTableIds = sameWindowReservations
            .Where(r => r.ReservationDateTime < requestedEnd && requestedStart < r.ReservationDateTime.AddMinutes(r.DurationMinutes))
            .Select(r => r.TableId!.Value)
            .ToHashSet();

        return candidateTables.Where(t => !busyTableIds.Contains(t.Id)).ToList();
    }

    public async Task<(bool Success, string Message, Reservation? Reservation)> CreateReservationAsync(
        string customerId, DateTime date, TimeSpan time, int partySize, string? specialRequests, int? diningAreaId, int? preferredTableId = null)
    {
        var setting = await _context.RestaurantSettings.FirstOrDefaultAsync();
        var duration = setting?.ReservationDurationMinutes ?? 90;
        var maxAdvanceDays = setting?.MaxAdvanceBookingDays ?? 60;

        var requestedStart = date.Date.Add(time);
        if (requestedStart < DateTime.Now.AddMinutes(-1))
            return (false, "You can't book a table in the past.", null);
        if (requestedStart > DateTime.Now.AddDays(maxAdvanceDays))
            return (false, $"Reservations can only be made up to {maxAdvanceDays} days in advance.", null);
        if (partySize < 1 || partySize > 30)
            return (false, "Party size must be between 1 and 30.", null);

        var availableTables = await GetAvailableTablesAsync(date, time, partySize, diningAreaId);
        if (availableTables.Count == 0)
            return (false, "Sorry, there is no table available for that date, time and party size. Try a different time or party size.", null);

        var chosenTable = preferredTableId.HasValue
            ? availableTables.FirstOrDefault(t => t.Id == preferredTableId.Value)
            : availableTables.First(); // smallest table that fits, since GetAvailableTablesAsync orders by capacity ascending

        if (chosenTable == null)
            return (false, "The table you selected is no longer available. Please pick another one.", null);

        var reservation = new Reservation
        {
            CustomerId = customerId,
            TableId = chosenTable.Id,
            PartySize = partySize,
            ReservationDateTime = requestedStart,
            DurationMinutes = duration,
            Status = ReservationStatus.Confirmed, // auto-confirmed since availability was just validated
            SpecialRequests = specialRequests,
            CreatedAt = DateTime.UtcNow,
            ConfirmedAt = DateTime.UtcNow
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        await _notificationService.NotifyAsync(
            customerId,
            "Reservation confirmed",
            $"Your table for {partySize} on {requestedStart:dddd, dd MMM yyyy} at {requestedStart:HH:mm} is confirmed (Table {chosenTable.TableNumber}).",
            NotificationType.Reservation,
            "/Reservations/Index");

        return (true, "Reservation confirmed.", reservation);
    }

    public async Task<(bool Success, string Message)> UpdateReservationAsync(
        int reservationId, string customerId, DateTime date, TimeSpan time, int partySize, string? specialRequests, bool isStaffOrAdmin)
    {
        var reservation = await _context.Reservations.Include(r => r.Table).FirstOrDefaultAsync(r => r.Id == reservationId);
        if (reservation == null)
            return (false, "Reservation not found.");
        if (!isStaffOrAdmin && reservation.CustomerId != customerId)
            return (false, "You can only modify your own reservations.");
        if (reservation.Status is ReservationStatus.Completed or ReservationStatus.Cancelled or ReservationStatus.NoShow)
            return (false, "This reservation can no longer be modified.");

        var availableTables = await GetAvailableTablesAsync(date, time, partySize);
        // Allow keeping the same table if it's already the one booked and still fits.
        var stillHasCurrentTable = reservation.TableId.HasValue &&
            (availableTables.Any(t => t.Id == reservation.TableId.Value) ||
             await IsSameSlotAsync(reservation, date, time));

        RestaurantTable? chosenTable = null;
        if (stillHasCurrentTable && reservation.Table != null && reservation.Table.Capacity >= partySize)
        {
            chosenTable = reservation.Table;
        }
        else if (availableTables.Count > 0)
        {
            chosenTable = availableTables.First();
        }
        else
        {
            return (false, "No table is available for the new date/time/party size. Try different details.");
        }

        var setting = await _context.RestaurantSettings.FirstOrDefaultAsync();
        reservation.ReservationDateTime = date.Date.Add(time);
        reservation.DurationMinutes = setting?.ReservationDurationMinutes ?? 90;
        reservation.PartySize = partySize;
        reservation.SpecialRequests = specialRequests;
        reservation.TableId = chosenTable.Id;
        reservation.Status = ReservationStatus.Confirmed;

        await _context.SaveChangesAsync();

        await _notificationService.NotifyAsync(
            reservation.CustomerId,
            "Reservation updated",
            $"Your reservation is now for {reservation.ReservationDateTime:dddd, dd MMM yyyy} at {reservation.ReservationDateTime:HH:mm}.",
            NotificationType.Reservation,
            "/Reservations/Index");

        return (true, "Reservation updated.");
    }

    private Task<bool> IsSameSlotAsync(Reservation reservation, DateTime date, TimeSpan time)
    {
        var newStart = date.Date.Add(time);
        return Task.FromResult(reservation.ReservationDateTime == newStart);
    }

    public async Task<(bool Success, string Message)> CancelReservationAsync(int reservationId, string requestingUserId, bool isStaffOrAdmin, string? reason)
    {
        var reservation = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId);
        if (reservation == null)
            return (false, "Reservation not found.");
        if (!isStaffOrAdmin && reservation.CustomerId != requestingUserId)
            return (false, "You can only cancel your own reservations.");
        if (reservation.Status is ReservationStatus.Completed or ReservationStatus.Cancelled)
            return (false, "This reservation is already closed.");

        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancellationReason = reason;
        await _context.SaveChangesAsync();

        await _notificationService.NotifyAsync(
            reservation.CustomerId,
            "Reservation cancelled",
            $"Your reservation for {reservation.ReservationDateTime:dddd, dd MMM yyyy} at {reservation.ReservationDateTime:HH:mm} has been cancelled.",
            NotificationType.Reservation,
            "/Reservations/Index");

        return (true, "Reservation cancelled.");
    }

    public async Task<(bool Success, string Message)> ConfirmReservationAsync(int reservationId, string staffId, int? tableId)
    {
        var reservation = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId);
        if (reservation == null)
            return (false, "Reservation not found.");

        if (tableId.HasValue)
            reservation.TableId = tableId.Value;

        reservation.Status = ReservationStatus.Confirmed;
        reservation.ConfirmedAt = DateTime.UtcNow;
        reservation.ConfirmedByStaffId = staffId;
        await _context.SaveChangesAsync();

        await _notificationService.NotifyAsync(
            reservation.CustomerId,
            "Reservation confirmed",
            $"Our team has confirmed your reservation for {reservation.ReservationDateTime:dddd, dd MMM yyyy} at {reservation.ReservationDateTime:HH:mm}.",
            NotificationType.Reservation,
            "/Reservations/Index");

        return (true, "Reservation confirmed.");
    }

    public async Task<(bool Success, string Message)> UpdateStatusAsync(int reservationId, ReservationStatus status, string staffId)
    {
        var reservation = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId);
        if (reservation == null)
            return (false, "Reservation not found.");

        reservation.Status = status;
        if (status == ReservationStatus.Confirmed)
        {
            reservation.ConfirmedAt = DateTime.UtcNow;
            reservation.ConfirmedByStaffId = staffId;
        }
        await _context.SaveChangesAsync();

        await _notificationService.NotifyAsync(
            reservation.CustomerId,
            "Reservation status updated",
            $"Your reservation is now marked as {status}.",
            NotificationType.Reservation,
            "/Reservations/Index");

        return (true, "Status updated.");
    }

    public async Task<List<Reservation>> GetCustomerReservationsAsync(string customerId)
    {
        return await _context.Reservations
            .Include(r => r.Table).ThenInclude(t => t!.DiningArea)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.ReservationDateTime)
            .ToListAsync();
    }

    public async Task<List<Reservation>> GetReservationsForDateAsync(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);
        return await _context.Reservations
            .Include(r => r.Customer)
            .Include(r => r.Table).ThenInclude(t => t!.DiningArea)
            .Where(r => r.ReservationDateTime >= start && r.ReservationDateTime < end)
            .OrderBy(r => r.ReservationDateTime)
            .ToListAsync();
    }

    public async Task<Reservation?> GetByIdAsync(int id)
    {
        return await _context.Reservations
            .Include(r => r.Table).ThenInclude(t => t!.DiningArea)
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}
