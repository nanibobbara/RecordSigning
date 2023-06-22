using Microsoft.EntityFrameworkCore;
using System.Data;

namespace RecordSigning.Shared
{
    public class RecordSignDbService
    {
        private readonly RecordSignDbContext _context;

        public RecordSignDbService( RecordSignDbContext context)
        {
            _context = context;
        }


        #region Record methods

        public List<Record> GetRecords(int batchSize)
        {
            List<Record> records = new List<Record>();
            var recordIds =  _context.Records
                .Where(r => r.batch_id == 0 && r.is_signed == false)
                .OrderBy(x=>x.batch_id)
                .Take(batchSize).Select(r => r.record_id)
                .ToList();

            if (recordIds.Count > 0)
            {
                int maxBatchId = _context.Records.Max(r => r.batch_id);
                maxBatchId++;
                records = UpdateRecordStatus(recordIds, maxBatchId);
            }

            return records;
        }

        public List<Record> UpdateRecordStatus(List<int> recordIds, int batchId)
        {
            var recordsToUpdate = _context.Records
                .Where(r => recordIds.Contains(r.record_id))
                .ToList();

            if (recordsToUpdate.Count > 0)
            {
                foreach (var record in recordsToUpdate)
                {
                    record.batch_id = batchId;
                }

                _context.SaveChanges();
            }
            return recordsToUpdate;
        }
        
        public void UpdateRecordStatus(int batchId)
        {
            if (batchId > 0)
            {
                var recordsToUpdate = _context.Records.Where(r => r.batch_id == batchId).ToList();
                foreach (var recordToUpdate in recordsToUpdate)
                {
                    recordToUpdate.is_signed = true;
                }

                _context.Records.UpdateRange(recordsToUpdate);
                _context.SaveChanges();
            }
        }

        #endregion

        
        #region SignedRecord methods

        public async Task<List<SignedRecord>> GetSignedRecords(int batchId)
        {
            var items = await _context.SignedRecords.Where(x => x.batch_id == batchId).ToListAsync();

            return items;
        }

        public void CreateSignedRecord(List<SignedRecord> signedrecords)
        {

            if (signedrecords?.Count > 0)
            {
                _context.SignedRecords.AddRange(signedrecords);
                _context.SaveChangesAsync();                
            }
        }

        #endregion


        #region KeyRing methods

        public Task<List<KeyRing>> LoadKeysFromKeyVault(int count)
        {
            List<KeyRing> keys = new List<KeyRing>();
            for (int i = 0; i < 20; i++)
            {
                var key = new KeyRing()
                {
                    key_name = $"Seeded_key_for_testing_{i}",
                    is_in_use = false,
                    last_used_at = DateTime.UtcNow
                };

                keys.Add(key);
            }

            return Task.FromResult(keys);
        }

        public async Task<string> SeedToKeys(List<KeyRing> keys)
        {
            if (keys?.Count > 0)
            {
                _context.KeyRing.AddRange(keys);
                await _context.SaveChangesAsync();
            }

            return $"{keys?.Count()}, keys loaded from Key Vault";
        }

        public async Task<KeyRing?> GetNextAvailableKey()
        {
            var availableKey = await _context.KeyRing
                .Where(key => key.is_in_use == false)
                .OrderBy(key => key.last_used_at)
                .FirstOrDefaultAsync();

            if (availableKey == null)
            {
                return null;
            }

            await UpdateKeyStatus(availableKey.key_id, true);
            return availableKey;
        }

        public async Task<KeyRing?> UpdateKeyStatus(int keyId, bool isInUse = false)
        {
            var itemToUpdate = await _context.KeyRing
                .FirstOrDefaultAsync(i => i.key_id == keyId);

            if (itemToUpdate == null)
            {
                return null;
            }

            itemToUpdate.is_in_use = isInUse;
            itemToUpdate.last_used_at = DateTime.UtcNow;

            _context.Update(itemToUpdate);
            await _context.SaveChangesAsync();

            return itemToUpdate;
        }

        public async Task<string> DeleteKeys()
        {
            var itemsToDelete = await _context.KeyRing.ToListAsync();
            if (itemsToDelete?.Count > 0)
            {
                _context.KeyRing.RemoveRange(itemsToDelete);
                await _context.SaveChangesAsync();
            }

            return $"{itemsToDelete?.Count}, records been deleted";
        }

        #endregion
    }
}
