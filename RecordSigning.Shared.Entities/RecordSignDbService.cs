using Microsoft.EntityFrameworkCore;
using System.Data;

namespace RecordSigning.Shared
{
    public class RecordSignDbService
    {
        RecordSignDbContext Context
        {
            get
            {
                return this.context;
            }
        }

        private readonly RecordSignDbContext context;

        public RecordSignDbService(RecordSignDbContext context)
        {
            this.context = context;
        }

        //public void Reset() => Context.ChangeTracker.Entries().Where(e => e.Entity != null).ToList().ForEach(e => e.State = EntityState.Detached);


        // Record methods

        /// <summary>
        /// fetch the next available records to process of given batch size to process
        /// </summary>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public async Task<List<Record>> GetRecords(int batchSize)
        {
            List<Record> records = new List<Record>();

            // fetch next available records to process of given batch size to process
            var recs = Context.Records
                .Where(r => r.batch_id == 0 && r.is_signed == false)
                .Take(batchSize).ToList<Record>();

            // get the max batch id and increment by 1
            // assign the new batch id to the records to avoid duplicate processing
            if (recs?.Count > 0)
            {
                records = recs;
                int maxbatchId = Context.Records.Max(r => r.batch_id) + 1;
                records.ForEach(record => record.batch_id = maxbatchId);
                await UpdateRecordStatusAsync(records);
            }

            return await Task.FromResult(records);
        }

        public List<Record> GetRecords1(int batchSize)
        {
            List<Record> records = new List<Record>();

            // fetch next available records to process of given batch size to process
            var recs = Context.Records
                .Where(r => r.batch_id == 0 && r.is_signed == false)
                .Take(batchSize).ToList<Record>();

            // get the max batch id and increment by 1
            // assign the new batch id to the records to avoid duplicate processing
            if (recs?.Count > 0)
            {
                records = recs;
                int maxbatchId = Context.Records.Max(r => r.batch_id) + 1;
                records.ForEach(record => record.batch_id = maxbatchId);
                UpdateRecordStatus(records);
            }

            return records;
        }

        public async Task<Record?> UpdateRecordStatus(int recordId)
        {
            Record? itemToUpdate = await Context.Records
                .Where(r => r.record_id == recordId)
                .FirstOrDefaultAsync();

            if (itemToUpdate == null)
            {
                throw new Exception($"{recordId}, Record not found");
            }

            itemToUpdate.is_signed = true;

            var entryToUpdate = Context.Entry(itemToUpdate);
            entryToUpdate.CurrentValues.SetValues(itemToUpdate);
            entryToUpdate.State = EntityState.Modified;

            if (Context.ChangeTracker.HasChanges())
            {
                await Context.SaveChangesAsync();
            }

            return itemToUpdate;
        }

        public async Task<List<Record>> UpdateRecordStatusAsync(List<Record> recordsToUpdate)
        {
            if (recordsToUpdate.Count == 0)
            {
                throw new Exception("No records found");
            }

            Context.Records.UpdateRange(recordsToUpdate);
            await Context.SaveChangesAsync();

            return recordsToUpdate;
        }

        public void UpdateRecordStatus(List<Record> recordsToUpdate)
        {
            if (recordsToUpdate.Count == 0)
            {
                throw new Exception("No records found");
            }

            Context.Records.UpdateRange(recordsToUpdate);
            Context.SaveChanges();

        }



        // SignedRecord methods

        public async Task<List<SignedRecord>> GetSignedRecords(int batchId)
        {
            var items = Context.SignedRecords.Where(x => x.batch_id == batchId).ToList<SignedRecord>();

            return await Task.FromResult(items);
        }
        public async Task<SignedRecord> CreateSignedRecord(SignedRecord signedrecord)
        {
            var existingItem = Context.SignedRecords
                              .Where(i => i.record_id == signedrecord.record_id)
                              .FirstOrDefault();

            if (existingItem != null)
            {
                throw new Exception("Item already signed");
            }

            try
            {
                await Context.SignedRecords.AddAsync(signedrecord);
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(signedrecord).State = EntityState.Detached;
                throw;
            }

            return signedrecord;
        }



        // Key methods

        public Task<List<Key>> LoadKeysFromKeyVault(int count)
        {
            List<Key> keys = new List<Key>();
            for (int i = 0; i < 20; i++)
            {
                var key = new Key()
                {
                    key_name = $"Seeded_key_for_testing_{i}",
                    is_in_use = false,
                    last_used_at = DateTime.UtcNow
                };

                keys.Add(key);
            }

            return Task.FromResult(keys);
        }

        public async Task<string> SeedToKeys(List<Key> keys)
        {
            if (keys?.Count() > 0)
            {
                await Context.Keys.AddRangeAsync(keys);
                Context.SaveChanges();
            };

            return $"{keys?.Count()}, keys loaded from Key Vault";
        }


        /*
        public async Task<Key?> GetNextAvailableKey()
        {
            var availableKey = await Context.Keys
                .Where(key => key.is_in_use == false)
                .OrderBy(key=>key.last_used_at)
                .FirstOrDefaultAsync<Key>();

            if (availableKey == null)
            {
                return null;
            }

            await UpdateKeyStatus(availableKey.key_id, true);
            return availableKey;
        }*/

        public Key GetNextAvailableKey()
        {
            var availableKey = Context.Keys
                .Where(key => key.is_in_use == false)
                .OrderBy(key => key.last_used_at)
                .FirstOrDefault<Key>();

            if (availableKey == null)
            {
                return null;
            }

            UpdateKeyStatus(availableKey.key_id, true);
            return availableKey;
        }

        public async Task<Key?> UpdateKeyStatus(int keyId, bool isInUse = false)
        {
            var itemToUpdate = Context.Keys
                                  .Where(i => i.key_id == keyId)
                                  .FirstOrDefault();

            if (itemToUpdate == null)
            {
                return null;
            }

            itemToUpdate.is_in_use = isInUse;
            itemToUpdate.last_used_at = DateTime.UtcNow;

            var entryToUpdate = Context.Entry(itemToUpdate);
            entryToUpdate.CurrentValues.SetValues(itemToUpdate);
            entryToUpdate.State = EntityState.Modified;

            if (Context.ChangeTracker.HasChanges())
            {
                await Context.SaveChangesAsync();
            }

            return itemToUpdate;
        }

        public async Task<string> DeleteKeys()
        {
            var itemsToDelete = await Context.Keys.ToListAsync();
            if (itemsToDelete?.Count() > 0)
            {
                Context.Keys.RemoveRange(itemsToDelete);
                await Context.SaveChangesAsync();
            }


            return $"{itemsToDelete?.Count}, records been deleted";
        }
    }

}
