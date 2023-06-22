using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace RecordSigning.Shared
{
    public partial class RecordSignDbContext : DbContext
    {
        private readonly string _connectionString;

        public RecordSignDbContext()
        {
        }

        public RecordSignDbContext(DbContextOptions<RecordSignDbContext> options) : base(options)
        {
        }

        partial void OnModelBuilding(ModelBuilder builder);

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<SignedRecord>()
              .HasOne(i => i.Record)
              .WithMany(i => i.SignedRecords)
              .HasForeignKey(i => i.record_id)
              .HasPrincipalKey(i => i.record_id);

            builder.Entity<Record>()
              .Property(p => p.is_signed)
              .HasDefaultValueSql(@"((0))");

            builder.Entity<KeyRing>()
              .Property(p => p.last_used_at)
              .HasColumnType("datetime");
            this.OnModelBuilding(builder);
        }

        public DbSet<KeyRing> KeyRing { get; set; }

        public DbSet<Record> Records { get; set; }

        public DbSet<SignedRecord> SignedRecords { get; set; }
    }
}
