using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordSigning.Shared.Entities.Models
{
    public class KeyPair
    {
        public byte[] PrivateKey { get; set; }
        public byte[] PublicKey { get; set; }
    }

}
