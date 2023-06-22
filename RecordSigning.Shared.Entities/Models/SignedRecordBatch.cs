using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordSigning.Shared
{
    public class SignedRecordBatch
    {
        public int batch_id { get; set; }
        public List<SignedRecord> records { get; set; }

        public SignedRecordBatch(int batch_id, List<SignedRecord> records)
        {
            this.batch_id = batch_id;
            this.records = records;
        }
    }
}
