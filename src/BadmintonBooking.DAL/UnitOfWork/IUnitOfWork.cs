using BadmintonBooking.DAL.Repositories;
using BadmintonBooking.Domain.Entities;

namespace BadmintonBooking.DAL.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Facility> Facilities { get; }
    IRepository<Court> Courts { get; }
    IRepository<TimeSlot> TimeSlots { get; }
    IRepository<Booking> Bookings { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

