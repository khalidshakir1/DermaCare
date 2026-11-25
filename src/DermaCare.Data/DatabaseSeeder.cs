using DermaCare.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DermaCare.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(DermaDbContext context, CancellationToken ct = default)
    {
        await context.Database.MigrateAsync(ct);

        if (!await context.Roles.AnyAsync(ct))
        {
            context.Roles.AddRange(
                new Role { RoleName = "Admin" },
                new Role { RoleName = "Doctor" },
                new Role { RoleName = "Receptionist" }
            );
            await context.SaveChangesAsync(ct);
        }

        if (!await context.Users.AnyAsync(ct))
        {
            var adminRole = await context.Roles.FirstAsync(r => r.RoleName == "Admin", ct);
            var recepRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Receptionist", ct);
            context.Users.Add(new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                RoleId = adminRole.RoleId,
                FullName = "Doctor / Clinic Administrator",
                IsActive = true
            });
            if (recepRole is not null)
            {
                context.Users.Add(new User
                {
                    Username = "receptionist",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Recep@123"),
                    RoleId = recepRole.RoleId,
                    FullName = "Front Desk Receptionist",
                    IsActive = true
                });
            }
            await context.SaveChangesAsync(ct);
        }

        if (!await context.ClinicProfiles.AnyAsync(ct))
        {
            context.ClinicProfiles.Add(new ClinicProfile
            {
                ClinicName = "DermaCare Dermatology Clinic",
                Address = "Gulberg III, Lahore, Pakistan",
                Phone = "+92-42-3571-2000",
                Email = "contact@dermacare.pk"
            });
            await context.SaveChangesAsync(ct);
        }

        if (!await context.Services.AnyAsync(ct))
        {
            context.Services.AddRange(
                new ServiceItem { ServiceName = "General Dermatology Consultation", DefaultPrice = 2500.00m, Category = "Consultation" },
                new ServiceItem { ServiceName = "Follow-Up Visit", DefaultPrice = 1500.00m, Category = "Consultation" },
                new ServiceItem { ServiceName = "Chemical Peel", DefaultPrice = 6000.00m, Category = "Procedure" },
                new ServiceItem { ServiceName = "Laser Skin Therapy", DefaultPrice = 12000.00m, Category = "Procedure" },
                new ServiceItem { ServiceName = "Skin Biopsy", DefaultPrice = 8500.00m, Category = "Procedure" }
            );
            await context.SaveChangesAsync(ct);
        }

        if (!await context.Medicines.AnyAsync(ct))
        {
            context.Medicines.AddRange(
                new Medicine { Name = "Adapalene 0.1% Gel", GenericName = "Adapalene", Category = "Gel", Unit = "Tube", MinStockLevel = 10, PurchasePrice = 420m, SellingPrice = 550m, CurrentStock = 25 },
                new Medicine { Name = "Hydrocortisone 1% Cream", GenericName = "Hydrocortisone", Category = "Cream", Unit = "Tube", MinStockLevel = 10, PurchasePrice = 180m, SellingPrice = 250m, CurrentStock = 30 },
                new Medicine { Name = "Ketoconazole 2% Shampoo", GenericName = "Ketoconazole", Category = "Shampoo", Unit = "Bottle", MinStockLevel = 8, PurchasePrice = 520m, SellingPrice = 680m, CurrentStock = 15 },
                new Medicine { Name = "Isotretinoin 20mg", GenericName = "Isotretinoin", Category = "Capsule", Unit = "Pack", MinStockLevel = 10, PurchasePrice = 1100m, SellingPrice = 1450m, CurrentStock = 12 },
                new Medicine { Name = "Sunblock SPF 50+ Lotion", GenericName = "Broad Spectrum SPF 50+", Category = "Lotion", Unit = "Bottle", MinStockLevel = 12, PurchasePrice = 1350m, SellingPrice = 1800m, CurrentStock = 8 }
            );
            await context.SaveChangesAsync(ct);
        }
    }
}
