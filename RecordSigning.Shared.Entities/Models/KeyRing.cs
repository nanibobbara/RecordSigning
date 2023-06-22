using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecordSigning.Shared
{
    [Table("KeyRing", Schema = "dbo")]
    public partial class KeyRing
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int key_id { get; set; }

        [ConcurrencyCheck]
        public string key_name { get; set; }

        [ConcurrencyCheck]
        public string key_data { get; set; }

        [ConcurrencyCheck]
        public bool? is_in_use { get; set; }

        [ConcurrencyCheck]
        public DateTime? last_used_at { get; set; }


        //public ICollection<SignedRecord> SignedRecords { get; set; }

    }
}
