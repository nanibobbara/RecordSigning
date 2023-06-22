using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace RecordSigning.Shared
{
     /// <summary>
     /// Initializes a new instance of the <see cref="RecordSignDbService"/> class.
     /// </summary>
     /// <param name="context">The database context.</param>
    public class RecordSignDbService
    {
        private readonly RecordSignDbContext _context;
        private readonly ILogger<RecordSignDbService> _logger;

        public RecordSignDbService(RecordSignDbContext context, ILogger<RecordSignDbService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Record methods

        /// <summary>
        /// Retrieves a batch of unsigned records from the database.
        /// </summary>
        /// <param name="batchSize">The size of the batch to retrieve.</param>
        /// <returns>A list of records.</returns>
        public List<Record> GetRecords(int batchSize)
        {
            List<Record> records = new List<Record>();
            int maxBatchId;

            // Use a lock to ensure atomicity and prevent concurrent updates
            lock (_context)
            {
                // Get the current maximum batch ID
                maxBatchId = _context.Records.Max(r => r.batch_id);

                // Increment the maxBatchId by 1 for the current batch
                maxBatchId++;
            }

            // Update the top batchSize records that match the criteria
            var updatedRecords = _context.Records
                .Where(r => r.batch_id == 0 && r.is_signed == false)
                .OrderBy(x => x.record_id)
                .Take(batchSize)
                .ToList();

            if (updatedRecords.Count > 0)
            {
                foreach (var record in updatedRecords)
                {
                    // Update the batch_id for each record
                    record.batch_id = maxBatchId;
                }

                try
                {
                    _context.SaveChanges();
                    records = updatedRecords;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update records batch_id");
                    // Handle the exception as per your application's error handling strategy
                }
            }

            return records;
        }
    

        /// <summary>
        /// Updates the status of all records in the specified batch to "signed".
        /// </summary>
        /// <param name="batchId">The batch ID of the records to update.</param>
        public void UpdateRecordStatus(int batchId)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating record statuses.");
            }
        }

        #endregion

        #region SignedRecord methods

        /// <summary>
        /// Retrieves a list of signed records for the specified batch ID.
        /// </summary>
        /// <param name="batchId">The batch ID to retrieve signed records for.</param>
        /// <returns>A list of signed records.</returns>
        public async Task<List<SignedRecord>> GetSignedRecords(int batchId)
        {
            var items = new List<SignedRecord>();

            try
            {
                items = await _context.SignedRecords.Where(x => x.batch_id == batchId).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving signed records.");
            }

            return items;
        }

        /// <summary>
        /// Creates new signed records in the database.
        /// </summary>
        /// <param name="signedRecords">The signed records to create.</param>
        public void CreateSignedRecord(List<SignedRecord> signedRecords)
        {
            try
            {
                if (signedRecords?.Count > 0)
                {
                    _context.SignedRecords.AddRange(signedRecords);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating signed records.");
            }
        }

        #endregion

        #region KeyRing methods

        /// <summary>
        /// Loads a specified number of keys from a key vault.
        /// </summary>
        /// <param name="count">The number of keys to load.</param>
        /// <returns>A list of loaded keys.</returns>
        public Task<List<KeyRing>> LoadKeysFromKeyVault(int count)
        {
            List<KeyRing> keys = new List<KeyRing>();

            try
            {
                for (int i = 0; i < count; i++)
                {
                    var key = new KeyRing()
                    {
                        key_name = $"Seeded_key_for_testing_{i}",
                        is_in_use = false,
                        last_used_at = DateTime.UtcNow
                    };

                    keys.Add(key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading keys from Key Vault.");
            }

            return Task.FromResult(keys);
        }

        /// <summary>
        /// Seeds the specified keys into the database.
        /// </summary>
        /// <param name="keys">The keys to seed.</param>
        /// <returns>A message indicating the number of keys loaded.</returns>
        public async Task<string> SeedToKeys(List<KeyRing> keys)
        {
            try
            {
                if (keys?.Count > 0)
                {
                    _context.KeyRing.AddRange(keys);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding keys into the database.");
            }

            return $"{keys?.Count()}, keys loaded from Key Vault";
        }

        /// <summary>
        /// Retrieves the next available key that is not currently in use.
        /// </summary>
        /// <returns>The next available key, or null if no key is available.</returns>
        public async Task<KeyRing?> GetNextAvailableKey()
        {
            KeyRing? availableKey = null;

            try
            {
                availableKey = await _context.KeyRing
                    .Where(key => key.is_in_use == false)
                    .OrderBy(key => key.last_used_at)
                    .FirstOrDefaultAsync();

                if (availableKey != null)
                {
                    await UpdateKeyStatus(availableKey.key_id, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving the next available key.");
            }

            return availableKey;
        }

        /// <summary>
        /// Updates the status of the specified key.
        /// </summary>
        /// <param name="keyId">The ID of the key to update.</param>
        /// <param name="isInUse">The new status indicating whether the key is in use.</param>
        /// <returns>The updated key, or null if the key was not found.</returns>
        public async Task<KeyRing?> UpdateKeyStatus(int keyId, bool isInUse = false)
        {
            KeyRing? itemToUpdate = null;

            try
            {
                itemToUpdate = await _context.KeyRing
                    .FirstOrDefaultAsync(i => i.key_id == keyId);

                if (itemToUpdate != null)
                {
                    itemToUpdate.is_in_use = isInUse;
                    itemToUpdate.last_used_at = DateTime.UtcNow;

                    _context.Update(itemToUpdate);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating the key status.");
            }

            return itemToUpdate;
        }

        /// <summary>
        /// Deletes all keys from the database.
        /// </summary>
        /// <returns>A message indicating the number of keys deleted.</returns>
        public async Task<string> DeleteKeys()
        {
            try
            {
                var itemsToDelete = await _context.KeyRing.ToListAsync();
                if (itemsToDelete?.Count > 0)
                {
                    _context.KeyRing.RemoveRange(itemsToDelete);
                    await _context.SaveChangesAsync();
                }

                return $"{itemsToDelete?.Count}, records have been deleted";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting keys from the database.");
                return "Error occurred while deleting keys.";
            }
        }

        #endregion
    }
}
