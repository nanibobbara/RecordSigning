using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RecordSigning.Shared;
using RecordSigning.Shared.Entities.Models;

namespace RecordSigning.Shared
{
    public static class Cryptography
    {
        public static List<KeyRing> GenerateKeys(int count)
        {
            List<KeyRing> keys = new List<KeyRing>();

            for (int i = 0; i < count; i++)
            {
                KeyPair keyPair = GenerateKey();
                var key = new KeyRing()
                {
                    key_name = Guid.NewGuid().ToString(),
                    is_in_use = false,
                    last_used_at = DateTime.UtcNow,
                    key_data = JsonSerializer.Serialize(keyPair)
                };
                keys.Add(key);
            }

            return keys;
        }

        public static KeyPair GenerateKey()
        {
            using (RSA rsa = RSA.Create())
            {
                byte[] privateKey = rsa.ExportRSAPrivateKey();
                byte[] publicKey = rsa.ExportRSAPublicKey();

                return new KeyPair
                {
                    PrivateKey = privateKey,
                    PublicKey = publicKey
                };
            }
        }



        public static byte[] SignData(string plainText, RSAParameters privateKey)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(privateKey);
                byte[] dataBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return signature;
            }
        }

        public static SignedRecordBatch SignBatchOfUnsignedRecords(UnsignedRecordBatch unsignedRecords, KeyPair keyPair)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportRSAPrivateKey(keyPair.PrivateKey, out _);

                List<SignedRecord> signedRecords = new List<SignedRecord>();
                foreach (Record record in unsignedRecords.records)
                {
                    string data = record.record_data;
                    byte[] dataToSign = Encoding.UTF8.GetBytes(data);
                    byte[] signature = rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    SignedRecord signedRec = new SignedRecord
                    {
                        record_id = record.record_id,
                        batch_id = record.batch_id,
                        key_name = "Sample Key Name",
                        signature_data = Convert.ToBase64String(signature),
                        signed_timestamp = DateTime.UtcNow
                    };
                    signedRecords.Add(signedRec);
                }

                SignedRecordBatch signedRecordBatch = new SignedRecordBatch(unsignedRecords.batch_id, signedRecords);

                return signedRecordBatch;
            }
        }
    }
}
