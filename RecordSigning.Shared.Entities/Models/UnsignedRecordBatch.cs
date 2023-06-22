using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordSigning.Shared
{
    public class UnsignedRecordBatch
    {
        public int batch_id { get; set; }
        public List<Record> records { get; set; }

        public UnsignedRecordBatch(int batch_id, List<Record> records)
        {
            this.batch_id = batch_id;
            this.records = records;
        }
    }
}
