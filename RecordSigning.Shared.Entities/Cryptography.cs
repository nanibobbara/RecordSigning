using RecordSigning.Shared.Entities.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RecordSigning.Shared
{
    /// <summary>
    /// Provides cryptographic operations such as key generation, signing, and signature verification.
    /// </summary>
    public static class Cryptography
    {
        /// <summary>
        /// Generates a specified number of key pairs.
        /// </summary>
        /// <param name="count">The number of key pairs to generate.</param>
        /// <returns>A list of generated key pairs.</returns>
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

        /// <summary>
        /// Generates a new RSA key pair.
        /// </summary>
        /// <returns>The generated RSA key pair.</returns>
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

        /// <summary>
        /// Signs the specified plaintext using the provided RSA private key.
        /// </summary>
        /// <param name="plainText">The plaintext to sign.</param>
        /// <param name="privateKey">The RSA private key.</param>
        /// <returns>The signature of the plaintext.</returns>
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

        /// <summary>
        /// Signs a batch of unsigned records using the provided key pair.
        /// </summary>
        /// <param name="unsignedRecords">The batch of unsigned records.</param>
        /// <param name="keyPair">The key pair used for signing.</param>
        /// <returns>The signed record batch.</returns>
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
