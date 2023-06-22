using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace RecordSigning.Shared
{
    /// <summary>
    /// Represents the database context for the record signing application.
    /// </summary>
    public partial class RecordSignDbContext : DbContext
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordSignDbContext"/> class.
        /// </summary>
        public RecordSignDbContext()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordSignDbContext"/> class with the specified options.
        /// </summary>
        /// <param name="options">The options to be used by the context.</param>
        public RecordSignDbContext(DbContextOptions<RecordSignDbContext> options) : base(options)
        {
        }

        partial void OnModelBuilding(ModelBuilder builder);

        /// <summary>
        /// Configures the model that was discovered by convention from the entity types.
        /// </summary>
        /// <param name="builder">The builder being used to construct the model for this context.</param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships
            builder.Entity<SignedRecord>()
              .HasOne(i => i.Record)
              .WithMany(i => i.SignedRecords)
              .HasForeignKey(i => i.record_id)
              .HasPrincipalKey(i => i.record_id);

            // Set default value for is_signed property
            builder.Entity<Record>()
              .Property(p => p.is_signed)
              .HasDefaultValueSql(@"((0))");

            // Configure data type for last_used_at property
            builder.Entity<KeyRing>()
              .Property(p => p.last_used_at)
              .HasColumnType("datetime");

            // Invoke custom model building logic
            this.OnModelBuilding(builder);
        }

        /// <summary>
        /// Gets or sets the DbSet of key rings.
        /// </summary>
        public DbSet<KeyRing> KeyRing { get; set; }

        /// <summary>
        /// Gets or sets the DbSet of records.
        /// </summary>
        public DbSet<Record> Records { get; set; }

        /// <summary>
        /// Gets or sets the DbSet of signed records.
        /// </summary>
        public DbSet<SignedRecord> SignedRecords { get; set; }
    }
}
