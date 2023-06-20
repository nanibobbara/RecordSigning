using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecordSigning.Shared
{
    [Table("Records", Schema = "dbo")]
    public partial class Record
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int record_id { get; set; }
        public int batch_id { get; set; }

        [ConcurrencyCheck]
        public string record_data { get; set; }

        [ConcurrencyCheck]
        public bool? is_signed { get; set; }

        public ICollection<SignedRecord> SignedRecords { get; set; }

    }
}
