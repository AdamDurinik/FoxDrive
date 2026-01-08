using Microsoft.EntityFrameworkCore;
namespace FoxDen.Web.Models.Recepie;
public class RecepieDbContext : DbContext
{
    public RecepieDbContext(DbContextOptions<RecepieDbContext> options)
        : base(options)
    {
    }

    public DbSet<RecepieGroup> RecepieGroups => Set<RecepieGroup>();
    public DbSet<RecepieVersion> RecepieVersions => Set<RecepieVersion>();
    public DbSet<RecepieIngredient> RecepieIngredients => Set<RecepieIngredient>();
    public DbSet<RecepieItem> RecepieItems => Set<RecepieItem>();
    public DbSet<RecepieProcess> RecepieProcesses => Set<RecepieProcess>();
    public DbSet<RecepieImage> RecepieImages => Set<RecepieImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureRecepieGroup(modelBuilder);
        ConfigureRecepieVersion(modelBuilder);
        ConfigureRecepieItem(modelBuilder);
        ConfigureRecepieIngredient(modelBuilder);
        ConfigureRecepieProcess(modelBuilder);
        ConfigureRecepieImage(modelBuilder);
    }

    private static void ConfigureRecepieGroup(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecepieGroup>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.HasMany(x => x.Versions)
                  .WithOne()
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureRecepieVersion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecepieVersion>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                  .HasMaxLength(200);

            entity.Property(x => x.Servings)
                  .IsRequired();

            entity.Property(x => x.Rating);

            entity.Property(x => x.CreatedUtc)
                  .IsRequired();

            entity.HasMany(x => x.Ingredients)
                  .WithOne()
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Steps)
                  .WithOne()
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Photo)
                  .WithMany()
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureRecepieItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecepieItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Quantity)
                  .IsRequired();

            entity.Property(x => x.QuantityType)
                  .IsRequired();

            entity.HasOne(x => x.Ingredient)
                  .WithMany()
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRecepieIngredient(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecepieIngredient>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(x => x.Description)
                  .HasMaxLength(1000);

            // self-referencing many-to-many (substitutions)
            entity.HasMany(x => x.Substitutions)
                  .WithMany()
                  .UsingEntity(j =>
                      j.ToTable("RecepieIngredientSubstitutions"));
        });
    }

    private static void ConfigureRecepieProcess(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecepieProcess>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Description)
                  .IsRequired()
                  .HasMaxLength(2000);
        });
    }

    private static void ConfigureRecepieImage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecepieImage>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Url)
                  .IsRequired()
                  .HasMaxLength(500);
        });
    }
}


// dotnet ef migrations add InitialCreate -p FoxDen.Web -s FoxDen.Web -o Models/Recepie/Migrations