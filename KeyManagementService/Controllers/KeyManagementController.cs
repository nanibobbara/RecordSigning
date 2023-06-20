using Microsoft.AspNetCore.Mvc;
using RecordSigning.Shared;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KeyManagementService.Controllers
{
    /// <summary>
    /// This controller is responsible for managing the keys
    /// </summary>
    [Route("")]
    [ApiController]
    public partial class KeyManagementController : Controller
    {
        private readonly RecordSignDbService _recordSignDbService;
        private readonly MessageQueueService _messageQueueService;
        public KeyManagementController(RecordSignDbService recordSignDbService, MessageQueueService messageQueueService)
        {
            _recordSignDbService = recordSignDbService;
            _messageQueueService = messageQueueService;
        }

        /// <summary>
        /// this method returns the next available key
        /// </summary>
        /// <returns></returns>
        [HttpGet("api/Keys/getNextAvailableKey")]
        public async Task<ActionResult<Key>> GetNextAvailableKey()
        {
            var response = _recordSignDbService.GetNextAvailableKey();

            if (response == null)
            {
                return NotFound("No available keys");
            }

            return response;
        }

        /// <summary>
        /// this method loads the keys from Keyvault, 
        /// this method will be called to load the keys from keyvault before start signing the records
        /// </summary>
        /// <returns></returns>
        [HttpPost("api/Keys/count")]
        public async Task<ActionResult<string>> LoadKeys(int count)
        {
            List<Key> keys = new List<Key>();
            //keys = await _recordSignDbService.LoadKeysFromKeyVault(count); 
            keys = Cryptography.GenerateKeys(count);


            if (keys?.Count == 0)
            {
                return NotFound("Key are not available in Key vault");
            }

            // first delete all the existing keys from keys table and then load from key vault

            var ItemsDeleted = await _recordSignDbService.DeleteKeys();

            var response = await _recordSignDbService.SeedToKeys(keys);

            if (response == null)
            {
                return NotFound("Key are not available in Key vault");
            }

            return response;
        }


        /// <summary>
        /// this method updates the key status, 
        /// this will be called once assinged batch of records are signed and mark the status to available for next batch of records    
        /// </summary>
        /// <param name="keyId"></param>
        /// <param name="isInUse"></param>
        /// <returns></returns>
        [HttpPut("api/Keys")]
        public async Task<ActionResult<Key>> UpdateKeyStatus(int keyId, bool isInUse = false)
        {
            var response = await _recordSignDbService.UpdateKeyStatus(keyId, isInUse);

            if (response == null)
            {
                return NotFound("Key not found");
            }

            return response;
        }

        /// <summary>
        /// this method deletes all the keys ,this method will be called once all the records are signed
        /// </summary>
        /// <returns></returns>
        [HttpDelete("api/Keys")]
        public async Task<ActionResult<string>> DeleteAllKeys()
        {
            var response = await _recordSignDbService.DeleteKeys();

            if (response == null)
            {
                return NotFound("There are no matching records to be deleted");
            }

            return response;
        }


        [HttpPut("api/Record/{batchSize}")]
        public IActionResult SetBatchSize(int batchSize)
        {
            // Publish the batch size to RabbitMQ
            _messageQueueService.PublishBatchSize(batchSize);

            return Ok();
        }

    }
}
