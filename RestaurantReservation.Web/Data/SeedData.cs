using Microsoft.AspNetCore.Identity;
using RestaurantReservation.Web.Models.Entities;

namespace RestaurantReservation.Web.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Create roles
        string[] roles = { "SuperAdmin", "RestaurantManager", "BranchManager", "Guest" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Create SuperAdmin user
        var superAdminEmail = "admin@restaurant-reservation.com";
        if (await userManager.FindByEmailAsync(superAdminEmail) == null)
        {
            var superAdmin = new ApplicationUser
            {
                UserName = "superadmin",
                Email = superAdminEmail,
                FirstName = "Super",
                LastName = "Admin",
                EmailConfirmed = true,
                IsActive = true
            };
            
            var result = await userManager.CreateAsync(superAdmin, "Admin@123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
            }
        }

        // Check if data already exists
        if (context.Restaurants.Any())
        {
            return;
        }

        // Create sample restaurants
        var restaurant1 = new Restaurant
        {
            Name = "Gourmet Paradise",
            Description = "Experience fine dining at its finest with our world-class cuisine and exceptional service.",
            Website = "https://gourmetparadise.example.com",
            Email = "info@gourmetparadise.example.com",
            LogoUrl = "/uploads/restaurant1-logo.png",
            IsActive = true
        };

        var restaurant2 = new Restaurant
        {
            Name = "Urban Bites",
            Description = "Modern casual dining with a focus on locally-sourced ingredients and creative dishes.",
            Website = "https://urbanbites.example.com",
            Email = "hello@urbanbites.example.com",
            LogoUrl = "/uploads/restaurant2-logo.png",
            IsActive = true
        };

        context.Restaurants.AddRange(restaurant1, restaurant2);
        await context.SaveChangesAsync();

        // Create branches for Restaurant 1
        var branch1 = new Branch
        {
            RestaurantId = restaurant1.Id,
            Name = "Gourmet Paradise - Downtown",
            Description = "Our flagship location in the heart of the city, featuring stunning views and an elegant atmosphere.",
            Address = "123 Main Street, Suite 100",
            City = "New York",
            State = "NY",
            ZipCode = "10001",
            Country = "USA",
            Latitude = 40.7128,
            Longitude = -74.0060,
            Phone = "+1-212-555-0100",
            Email = "downtown@gourmetparadise.example.com",
            Website = "https://gourmetparadise.example.com/downtown",
            Cuisine = "French, Mediterranean",
            Capacity = 120,
            Area = "Financial District",
            HasParking = true,
            PaymentOptions = "Credit Card, Debit Card, Cash, Apple Pay",
            DressCode = "Smart Casual",
            IsAccessible = true,
            IsChildFriendly = true,
            OperatingHoursJson = @"{
                ""monday"": {""open"": ""11:00"", ""close"": ""22:00""},
                ""tuesday"": {""open"": ""11:00"", ""close"": ""22:00""},
                ""wednesday"": {""open"": ""11:00"", ""close"": ""22:00""},
                ""thursday"": {""open"": ""11:00"", ""close"": ""23:00""},
                ""friday"": {""open"": ""11:00"", ""close"": ""23:00""},
                ""saturday"": {""open"": ""10:00"", ""close"": ""23:00""},
                ""sunday"": {""open"": ""10:00"", ""close"": ""21:00""}
            }",
            ClosedDaysJson = @"[""2024-12-25"", ""2024-01-01""]",
            BookingIntervalMinutes = 30,
            CancellationPolicyHours = 24,
            MinimumCharge = 50,
            RequireDeposit = true,
            DepositAmount = 25,
            PhotosJson = @"[""/uploads/branch1-1.jpg"", ""/uploads/branch1-2.jpg"", ""/uploads/branch1-3.jpg""]",
            IsActive = true
        };

        var branch2 = new Branch
        {
            RestaurantId = restaurant1.Id,
            Name = "Gourmet Paradise - Midtown",
            Description = "A cozy neighborhood spot perfect for intimate dinners and business meetings.",
            Address = "456 Park Avenue",
            City = "New York",
            State = "NY",
            ZipCode = "10022",
            Country = "USA",
            Latitude = 40.7589,
            Longitude = -73.9851,
            Phone = "+1-212-555-0200",
            Email = "midtown@gourmetparadise.example.com",
            Cuisine = "French, Mediterranean",
            Capacity = 80,
            Area = "Midtown Manhattan",
            HasParking = false,
            PaymentOptions = "Credit Card, Debit Card, Cash",
            DressCode = "Casual",
            IsAccessible = true,
            IsChildFriendly = false,
            OperatingHoursJson = @"{
                ""monday"": {""open"": ""12:00"", ""close"": ""22:00""},
                ""tuesday"": {""open"": ""12:00"", ""close"": ""22:00""},
                ""wednesday"": {""open"": ""12:00"", ""close"": ""22:00""},
                ""thursday"": {""open"": ""12:00"", ""close"": ""22:00""},
                ""friday"": {""open"": ""12:00"", ""close"": ""23:00""},
                ""saturday"": {""open"": ""11:00"", ""close"": ""23:00""},
                ""sunday"": {""closed"": true}
            }",
            BookingIntervalMinutes = 30,
            CancellationPolicyHours = 12,
            PhotosJson = @"[""/uploads/branch2-1.jpg"", ""/uploads/branch2-2.jpg""]",
            IsActive = true
        };

        var branch3 = new Branch
        {
            RestaurantId = restaurant2.Id,
            Name = "Urban Bites - Brooklyn",
            Description = "Trendy Brooklyn location with rooftop dining and craft cocktails.",
            Address = "789 Bedford Avenue",
            City = "Brooklyn",
            State = "NY",
            ZipCode = "11211",
            Country = "USA",
            Latitude = 40.7182,
            Longitude = -73.9571,
            Phone = "+1-718-555-0100",
            Email = "brooklyn@urbanbites.example.com",
            Cuisine = "American, Fusion",
            Capacity = 100,
            Area = "Williamsburg",
            HasParking = true,
            PaymentOptions = "Credit Card, Debit Card, Cash, Venmo",
            DressCode = "Casual",
            IsAccessible = true,
            IsChildFriendly = true,
            OperatingHoursJson = @"{
                ""monday"": {""open"": ""11:00"", ""close"": ""23:00""},
                ""tuesday"": {""open"": ""11:00"", ""close"": ""23:00""},
                ""wednesday"": {""open"": ""11:00"", ""close"": ""23:00""},
                ""thursday"": {""open"": ""11:00"", ""close"": ""00:00""},
                ""friday"": {""open"": ""11:00"", ""close"": ""01:00""},
                ""saturday"": {""open"": ""10:00"", ""close"": ""01:00""},
                ""sunday"": {""open"": ""10:00"", ""close"": ""22:00""}
            }",
            BookingIntervalMinutes = 15,
            CancellationPolicyHours = 6,
            PhotosJson = @"[""/uploads/branch3-1.jpg"", ""/uploads/branch3-2.jpg"", ""/uploads/branch3-3.jpg""]",
            IsActive = true
        };

        context.Branches.AddRange(branch1, branch2, branch3);
        await context.SaveChangesAsync();

        // Create tables for each branch
        var tables = new List<Table>();
        
        // Branch 1 tables
        for (int i = 1; i <= 15; i++)
        {
            tables.Add(new Table
            {
                BranchId = branch1.Id,
                TableNumber = $"T{i:D2}",
                MinCapacity = i <= 5 ? 2 : (i <= 10 ? 4 : 6),
                MaxCapacity = i <= 5 ? 2 : (i <= 10 ? 6 : 10),
                LocationType = i <= 5 ? TableLocationType.Indoor : (i <= 10 ? TableLocationType.Outdoor : TableLocationType.Terrace),
                LayoutJson = $@"{{""x"": {(i % 5) * 100}, ""y"": {(i / 5) * 100}, ""width"": 80, ""height"": 80}}",
                IsActive = true
            });
        }

        // Branch 2 tables
        for (int i = 1; i <= 10; i++)
        {
            tables.Add(new Table
            {
                BranchId = branch2.Id,
                TableNumber = $"M{i:D2}",
                MinCapacity = i <= 4 ? 2 : 4,
                MaxCapacity = i <= 4 ? 2 : 8,
                LocationType = i <= 6 ? TableLocationType.Indoor : TableLocationType.PrivateRoom,
                LayoutJson = $@"{{""x"": {(i % 4) * 120}, ""y"": {(i / 4) * 100}, ""width"": 100, ""height"": 80}}",
                IsActive = true
            });
        }

        // Branch 3 tables
        for (int i = 1; i <= 12; i++)
        {
            tables.Add(new Table
            {
                BranchId = branch3.Id,
                TableNumber = $"B{i:D2}",
                MinCapacity = i <= 4 ? 2 : (i <= 8 ? 4 : 6),
                MaxCapacity = i <= 4 ? 4 : (i <= 8 ? 6 : 12),
                LocationType = i <= 4 ? TableLocationType.Bar : (i <= 8 ? TableLocationType.Indoor : TableLocationType.Outdoor),
                LayoutJson = $@"{{""x"": {(i % 4) * 110}, ""y"": {(i / 4) * 90}, ""width"": 90, ""height"": 70}}",
                IsActive = true
            });
        }

        context.Tables.AddRange(tables);
        await context.SaveChangesAsync();

        // Create time slots
        var timeSlots = new List<TimeSlot>();
        
        // Lunch slots (11:00 - 14:00)
        foreach (var branch in new[] { branch1, branch2, branch3 })
        {
            for (int hour = 11; hour < 14; hour++)
            {
                timeSlots.Add(new TimeSlot
                {
                    BranchId = branch.Id,
                    MealType = MealType.Lunch,
                    StartTime = new TimeSpan(hour, 0, 0),
                    EndTime = new TimeSpan(hour, 30, 0),
                    MaxBookings = 5,
                    IsActive = true
                });
                timeSlots.Add(new TimeSlot
                {
                    BranchId = branch.Id,
                    MealType = MealType.Lunch,
                    StartTime = new TimeSpan(hour, 30, 0),
                    EndTime = new TimeSpan(hour + 1, 0, 0),
                    MaxBookings = 5,
                    IsActive = true
                });
            }
        }

        // Dinner slots (18:00 - 22:00)
        foreach (var branch in new[] { branch1, branch2, branch3 })
        {
            for (int hour = 18; hour < 22; hour++)
            {
                timeSlots.Add(new TimeSlot
                {
                    BranchId = branch.Id,
                    MealType = MealType.Dinner,
                    StartTime = new TimeSpan(hour, 0, 0),
                    EndTime = new TimeSpan(hour, 30, 0),
                    MaxBookings = 8,
                    IsActive = true
                });
                timeSlots.Add(new TimeSlot
                {
                    BranchId = branch.Id,
                    MealType = MealType.Dinner,
                    StartTime = new TimeSpan(hour, 30, 0),
                    EndTime = new TimeSpan(hour + 1, 0, 0),
                    MaxBookings = 8,
                    IsActive = true
                });
            }
        }

        context.TimeSlots.AddRange(timeSlots);
        await context.SaveChangesAsync();

        // Create menus
        var menu1 = new Menu
        {
            RestaurantId = restaurant1.Id,
            Name = "Main Menu",
            Description = "Our signature dishes featuring the finest ingredients",
            IsActive = true
        };

        var menu2 = new Menu
        {
            BranchId = branch3.Id,
            Name = "Urban Bites Menu",
            Description = "Creative American cuisine with a modern twist",
            IsActive = true
        };

        context.Menus.AddRange(menu1, menu2);
        await context.SaveChangesAsync();

        // Create menu categories
        var appetizers = new MenuCategory
        {
            MenuId = menu1.Id,
            Name = "Appetizers",
            Description = "Start your meal with these delicious options",
            DisplayOrder = 1,
            IsActive = true
        };

        var mainCourses = new MenuCategory
        {
            MenuId = menu1.Id,
            Name = "Main Courses",
            Description = "Our signature main dishes",
            DisplayOrder = 2,
            IsActive = true
        };

        var desserts = new MenuCategory
        {
            MenuId = menu1.Id,
            Name = "Desserts",
            Description = "Sweet endings to your meal",
            DisplayOrder = 3,
            IsActive = true
        };

        var starters = new MenuCategory
        {
            MenuId = menu2.Id,
            Name = "Starters",
            Description = "Perfect for sharing",
            DisplayOrder = 1,
            IsActive = true
        };

        var mains = new MenuCategory
        {
            MenuId = menu2.Id,
            Name = "Mains",
            Description = "Hearty main dishes",
            DisplayOrder = 2,
            IsActive = true
        };

        context.MenuCategories.AddRange(appetizers, mainCourses, desserts, starters, mains);
        await context.SaveChangesAsync();

        // Create menu items
        var menuItems = new List<MenuItem>
        {
            // Appetizers
            new MenuItem
            {
                CategoryId = appetizers.Id,
                Name = "French Onion Soup",
                Description = "Classic soup with caramelized onions and gruyère cheese",
                Price = 14.00m,
                IsVegetarian = true,
                ContainsDairy = true,
                CalorieCount = 350,
                DisplayOrder = 1,
                IsPopular = true
            },
            new MenuItem
            {
                CategoryId = appetizers.Id,
                Name = "Tuna Tartare",
                Description = "Fresh ahi tuna with avocado, sesame, and soy glaze",
                Price = 22.00m,
                IsGlutenFree = true,
                CalorieCount = 280,
                DisplayOrder = 2,
                IsPopular = true
            },
            new MenuItem
            {
                CategoryId = appetizers.Id,
                Name = "Burrata Salad",
                Description = "Creamy burrata with heirloom tomatoes and basil",
                Price = 18.00m,
                IsVegetarian = true,
                IsGlutenFree = true,
                ContainsDairy = true,
                CalorieCount = 320,
                DisplayOrder = 3
            },
            
            // Main Courses
            new MenuItem
            {
                CategoryId = mainCourses.Id,
                Name = "Filet Mignon",
                Description = "8oz prime beef with red wine reduction and truffle mash",
                Price = 52.00m,
                IsGlutenFree = true,
                CalorieCount = 680,
                DisplayOrder = 1,
                IsPopular = true
            },
            new MenuItem
            {
                CategoryId = mainCourses.Id,
                Name = "Pan-Seared Salmon",
                Description = "Atlantic salmon with lemon butter sauce and seasonal vegetables",
                Price = 38.00m,
                IsGlutenFree = true,
                ContainsDairy = true,
                CalorieCount = 520,
                DisplayOrder = 2
            },
            new MenuItem
            {
                CategoryId = mainCourses.Id,
                Name = "Vegetable Risotto",
                Description = "Arborio rice with seasonal vegetables and parmesan",
                Price = 28.00m,
                IsVegetarian = true,
                IsGlutenFree = true,
                ContainsDairy = true,
                CalorieCount = 450,
                DisplayOrder = 3
            },
            
            // Desserts
            new MenuItem
            {
                CategoryId = desserts.Id,
                Name = "Crème Brûlée",
                Description = "Classic vanilla custard with caramelized sugar",
                Price = 12.00m,
                IsVegetarian = true,
                IsGlutenFree = true,
                ContainsDairy = true,
                CalorieCount = 380,
                DisplayOrder = 1,
                IsPopular = true
            },
            new MenuItem
            {
                CategoryId = desserts.Id,
                Name = "Chocolate Lava Cake",
                Description = "Warm chocolate cake with molten center and vanilla ice cream",
                Price = 14.00m,
                IsVegetarian = true,
                ContainsDairy = true,
                CalorieCount = 520,
                DisplayOrder = 2
            },
            
            // Urban Bites Starters
            new MenuItem
            {
                CategoryId = starters.Id,
                Name = "Truffle Fries",
                Description = "Crispy fries with truffle oil and parmesan",
                Price = 12.00m,
                IsVegetarian = true,
                ContainsDairy = true,
                CalorieCount = 420,
                DisplayOrder = 1,
                IsPopular = true
            },
            new MenuItem
            {
                CategoryId = starters.Id,
                Name = "Wings Platter",
                Description = "Crispy wings with choice of buffalo, BBQ, or honey garlic",
                Price = 16.00m,
                CalorieCount = 580,
                DisplayOrder = 2
            },
            
            // Urban Bites Mains
            new MenuItem
            {
                CategoryId = mains.Id,
                Name = "Signature Burger",
                Description = "Angus beef patty with aged cheddar, bacon, and special sauce",
                Price = 22.00m,
                ContainsDairy = true,
                CalorieCount = 850,
                DisplayOrder = 1,
                IsPopular = true
            },
            new MenuItem
            {
                CategoryId = mains.Id,
                Name = "Impossible Burger",
                Description = "Plant-based patty with all the fixings",
                Price = 24.00m,
                IsVegan = true,
                CalorieCount = 720,
                DisplayOrder = 2
            }
        };

        context.MenuItems.AddRange(menuItems);
        await context.SaveChangesAsync();

        // Create offers
        var offers = new List<Offer>
        {
            new Offer
            {
                RestaurantId = restaurant1.Id,
                Title = "Happy Hour Special",
                Description = "20% off all appetizers and drinks from 4-6 PM",
                Type = OfferType.Percentage,
                DiscountValue = 20,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(3),
                ValidFromTime = new TimeSpan(16, 0, 0),
                ValidToTime = new TimeSpan(18, 0, 0),
                ValidDaysJson = @"[1, 2, 3, 4, 5]",
                IsActive = true
            },
            new Offer
            {
                BranchId = branch3.Id,
                Title = "Weekend Brunch Deal",
                Description = "$10 off when you order brunch for 2 or more",
                Type = OfferType.FixedAmount,
                DiscountValue = 10,
                MinimumOrderAmount = 30,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(2),
                ValidDaysJson = @"[0, 6]",
                IsActive = true
            },
            new Offer
            {
                RestaurantId = restaurant1.Id,
                Title = "Loyalty Bonus",
                Description = "Earn double loyalty points on all bookings this month",
                Type = OfferType.LoyaltyBonus,
                LoyaltyPointsBonus = 100,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(30),
                IsActive = true
            }
        };

        context.Offers.AddRange(offers);
        await context.SaveChangesAsync();

        // Create coupons
        var coupons = new List<Coupon>
        {
            new Coupon
            {
                Code = "WELCOME10",
                Description = "10% off your first booking",
                Type = OfferType.Percentage,
                DiscountValue = 10,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddYears(1),
                MaxUsages = 1000,
                IsActive = true
            },
            new Coupon
            {
                Code = "SAVE20",
                RestaurantId = restaurant1.Id,
                Description = "$20 off orders over $100",
                Type = OfferType.FixedAmount,
                DiscountValue = 20,
                MinimumOrderAmount = 100,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(6),
                MaxUsages = 500,
                IsActive = true
            }
        };

        context.Coupons.AddRange(coupons);
        await context.SaveChangesAsync();

        // Create sample reviews
        var reviews = new List<Review>
        {
            new Review
            {
                BranchId = branch1.Id,
                GuestName = "John D.",
                Rating = 5,
                Comment = "Absolutely amazing experience! The food was exquisite and the service impeccable.",
                FoodRating = 5,
                ServiceRating = 5,
                AmbianceRating = 5,
                ValueRating = 4,
                IsApproved = true,
                IsVisible = true
            },
            new Review
            {
                BranchId = branch1.Id,
                GuestName = "Sarah M.",
                Rating = 4,
                Comment = "Great food and atmosphere. A bit pricey but worth it for special occasions.",
                FoodRating = 5,
                ServiceRating = 4,
                AmbianceRating = 5,
                ValueRating = 3,
                IsApproved = true,
                IsVisible = true
            },
            new Review
            {
                BranchId = branch3.Id,
                GuestName = "Mike T.",
                Rating = 5,
                Comment = "Love this place! Great vibe and the burger is to die for.",
                FoodRating = 5,
                ServiceRating = 5,
                AmbianceRating = 5,
                ValueRating = 5,
                IsApproved = true,
                IsVisible = true
            }
        };

        context.Reviews.AddRange(reviews);
        await context.SaveChangesAsync();

        // Create Restaurant Manager user
        var managerEmail = "manager@gourmetparadise.example.com";
        if (await userManager.FindByEmailAsync(managerEmail) == null)
        {
            var manager = new ApplicationUser
            {
                UserName = "restaurantmanager",
                Email = managerEmail,
                FirstName = "Restaurant",
                LastName = "Manager",
                EmailConfirmed = true,
                IsActive = true,
                RestaurantId = restaurant1.Id
            };
            
            var result = await userManager.CreateAsync(manager, "Manager@123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(manager, "RestaurantManager");
            }
        }

        // Create Branch Manager user
        var branchManagerEmail = "branch@gourmetparadise.example.com";
        if (await userManager.FindByEmailAsync(branchManagerEmail) == null)
        {
            var branchManager = new ApplicationUser
            {
                UserName = "branchmanager",
                Email = branchManagerEmail,
                FirstName = "Branch",
                LastName = "Manager",
                EmailConfirmed = true,
                IsActive = true,
                BranchId = branch1.Id
            };
            
            var result = await userManager.CreateAsync(branchManager, "Branch@123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(branchManager, "BranchManager");
            }
        }

        // Create sample guest user
        var guestEmail = "guest@example.com";
        if (await userManager.FindByEmailAsync(guestEmail) == null)
        {
            var guest = new ApplicationUser
            {
                UserName = "guest",
                Email = guestEmail,
                FirstName = "Guest",
                LastName = "User",
                EmailConfirmed = true,
                IsActive = true,
                LoyaltyPoints = 150
            };
            
            var result = await userManager.CreateAsync(guest, "Guest@123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(guest, "Guest");
            }
        }
    }
}
