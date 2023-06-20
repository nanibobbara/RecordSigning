using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Cryptography;
using System.Text;

namespace RecordSigning.Shared
{
    public static class Cryptography
    {
        public static List<Key> GenerateKeys(int count)
        {
            List<Key> keys = new List<Key>();

            for (int i = 0; i < count; i++)
            {
                var key = new Key()
                {
                    key_name = Guid.NewGuid().ToString(),
                    is_in_use = false,
                    last_used_at = DateTime.UtcNow,
                    key_data = GenerateKey()
                };
                keys.Add(key);
            }



            return keys;
        }

        public static byte[] GenerateKey()
        {
            using (RSA rsa = RSA.Create())
            {
                return rsa.ExportRSAPrivateKey();
            }

        }

        public static byte[] SignData(string plainText, byte[] privateKey)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportRSAPrivateKey(privateKey, out _);
                byte[] dataBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return signature;
            }
        }

        public static List<Record> SignData(List<Record> records, RSAParameters privateKey)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(privateKey);

                foreach (Record record in records)
                {
                    string data = record.record_data;
                    byte[] dataToSign = Encoding.UTF8.GetBytes(data);
                    byte[] signature = rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    record.is_signed = true;

                    record.SignedRecords.Add(new SignedRecord
                    {
                        key_name = rsa.ToXmlString(true),
                        signature_data = Convert.ToBase64String(signature)
                    });
                }

                //byte[] dataToSign = Encoding.UTF8.GetBytes(data);
                //byte[] signature = rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                return records;
            }
        }
    }
}
