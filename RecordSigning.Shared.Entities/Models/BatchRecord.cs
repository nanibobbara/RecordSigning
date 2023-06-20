using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordSigning.Shared
{
    public class BatchRecord
    {
        public int batch_id { get; set; }
        public List<Record> batch_records { get; set; }

        public BatchRecord(int batch_id, List<Record> batch_records)
        {
            this.batch_id = batch_id;
            this.batch_records = batch_records;
        }
    }
}
