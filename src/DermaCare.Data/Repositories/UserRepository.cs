using DermaCare.Core.Interfaces;
using DermaCare.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DermaCare.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DermaDbContext _context;

    public UserRepository(DermaDbContext context) => _context = context;

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        _context.Users.Include(u => u.Role).Include(u => u.DoctorProfile).FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<User?> GetByIdAsync(int userId, CancellationToken ct = default) =>
        _context.Users.Include(u => u.Role).Include(u => u.DoctorProfile).FirstOrDefaultAsync(u => u.UserId == userId, ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Users.Include(u => u.Role).Include(u => u.DoctorProfile).OrderBy(u => u.FullName).ToListAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(ct);
    }

    public Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken ct = default) =>
        _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName, ct);

    public async Task AddDoctorUserAsync(User user, Doctor doctor, CancellationToken ct = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
        doctor.UserId = user.UserId;
        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync(ct);
    }
}
