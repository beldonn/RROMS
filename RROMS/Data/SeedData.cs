using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RROMS.Models;

namespace RROMS.Data;

/// <summary>
/// Runs once at startup (see Program.cs). Applies any pending EF Core migrations,
/// then seeds roles, two demo accounts, and enough reference data (dining areas,
/// tables, menu, operating hours, FAQs) for the app to be usable immediately after
/// `dotnet run` without any manual data entry.
/// </summary>
public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        await context.Database.MigrateAsync();

        // ---------------- Roles ----------------
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // ---------------- Admin account ----------------
        var adminEmail = config["RestaurantSettings:SeedAdminEmail"] ?? "admin@rroms.local";
        var adminPassword = config["RestaurantSettings:SeedAdminPassword"] ?? "Admin@12345";
        var admin = await EnsureUserAsync(userManager, adminEmail, adminPassword, "System Administrator", Roles.Administrator);

        // ---------------- Staff account ----------------
        var staffEmail = config["RestaurantSettings:SeedStaffEmail"] ?? "staff@rroms.local";
        var staffPassword = config["RestaurantSettings:SeedStaffPassword"] ?? "Staff@12345";
        await EnsureUserAsync(userManager, staffEmail, staffPassword, "Front of House Staff", Roles.Staff);

        // ---------------- Demo customer account ----------------
        await EnsureUserAsync(userManager, "customer@rroms.local", "Customer@12345", "Demo Customer", Roles.Customer);

        // ---------------- Restaurant setting (single row) ----------------
        if (!await context.RestaurantSettings.AnyAsync())
        {
            context.RestaurantSettings.Add(new RestaurantSetting
            {
                RestaurantName = "Ember & Thyme",
                Address = "12 Market Lane, Springfield",
                Phone = "+1 555-0142",
                Email = "hello@emberandthyme.local",
                Description = "Modern seasonal cooking over an open flame, served in a warm dining room built for long, unhurried meals.",
                ReservationDurationMinutes = 90,
                MaxAdvanceBookingDays = 60,
                TaxRatePercent = 6.0m
            });
            await context.SaveChangesAsync();
        }

        // ---------------- Operating hours ----------------
        if (!await context.OperatingHours.AnyAsync())
        {
            var hours = new List<OperatingHours>();
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                var isWeekend = day is DayOfWeek.Friday or DayOfWeek.Saturday;
                hours.Add(new OperatingHours
                {
                    DayOfWeek = day,
                    OpenTime = new TimeSpan(11, 0, 0),
                    CloseTime = isWeekend ? new TimeSpan(23, 0, 0) : new TimeSpan(22, 0, 0),
                    IsClosed = false
                });
            }
            context.OperatingHours.AddRange(hours);
            await context.SaveChangesAsync();
        }

        // ---------------- Dining areas + tables ----------------
        if (!await context.DiningAreas.AnyAsync())
        {
            var mainHall = new DiningArea { Name = "Main Hall", Description = "Our lively main dining room." };
            var patio = new DiningArea { Name = "Garden Patio", Description = "Open-air seating, weather permitting." };
            var privateRoom = new DiningArea { Name = "Private Room", Description = "Semi-private space for larger parties." };
            context.DiningAreas.AddRange(mainHall, patio, privateRoom);
            await context.SaveChangesAsync();

            var tables = new List<RestaurantTable>
            {
                new() { TableNumber = "M1", DiningAreaId = mainHall.Id, Capacity = 2 },
                new() { TableNumber = "M2", DiningAreaId = mainHall.Id, Capacity = 2 },
                new() { TableNumber = "M3", DiningAreaId = mainHall.Id, Capacity = 4 },
                new() { TableNumber = "M4", DiningAreaId = mainHall.Id, Capacity = 4 },
                new() { TableNumber = "M5", DiningAreaId = mainHall.Id, Capacity = 6 },
                new() { TableNumber = "P1", DiningAreaId = patio.Id, Capacity = 2 },
                new() { TableNumber = "P2", DiningAreaId = patio.Id, Capacity = 4 },
                new() { TableNumber = "P3", DiningAreaId = patio.Id, Capacity = 4 },
                new() { TableNumber = "R1", DiningAreaId = privateRoom.Id, Capacity = 10 },
                new() { TableNumber = "R2", DiningAreaId = privateRoom.Id, Capacity = 12 },
            };
            context.RestaurantTables.AddRange(tables);
            await context.SaveChangesAsync();
        }

        // ---------------- Menu categories + items ----------------
        if (!await context.MenuCategories.AnyAsync())
        {
            var starters = new MenuCategory { Name = "Starters", DisplayOrder = 1 };
            var mains = new MenuCategory { Name = "Mains", DisplayOrder = 2 };
            var desserts = new MenuCategory { Name = "Desserts", DisplayOrder = 3 };
            var drinks = new MenuCategory { Name = "Drinks", DisplayOrder = 4 };
            context.MenuCategories.AddRange(starters, mains, desserts, drinks);
            await context.SaveChangesAsync();

            var items = new List<MenuItem>
            {
                new() { Name = "Charred Corn Soup", Description = "Roasted sweetcorn, smoked paprika oil, chive.", Price = 8.50m, MenuCategoryId = starters.Id, IsVegetarian = true, Calories = 220 },
                new() { Name = "Beef Tartare", Description = "Hand-cut beef, capers, pickled shallot, rye crisp.", Price = 13.00m, MenuCategoryId = starters.Id, Calories = 310 },
                new() { Name = "Charred Octopus", Description = "Smoked paprika, crushed potato, salsa verde.", Price = 15.50m, MenuCategoryId = starters.Id, IsSpicy = true, Calories = 340 },
                new() { Name = "Heirloom Tomato Salad", Description = "Burrata, basil oil, aged balsamic.", Price = 11.00m, MenuCategoryId = starters.Id, IsVegetarian = true, Calories = 260 },

                new() { Name = "Flame-Grilled Ribeye", Description = "28-day aged ribeye, bone marrow butter, fries.", Price = 34.00m, MenuCategoryId = mains.Id, Calories = 890 },
                new() { Name = "Pan-Seared Salmon", Description = "Crisp skin salmon, charred leeks, brown butter.", Price = 26.00m, MenuCategoryId = mains.Id, Calories = 610 },
                new() { Name = "Wild Mushroom Risotto", Description = "Arborio rice, parmesan, black truffle.", Price = 21.00m, MenuCategoryId = mains.Id, IsVegetarian = true, Calories = 540 },
                new() { Name = "Roasted Half Chicken", Description = "Herb butter, charred lemon, jus.", Price = 24.00m, MenuCategoryId = mains.Id, Calories = 720 },
                new() { Name = "Spiced Lamb Shoulder", Description = "Slow-cooked, harissa, minted yoghurt.", Price = 29.00m, MenuCategoryId = mains.Id, IsSpicy = true, Calories = 760 },
                new() { Name = "Charred Cauliflower Steak", Description = "Tahini, pomegranate, toasted seeds.", Price = 19.00m, MenuCategoryId = mains.Id, IsVegetarian = true, Calories = 430 },

                new() { Name = "Dark Chocolate Torte", Description = "70% cocoa, salted caramel, cocoa nib.", Price = 9.50m, MenuCategoryId = desserts.Id, IsVegetarian = true, Calories = 480 },
                new() { Name = "Burnt Basque Cheesecake", Description = "Vanilla bean, seasonal berry compote.", Price = 9.00m, MenuCategoryId = desserts.Id, IsVegetarian = true, Calories = 460 },
                new() { Name = "Citrus Panna Cotta", Description = "Blood orange, candied peel, shortbread.", Price = 8.50m, MenuCategoryId = desserts.Id, IsVegetarian = true, Calories = 380 },

                new() { Name = "House Old Fashioned", Description = "Bourbon, demerara, orange bitters.", Price = 12.00m, MenuCategoryId = drinks.Id, Calories = 180 },
                new() { Name = "Smoked Rosemary Sour", Description = "Gin, lemon, rosemary smoke.", Price = 12.50m, MenuCategoryId = drinks.Id, Calories = 190 },
                new() { Name = "Sparkling Elderflower", Description = "Non-alcoholic, elderflower, soda, mint.", Price = 6.00m, MenuCategoryId = drinks.Id, IsVegetarian = true, Calories = 90 },
                new() { Name = "House Red Blend (Glass)", Description = "Medium-bodied, notes of dark cherry and oak.", Price = 11.00m, MenuCategoryId = drinks.Id, Calories = 125 },
            };
            context.MenuItems.AddRange(items);
            await context.SaveChangesAsync();

            context.Promotions.Add(new Promotion
            {
                Title = "Weeknight Ribeye Special",
                Description = "15% off our Flame-Grilled Ribeye, Sunday to Thursday.",
                DiscountPercent = 15,
                MenuItemId = items.First(i => i.Name == "Flame-Grilled Ribeye").Id,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(30)
            });
            await context.SaveChangesAsync();
        }

        // ---------------- Announcement ----------------
        if (!await context.Announcements.AnyAsync())
        {
            context.Announcements.Add(new Announcement
            {
                Title = "Welcome to our new online booking system",
                Content = "You can now reserve a table, order ahead, and track your visit history all in one place. We look forward to welcoming you.",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(60),
                IsActive = true,
                CreatedByAdminId = admin?.Id
            });
            await context.SaveChangesAsync();
        }

        // ---------------- Chatbot FAQs ----------------
        if (!await context.ChatbotFaqs.AnyAsync())
        {
            context.ChatbotFaqs.AddRange(
                new ChatbotFaq { Question = "What are your opening hours?", Keywords = "hours,open,close,opening,closing,time", Answer = "We're open 11:00 AM to 10:00 PM Sunday-Thursday, and until 11:00 PM Friday-Saturday. You can see the full schedule on our Menu and Home pages.", Category = "Hours" },
                new ChatbotFaq { Question = "How do I make a reservation?", Keywords = "reservation,book,booking,table,reserve", Answer = "Go to Reservations > New Reservation, choose a date, time and party size, and we'll show you every table that's free at that moment.", Category = "Reservations" },
                new ChatbotFaq { Question = "Can I cancel or change my reservation?", Keywords = "cancel,change,modify,reschedule,edit reservation", Answer = "Yes - open My Reservations, select the booking, and choose Modify or Cancel. Changes are free up until the reservation time.", Category = "Reservations" },
                new ChatbotFaq { Question = "Do you take walk-ins?", Keywords = "walk-in,walkin,without reservation,no booking", Answer = "We do take walk-ins when tables are available, but booking ahead guarantees your seat, especially on weekends.", Category = "Reservations" },
                new ChatbotFaq { Question = "How do I order food online?", Keywords = "order,menu,food,delivery,takeaway,pickup", Answer = "Browse Menu, add dishes to your cart, then check out as Dine-in (linked to a reservation) or Takeaway. You'll be able to track the status in My Orders.", Category = "Orders" },
                new ChatbotFaq { Question = "What payment methods do you accept?", Keywords = "pay,payment,card,cash,online payment", Answer = "We accept cash at the restaurant, and card/online payment at checkout. Your receipt appears under My Orders once payment is confirmed.", Category = "Payments" },
                new ChatbotFaq { Question = "Do you have vegetarian options?", Keywords = "vegetarian,vegan,plant based,veggie", Answer = "Yes! Look for the vegetarian tag on the Menu page - we have starters, mains and desserts marked for you.", Category = "Menu" },
                new ChatbotFaq { Question = "How do I leave feedback?", Keywords = "feedback,review,rating,complaint,compliment", Answer = "After a visit, go to Feedback > Leave Feedback, pick the order or reservation, and rate your experience 1-5 stars with comments.", Category = "Feedback" }
            );
            await context.SaveChangesAsync();
        }

        logger.LogInformation("Database seeding completed.");
    }

    private static async Task<ApplicationUser?> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string fullName,
        string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null)
            return existing;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true,
            DateJoined = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);
            return user;
        }

        return null;
    }
}
