using BadmintonBooking.DAL.Repositories;
using BadmintonBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace BadmintonBooking.DAL.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    private IRepository<User>? _users;
    private IRepository<Facility>? _facilities;
    private IRepository<Court>? _courts;
    private IRepository<TimeSlot>? _timeSlots;
    private IRepository<Booking>? _bookings;
    private IRepository<Wallet>? _wallets;
    private IRepository<WalletTransaction>? _walletTransactions;
    private IRepository<PaymentOrder>? _paymentOrders;
    private IRepository<BankTransaction>? _bankTransactions;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Facility> Facilities => _facilities ??= new Repository<Facility>(_context);
    public IRepository<Court> Courts => _courts ??= new Repository<Court>(_context);
    public IRepository<TimeSlot> TimeSlots => _timeSlots ??= new Repository<TimeSlot>(_context);
    public IRepository<Booking> Bookings => _bookings ??= new Repository<Booking>(_context);
    public IRepository<Wallet> Wallets => _wallets ??= new Repository<Wallet>(_context);
    public IRepository<WalletTransaction> WalletTransactions => _walletTransactions ??= new Repository<WalletTransaction>(_context);
    public IRepository<PaymentOrder> PaymentOrders => _paymentOrders ??= new Repository<PaymentOrder>(_context);
    public IRepository<BankTransaction> BankTransactions => _bankTransactions ??= new Repository<BankTransaction>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
