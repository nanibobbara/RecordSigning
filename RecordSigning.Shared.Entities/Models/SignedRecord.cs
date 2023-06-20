using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordSigning.Shared
{
    [Table("SignedRecords", Schema = "dbo")]
    public partial class SignedRecord
    {

        [Key]
        [Required]
        public int record_id { get; set; }

        public int batch_id { get; set; }

        [ConcurrencyCheck]
        public string key_name { get; set; }

        [ConcurrencyCheck]
        public string signature_data { get; set; }

        public Record Record { get; set; }


    }
}
