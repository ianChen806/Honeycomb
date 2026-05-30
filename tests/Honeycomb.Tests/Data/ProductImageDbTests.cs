using Honeycomb.Data;
using Honeycomb.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Honeycomb.Tests.Data;

public class ProductImageDbTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public ProductImageDbTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _db.Currencies.Add(new Currency { Code = "TWD", Name = "新台幣" });
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private Product AddProduct(string name = "A")
    {
        var currency = _db.Currencies.First();
        var product = new Product
        {
            Name = name,
            UnitPrice = 100,
            CurrencyId = currency.Id,
            ExchangeRate = 1,
            Discount = 1,
            ListingPrice = 200,
            CommissionFee = 10,
            CategoryId = 1,
            CreatedAt = DateTime.Now
        };
        _db.Products.Add(product);
        _db.SaveChanges();
        return product;
    }

    [Fact]
    public void ProductImage_RoundTrips()
    {
        var product = AddProduct();
        var bytes = new byte[] { 1, 2, 3, 4 };

        _db.ProductImages.Add(new ProductImage { ProductId = product.Id, Data = bytes });
        _db.SaveChanges();

        var loaded = _db.ProductImages.Single(pi => pi.ProductId == product.Id);
        Assert.Equal(bytes, loaded.Data);
    }

    [Fact]
    public void ProductImage_DuplicateProductId_RejectedByUniqueIndex()
    {
        // 直接以 raw SQL 插入第二筆，繞過 EF change tracker（tracker 在 1:1 設定下
        // 會在送往 DB 前先攔掉重複），以驗證 DB 層唯一索引這道安全網確實存在。
        var product = AddProduct();
        _db.ProductImages.Add(new ProductImage { ProductId = product.Id, Data = new byte[] { 1 } });
        _db.SaveChanges();

        var ex = Assert.ThrowsAny<Exception>(() =>
            _db.Database.ExecuteSqlRaw(
                $"INSERT INTO ProductImages (ProductId, Data) VALUES ({product.Id}, X'02')"));

        Assert.Contains("UNIQUE", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
