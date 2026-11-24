using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("Inventory")
    ?? throw new InvalidOperationException("Set ConnectionStrings__Inventory.");
builder.Services.AddDbContext<InventoryDb>(o => o.UseSqlServer(connection));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/products", async (InventoryDb db, bool? lowStock) =>
    await db.Products.AsNoTracking()
        .Where(p => lowStock != true || p.Quantity <= p.ReorderLevel)
        .OrderBy(p => p.Id).ToListAsync());
app.MapGet("/products/{id:int}", async (int id, InventoryDb db) =>
    await db.Products.FindAsync(id) is Product p ? Results.Ok(p) : Results.NotFound());
app.MapPost("/products", async (ProductInput input, InventoryDb db) =>
{
    if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 120 ||
        input.ReorderLevel < 0 || string.IsNullOrWhiteSpace(input.Sku) || input.Sku.Length > 40)
        return Results.BadRequest("Invalid product.");
    var sku = input.Sku.Trim().ToUpperInvariant();
    var p = new Product { Name = input.Name.Trim(), Sku = sku, ReorderLevel = input.ReorderLevel };
    db.Products.Add(p);
    try { await db.SaveChangesAsync(); }
    catch (DbUpdateException) { return Results.Conflict("Could not create product; SKU may already exist."); }
    return Results.Created($"/products/{p.Id}", p);
});
app.MapPost("/products/{id:int}/movements", async (int id, MovementInput input, InventoryDb db) =>
{
    if (input.Delta == 0 || Math.Abs((long)input.Delta) > 1000000)
        return Results.BadRequest("Delta must be nonzero and at most one million in magnitude.");
    var p = await db.Products.FindAsync(id);
    if (p is null) return Results.NotFound();
    long next = (long)p.Quantity + input.Delta;
    if (next < 0 || next > int.MaxValue) return Results.Conflict("Stock would be outside valid range.");
    p.Quantity = (int)next;
    db.Movements.Add(new StockMovement { ProductId = id, Delta = input.Delta, RecordedAt = DateTimeOffset.UtcNow });
    try { await db.SaveChangesAsync(); }
    catch (DbUpdateConcurrencyException) { return Results.Conflict("Stock changed concurrently; retry."); }
    return Results.Ok(p);
});
app.MapGet("/products/{id:int}/movements", async (int id, InventoryDb db) =>
    await db.Movements.AsNoTracking().Where(m => m.ProductId == id)
        .OrderByDescending(m => m.RecordedAt).ToListAsync());
if (app.Configuration.GetValue<bool>("InitializeDatabase"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<InventoryDb>().Database.EnsureCreatedAsync();
}
app.Run();

public record ProductInput(string Name, string Sku, int ReorderLevel);
public record MovementInput(int Delta);
public class Product
{
    public int Id { get; set; }
    [MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(40)] public string Sku { get; set; } = "";
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; }
    [Timestamp] public byte[] Version { get; set; } = [];
}
public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Delta { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}
public class InventoryDb(DbContextOptions<InventoryDb> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> Movements => Set<StockMovement>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Product>().HasIndex(p => p.Sku).IsUnique();
        model.Entity<StockMovement>().HasOne<Product>().WithMany().HasForeignKey(m => m.ProductId);
    }
}
