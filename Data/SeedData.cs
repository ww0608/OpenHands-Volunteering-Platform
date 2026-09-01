using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;

namespace OpenHandsVolunteerPlatform.Data
{
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

            // Create roles if they don't exist
            await CreateRoles(roleManager);

            // Seed all data (users, profiles, organizations, etc.)
            await SeedAllData(userManager, context);
        }

        private static async Task CreateRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "Organization", "Volunteer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task SeedAllData(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            // 1. Admin User
            await SeedAdminUser(userManager);

            // 2. Volunteer Users and Profiles
            await SeedVolunteers(userManager, context);

            // 3. Organization Users and Profiles
            await SeedOrganizations(userManager, context);

            // 4. Opportunities
            await SeedOpportunities(context);

            // 5. Applications
            await SeedApplications(context);

            // 6. Certificates
            await SeedCertificates(context);

            // 7. Follows
            await SeedFollows(context);

            // 8. Notifications
            await SeedNotifications(context);
        }

        private static async Task SeedAdminUser(UserManager<ApplicationUser> userManager)
        {
            var adminEmail = "admin@openhands.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now.AddDays(-60)
                };
                await userManager.CreateAsync(adminUser, "Admin@123");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        private static async Task SeedVolunteers(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            var volunteers = new[]
            {
                new { Email = "john.doe@example.com", Password = "Volunteer@123", Name = "John Doe", Age = 25, Phone = "012-3456789", Availability = AvailabilityType.Weekend },
                new { Email = "jane.smith@example.com", Password = "Volunteer@123", Name = "Jane Smith", Age = 28, Phone = "012-3456790", Availability = AvailabilityType.Weekday },
                new { Email = "mike.wilson@example.com", Password = "Volunteer@123", Name = "Mike Wilson", Age = 32, Phone = "012-3456791", Availability = AvailabilityType.Afternoon },
                new { Email = "sarah.johnson@example.com", Password = "Volunteer@123", Name = "Sarah Johnson", Age = 22, Phone = "012-3456792", Availability = AvailabilityType.Evening },
                new { Email = "david.lee@example.com", Password = "Volunteer@123", Name = "David Lee", Age = 35, Phone = "012-3456793", Availability = AvailabilityType.Weekday },
                new { Email = "emma.brown@example.com", Password = "Volunteer@123", Name = "Emma Brown", Age = 27, Phone = "012-3456794", Availability = AvailabilityType.Morning },
                new { Email = "alex.chen@example.com", Password = "Volunteer@123", Name = "Alex Chen", Age = 29, Phone = "012-3456795", Availability = AvailabilityType.Weekend },
                new { Email = "lisa.wong@example.com", Password = "Volunteer@123", Name = "Lisa Wong", Age = 24, Phone = "012-3456796", Availability = AvailabilityType.Weekday },
                new { Email = "daniel.tan@example.com", Password = "Volunteer@123", Name = "Daniel Tan", Age = 31, Phone = "012-3456797", Availability = AvailabilityType.Morning },
                new { Email = "michelle.ng@example.com", Password = "Volunteer@123", Name = "Michelle Ng", Age = 26, Phone = "012-3456798", Availability = AvailabilityType.Weekend }
            };

            foreach (var v in volunteers)
            {
                var existingUser = await userManager.FindByEmailAsync(v.Email);
                if (existingUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = v.Email,
                        Email = v.Email,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.Now.AddDays(-30)
                    };

                    var result = await userManager.CreateAsync(user, v.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Volunteer");

                        var volunteer = new Volunteer
                        {
                            VolunteerId = Guid.NewGuid().ToString(),
                            UserId = user.Id,
                            Name = v.Name,
                            Age = v.Age,
                            Phone = v.Phone,
                            Availability = v.Availability,
                            CreditLevel = CreditLevel.Growing,
                            TotalHours = 0,
                            CompletedCount = 0,
                            NoShowCount = 0
                        };
                        context.Volunteers.Add(volunteer);
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedOrganizations(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            var organizations = new[]
            {
            (Email: "habitat@example.com", Password: "Organization@123", Name: "Habitat for Humanity Malaysia",
             Mission: "Building homes and communities for those in need. We believe everyone deserves a decent place to live.",
             Phone: "03-12345678", Verified: true, VerifiedAt: (DateTime?)DateTime.Now.AddDays(-30)),

            (Email: "foodbank@example.com", Password: "Organization@123", Name: "Food Bank Malaysia",
             Mission: "Fighting hunger, feeding hope. We redistribute surplus food to those in need.",
             Phone: "03-12345679", Verified: true, VerifiedAt: (DateTime?)DateTime.Now.AddDays(-25)),

            (Email: "greenpeace@example.com", Password: "Organization@123", Name: "Green Peace Malaysia",
             Mission: "Environmental protection and conservation. Protecting our planet for future generations.",
             Phone: "03-12345680", Verified: true, VerifiedAt: (DateTime?)DateTime.Now.AddDays(-20)),

            (Email: "redcrescent@example.com", Password: "Organization@123", Name: "Red Crescent Society",
             Mission: "Humanitarian aid and disaster relief. Providing help in times of crisis.",
             Phone: "03-12345681", Verified: true, VerifiedAt: (DateTime?)DateTime.Now.AddDays(-15)),

            (Email: "animalcare@example.com", Password: "Organization@123", Name: "Animal Care Society",
             Mission: "Rescue and rehabilitation of stray animals. Giving abandoned animals a second chance.",
             Phone: "03-12345682", Verified: false, VerifiedAt: (DateTime?)null),

            (Email: "eduforall@example.com", Password: "Organization@123", Name: "Education For All",
             Mission: "Providing education opportunities to underprivileged children.",
             Phone: "03-12345683", Verified: true, VerifiedAt: (DateTime?)DateTime.Now.AddDays(-10)),

            (Email: "eldercare@example.com", Password: "Organization@123", Name: "Elder Care Foundation",
             Mission: "Supporting elderly citizens with care and companionship.",
             Phone: "03-12345684", Verified: true, VerifiedAt: (DateTime?)DateTime.Now.AddDays(-5))
        };

            foreach (var o in organizations)
            {
                // Check if user exists
                var existingUser = await userManager.FindByEmailAsync(o.Email);

                if (existingUser == null)
                {
                    // Create the user
                    var user = new ApplicationUser
                    {
                        UserName = o.Email,
                        Email = o.Email,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.Now.AddDays(-30)
                    };

                    var result = await userManager.CreateAsync(user, o.Password);

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Organization");
                        existingUser = user;
                    }
                    else
                    {
                        continue;
                    }
                }

                // Check if organization profile already exists for this user
                var existingOrg = await context.Organizations
                    .FirstOrDefaultAsync(org => org.UserId == existingUser.Id);

                if (existingOrg == null)
                {
                    // Create organization profile
                    var organization = new Organization
                    {
                        OrganizationId = Guid.NewGuid().ToString(),
                        UserId = existingUser.Id,
                        OrganizationName = o.Name,
                        Mission = o.Mission,
                        Phone = o.Phone,
                        VerificationStatus = o.Verified ? VerificationStatus.Approved : VerificationStatus.Pending,
                        LogoPath = "/images/default-logo.png",
                        VerificationDocumentPath = "/docs/sample-doc.pdf",
                        VerifiedAt = o.VerifiedAt,
                        CreatedAt = DateTime.Now.AddDays(-30)
                    };
                    context.Organizations.Add(organization);
                }
            }

            await context.SaveChangesAsync();
        }
        private static async Task SeedOpportunities(ApplicationDbContext context)
        {
            var organizations = await context.Organizations
                .Where(o => o.VerificationStatus == VerificationStatus.Approved)
                .ToListAsync();

            if (!organizations.Any()) return;

            var random = new Random();
            var opportunities = new List<Opportunity>();

            // Past opportunities (COMPLETE) - last 2 months
            for (int i = 1; i <= 12; i++)
            {
                var org = organizations[i % organizations.Count];
                var date = DateTime.Now.AddDays(-(i * 3));

                opportunities.Add(new Opportunity
                {
                    OpportunityId = Guid.NewGuid().ToString(),
                    OrganizationId = org.OrganizationId,
                    Title = GetPastOpportunityTitle(i),
                    Description = GetPastOpportunityDescription(i),
                    Date = date,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(13, 0, 0),
                    Location = GetLocation(i),
                    VolunteersNeeded = random.Next(10, 25),
                    ApplicationCloseDate = date.AddDays(-7),
                    Status = OpportunityOpenStatus.Open,
                    AutoCloseWhenFull = false,
                    IsEmergency = false,
                    MinimumCreditLevel = null
                });
            }

            // Upcoming opportunities (FUTURE) - next 2 months
            for (int i = 1; i <= 15; i++)
            {
                var org = organizations[i % organizations.Count];
                var date = DateTime.Now.AddDays(i * 2);

                opportunities.Add(new Opportunity
                {
                    OpportunityId = Guid.NewGuid().ToString(),
                    OrganizationId = org.OrganizationId,
                    Title = GetFutureOpportunityTitle(i),
                    Description = GetFutureOpportunityDescription(i),
                    Date = date,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(13, 0, 0),
                    Location = GetLocation(i + 10),
                    VolunteersNeeded = random.Next(15, 30),
                    ApplicationCloseDate = date.AddDays(-2),
                    Status = OpportunityOpenStatus.Open,
                    AutoCloseWhenFull = false,
                    IsEmergency = i % 5 == 0,
                    MinimumCreditLevel = i % 7 == 0 ? CreditLevel.Growing : null
                });
            }

            // Ongoing/Today's opportunities
            var todayOrg = organizations.FirstOrDefault();
            if (todayOrg != null)
            {
                opportunities.Add(new Opportunity
                {
                    OpportunityId = Guid.NewGuid().ToString(),
                    OrganizationId = todayOrg.OrganizationId,
                    Title = "Community Cleanup Drive",
                    Description = "Join us for today's community cleanup! We need volunteers to help clean up Taman Jaya Park. Equipment provided.",
                    Date = DateTime.Today,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0),
                    Location = "Taman Jaya Park, Petaling Jaya",
                    VolunteersNeeded = 20,
                    ApplicationCloseDate = DateTime.Today.AddHours(2),
                    Status = OpportunityOpenStatus.Open,
                    AutoCloseWhenFull = false,
                    IsEmergency = false,
                    MinimumCreditLevel = null
                });
            }

            var secondOrg = organizations.Skip(1).FirstOrDefault();
            if (secondOrg != null)
            {
                opportunities.Add(new Opportunity
                {
                    OpportunityId = Guid.NewGuid().ToString(),
                    OrganizationId = secondOrg.OrganizationId,
                    Title = "Food Distribution Center",
                    Description = "Help pack and distribute food packages to families in need. Morning shift available.",
                    Date = DateTime.Today,
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(14, 0, 0),
                    Location = "Food Bank Warehouse, Shah Alam",
                    VolunteersNeeded = 15,
                    ApplicationCloseDate = DateTime.Today.AddHours(3),
                    Status = OpportunityOpenStatus.Open,
                    AutoCloseWhenFull = false,
                    IsEmergency = true,
                    MinimumCreditLevel = null
                });
            }

            foreach (var opp in opportunities)
            {
                if (!await context.Opportunities.AnyAsync(o => o.Title == opp.Title && o.OrganizationId == opp.OrganizationId))
                {
                    context.Opportunities.Add(opp);
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedApplications(ApplicationDbContext context)
        {
            var volunteers = await context.Volunteers.ToListAsync();
            var opportunities = await context.Opportunities
                .Include(o => o.Applications)
                .ToListAsync();

            if (!volunteers.Any() || !opportunities.Any()) return;

            var random = new Random();
            var applications = new List<Application>();

            foreach (var opp in opportunities)
            {
                var maxApplicants = Math.Min(opp.VolunteersNeeded + 8, volunteers.Count);
                var numApplicants = random.Next(5, maxApplicants);
                var shuffledVolunteers = volunteers.OrderBy(x => random.Next()).Take(numApplicants).ToList();

                for (int i = 0; i < shuffledVolunteers.Count; i++)
                {
                    var volunteer = shuffledVolunteers[i];
                    var status = ApplicationStatus.Applied;
                    var attendanceStatus = AttendanceStatus.Pending;
                    DateTime? checkInTime = null;

                    // For past events, set attendance status
                    if (opp.Date < DateTime.Today)
                    {
                        var isPresent = random.Next(100) < 75;

                        if (isPresent)
                        {
                            attendanceStatus = AttendanceStatus.Present;
                            var baseTime = opp.StartTime;
                            var randomMinutes = random.Next(0, 60);
                            checkInTime = opp.Date.Add(baseTime).AddMinutes(randomMinutes);

                            var hours = (opp.EndTime - opp.StartTime).TotalHours;
                            volunteer.TotalHours += (int)Math.Round(hours);
                            volunteer.CompletedCount++;
                        }
                        else
                        {
                            attendanceStatus = AttendanceStatus.NoShow;
                            volunteer.NoShowCount++;
                        }

                        UpdateVolunteerCredit(volunteer);
                    }

                    // For future opportunities, determine if on waitlist
                    if (opp.Date > DateTime.Today && i >= opp.VolunteersNeeded)
                    {
                        status = ApplicationStatus.Waitlist;
                    }

                    var application = new Application
                    {
                        ApplicationId = Guid.NewGuid().ToString(),
                        OpportunityId = opp.OpportunityId,
                        VolunteerId = volunteer.VolunteerId,
                        Status = status,
                        AppliedAt = DateTime.Now.AddDays(-random.Next(1, 10)),
                        AttendanceStatus = attendanceStatus,
                        CheckInTime = checkInTime,
                        WithdrawnAt = null
                    };

                    // Some applications were withdrawn (for variety)
                    if (opp.Date > DateTime.Today && random.Next(100) < 10 && status == ApplicationStatus.Applied)
                    {
                        application.Status = ApplicationStatus.Withdrawn;
                        application.WithdrawnAt = DateTime.Now.AddDays(-random.Next(1, 5));
                    }

                    applications.Add(application);
                }
            }

            foreach (var app in applications)
            {
                var exists = await context.Applications.AnyAsync(a =>
                    a.OpportunityId == app.OpportunityId && a.VolunteerId == app.VolunteerId);

                if (!exists)
                {
                    context.Applications.Add(app);
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedCertificates(ApplicationDbContext context)
        {
            var completedOpportunities = await context.Opportunities
                .Where(o => o.Date < DateTime.Today)
                .Include(o => o.Applications)
                .ToListAsync();

            var certificates = new List<Certificate>();

            foreach (var opp in completedOpportunities)
            {
                var presentVolunteers = opp.Applications
                    .Where(a => a.AttendanceStatus == AttendanceStatus.Present)
                    .Select(a => a.VolunteerId)
                    .ToList();

                foreach (var volunteerId in presentVolunteers)
                {
                    var existingCert = await context.Certificates
                        .AnyAsync(c => c.OpportunityId == opp.OpportunityId && c.VolunteerId == volunteerId);

                    if (!existingCert)
                    {
                        certificates.Add(new Certificate
                        {
                            CertificateId = Guid.NewGuid().ToString(),
                            VolunteerId = volunteerId,
                            OrganizationId = opp.OrganizationId,
                            OpportunityId = opp.OpportunityId,
                            IssuedAt = opp.Date.AddDays(7),
                            PdfPath = $"/certificates/{opp.OpportunityId}_{volunteerId}.pdf"
                        });
                    }
                }
            }

            if (certificates.Any())
            {
                await context.Certificates.AddRangeAsync(certificates);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedFollows(ApplicationDbContext context)
        {
            var volunteers = await context.Volunteers.ToListAsync();
            var organizations = await context.Organizations.ToListAsync();

            if (!volunteers.Any() || !organizations.Any()) return;

            var random = new Random();
            var follows = new List<Follow>();

            foreach (var volunteer in volunteers)
            {
                var numFollows = random.Next(2, Math.Min(6, organizations.Count + 1));
                var shuffledOrgs = organizations.OrderBy(x => random.Next()).Take(numFollows).ToList();

                foreach (var org in shuffledOrgs)
                {
                    var existingFollow = await context.Follows
                        .AnyAsync(f => f.VolunteerId == volunteer.VolunteerId && f.OrganizationId == org.OrganizationId);

                    if (!existingFollow)
                    {
                        follows.Add(new Follow
                        {
                            FollowId = Guid.NewGuid().ToString(),
                            VolunteerId = volunteer.VolunteerId,
                            OrganizationId = org.OrganizationId,
                            FollowedAt = DateTime.Now.AddDays(-random.Next(1, 30))
                        });
                    }
                }
            }

            if (follows.Any())
            {
                await context.Follows.AddRangeAsync(follows);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedNotifications(ApplicationDbContext context)
        {
            var users = await context.Users.ToListAsync();
            if (!users.Any()) return;

            var random = new Random();
            var notifications = new List<Notification>();

            var notificationMessages = new[]
            {
                "New opportunity available in your area!",
                "Your application has been approved.",
                "Upcoming event reminder: tomorrow at 9 AM",
                "Organization followed you back!",
                "Certificate available for download"
            };

            foreach (var user in users)
            {
                var numNotifications = random.Next(0, 5);
                for (int i = 0; i < numNotifications; i++)
                {
                    notifications.Add(new Notification
                    {
                        NotificationId = Guid.NewGuid().ToString(),
                        UserId = user.Id,
                        Type = (NotificationType)random.Next(1, 5),
                        Message = notificationMessages[random.Next(notificationMessages.Length)],
                        IsRead = random.Next(100) < 70,
                        RelatedId = null,
                        CreatedAt = DateTime.Now.AddDays(-random.Next(1, 10))
                    });
                }
            }

            if (notifications.Any())
            {
                await context.Notifications.AddRangeAsync(notifications);
                await context.SaveChangesAsync();
            }
        }

        private static void UpdateVolunteerCredit(Volunteer volunteer)
        {
            var totalEvents = volunteer.CompletedCount + volunteer.NoShowCount;
            if (totalEvents == 0) return;

            var reliabilityRate = (double)volunteer.CompletedCount / totalEvents;

            if (reliabilityRate >= 0.9)
                volunteer.CreditLevel = CreditLevel.Core;
            else if (reliabilityRate >= 0.7)
                volunteer.CreditLevel = CreditLevel.Growing;
            else if (reliabilityRate >= 0.5)
                volunteer.CreditLevel = CreditLevel.Core;
            else
                volunteer.CreditLevel = CreditLevel.Inactive;
        }

        private static string GetPastOpportunityTitle(int i)
        {
            var titles = new[]
            {
                "River Cleanup Project",
                "Food Bank Sorting Day",
                "Tree Planting Campaign",
                "Elderly Home Visit",
                "School Painting Workshop",
                "Beach Cleanup",
                "Animal Shelter Help",
                "Charity Run",
                "Blood Donation Drive",
                "Disaster Relief Aid",
                "Community Teaching",
                "Park Restoration"
            };
            //return titles[i % titles.Length] + " (Completed)";
            return titles[i % titles.Length];
        }

        private static string GetPastOpportunityDescription(int i)
        {
            return $"This was a successful volunteer event where {GetRandomActivity()}. Volunteers made a significant impact on the community.";
        }

        private static string GetFutureOpportunityTitle(int i)
        {
            var titles = new[]
            {
                "Community Garden Project",
                "Weekend Food Distribution",
                "Environmental Awareness Workshop",
                "Senior Citizens Tech Support",
                "Children's Reading Program",
                "Urban Farming Initiative",
                "Plastic Waste Reduction Campaign",
                "Disaster Preparedness Training",
                "Mental Health Awareness Walk",
                "Sports Day for Underprivileged Kids",
                "Art Therapy Session",
                "Mobile Health Clinic",
                "Career Guidance Workshop",
                "Recycling Education Program",
                "Park Beautification"
            };
            return titles[i % titles.Length];
        }

        private static string GetFutureOpportunityDescription(int i)
        {
            return $"Join us for an exciting opportunity to {GetRandomActivity()}. Volunteers will receive training and support.";
        }

        private static string GetRandomActivity()
        {
            var activities = new[]
            {
                "environmental conservation",
                "community service",
                "educational support",
                "elderly care",
                "animal welfare",
                "disaster relief",
                "health awareness"
            };
            return activities[new Random().Next(activities.Length)];
        }

        private static string GetLocation(int seed)
        {
            var locations = new[]
            {
                "Kuala Lumpur",
                "Petaling Jaya",
                "Shah Alam",
                "Cheras",
                "Ampang",
                "Subang Jaya",
                "Puchong",
                "Kajang",
                "Cyberjaya",
                "Putrajaya"
            };
            return locations[seed % locations.Length];
        }
    }
}