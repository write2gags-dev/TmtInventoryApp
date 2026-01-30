using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models.DealerPortal;
using System.Security.Cryptography;
using System.Text;

namespace TmtInventoryApp.Services
{
    public class DealerPortalSeeder
    {
        private readonly InventoryContext _context;

        public DealerPortalSeeder(InventoryContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            // Seed Product Categories
            if (!await _context.ProductCategories.AnyAsync())
            {
                var categories = new List<ProductCategory>
                {
                    new ProductCategory
                    {
                        Name = "Cement",
                        Description = "Various cement brands",
                        UnitOfMeasurement = "Bags",
                        IsActive = true
                    },
                    new ProductCategory
                    {
                        Name = "Asbestos",
                        Description = "Asbestos sheets and products",
                        UnitOfMeasurement = "Sheets",
                        IsActive = true
                    },
                    new ProductCategory
                    {
                        Name = "TMT Bars",
                        Description = "TMT steel bars",
                        UnitOfMeasurement = "KG",
                        IsActive = true
                    }
                };

                _context.ProductCategories.AddRange(categories);
                await _context.SaveChangesAsync();
            }

            // Seed Product Brands
            if (!await _context.ProductBrands.AnyAsync())
            {
                var cementCategory = await _context.ProductCategories.FirstOrDefaultAsync(c => c.Name == "Cement");
                var asbestosCategory = await _context.ProductCategories.FirstOrDefaultAsync(c => c.Name == "Asbestos");
                var tmtCategory = await _context.ProductCategories.FirstOrDefaultAsync(c => c.Name == "TMT Bars");

                var brands = new List<ProductBrand>();

                // Cement Brands
                if (cementCategory != null)
                {
                    brands.AddRange(new[]
                    {
                        new ProductBrand { Name = "UltraTech", ProductCategoryId = cementCategory.Id, IsActive = true },
                        new ProductBrand { Name = "ACC", ProductCategoryId = cementCategory.Id, IsActive = true },
                        new ProductBrand { Name = "Ambuja", ProductCategoryId = cementCategory.Id, IsActive = true },
                        new ProductBrand { Name = "Shree Cement", ProductCategoryId = cementCategory.Id, IsActive = true },
                        new ProductBrand { Name = "Birla Shakti", ProductCategoryId = cementCategory.Id, IsActive = true }
                    });
                }

                // Asbestos Brands
                if (asbestosCategory != null)
                {
                    brands.AddRange(new[]
                    {
                        new ProductBrand { Name = "Everest", ProductCategoryId = asbestosCategory.Id, IsActive = true },
                        new ProductBrand { Name = "Visaka", ProductCategoryId = asbestosCategory.Id, IsActive = true },
                        new ProductBrand { Name = "Ramco", ProductCategoryId = asbestosCategory.Id, IsActive = true }
                    });
                }

                // TMT Brands
                if (tmtCategory != null)
                {
                    brands.AddRange(new[]
                    {
                        new ProductBrand { Name = "TATA Tiscon", ProductCategoryId = tmtCategory.Id, IsActive = true },
                        new ProductBrand { Name = "JSW NeoSteel", ProductCategoryId = tmtCategory.Id, IsActive = true },
                        new ProductBrand { Name = "Kamdhenu", ProductCategoryId = tmtCategory.Id, IsActive = true },
                        new ProductBrand { Name = "SAIL", ProductCategoryId = tmtCategory.Id, IsActive = true },
                        new ProductBrand { Name = "Jindal Panther", ProductCategoryId = tmtCategory.Id, IsActive = true }
                    });
                }

                _context.ProductBrands.AddRange(brands);
                await _context.SaveChangesAsync();
            }
        }

        public async Task CreateDealerUserAsync(int dealerId, string username, string password)
        {
            // Check if dealer exists
            var dealer = await _context.Dealers.FindAsync(dealerId);
            if (dealer == null)
            {
                throw new Exception($"Dealer with ID {dealerId} not found.");
            }

            // Check if username already exists
            var existingUser = await _context.DealerUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (existingUser != null)
            {
                throw new Exception($"Username '{username}' already exists.");
            }

            // Create dealer user
            var dealerUser = new DealerUser
            {
                Username = username,
                PasswordHash = HashPassword(password),
                DealerId = dealerId,
                IsActive = true,
                CreatedDate = DateTime.Now,
                Email = dealer.ContactPerson,
                Phone = dealer.PhoneNumber
            };

            _context.DealerUsers.Add(dealerUser);
            await _context.SaveChangesAsync();
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
