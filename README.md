
# Record Signing Service 

The purpose of this document is to provide a detailed design for the implementation of the Record Signing Service. The architecture aims to implement a record signing service using a message-driven/microservice solution, ensuring secure and efficient signing of records.



## Documentation


## Features

    * Implement a record signing service using a message-driven/microservice solution.
    * Process and sign batches of records concurrently.
    * Store the signatures in a database until all records are signed.
    * Ensure no double signing of records.
    * Prevent concurrent usage of the same key.
    * Use a single key for signing all records in a batch.
    * Select keys from least recently used to most recently used.
    * Allow the batch size to be configurable by the user.
    * Use a public key crypto algorithm of choice.

## Solution Overview

The proposed solution consists of the following key components:

**KeyManagementService**

Manages the collection of private keys, ensures key availability, and prevents concurrent usage.This is the only interface where users can interact with the system to input data.

**Responsibilities**

    * Ensuring that the keys are secure.
    * Providing a consistent way to access the available key to use for signing.
    * Keeping track of which keys are used for which records.
    * Allowing users to generate (load from vault) new keys when needed.
    * Deleting keys that are no longer needed.

**Workflow**

The workflow for using the KeyManagementService is as follows:

    1. The user requests a private keys to load from keyVault.
    2. The KeyManagementService generates/load keys and store in database for signing.
    3. Dedicated endpoint created to get the next avaible key for signing the whole batch of records.
    4. Here user can set the batch size, Soon after it receives an input fron user using dedicate endpoint to feed it publish it on Messing queue.

The KeyManagementService is a critical component of the system, as it ensures that the keys are secure and that the data is protected. It is important to follow the workflow carefully to ensure that the keys are used correctly and that the data is not compromised.

**BatchProcessingService**

The Batch Processing Service is responsible for processing batches of records. It is a message-driven microservice that receives batch size from the KeyManagementService and then processes them in parallel. The service uses a distributed architecture to scale to handle large volumes of data.

**Responsibilities**

    * Receive batch size from the KeyManagementService.
    * Partined all unsinged records and assign the batch ids to each batch of given size and store them back in database, by default all records are unsigned.    
    * Same set of records published in "UsingnedQueue" on Messaging Queue for further processing    

**Workflow**

    1.The KeyManagementService publishes a batch size the Message queue.
    2.The Batch Processing subscribed to receive batch size from the Message queue.
    3.The Batch Processing Service divide all available unsigned records and group them as batches and updated in database in parallel.
    5.The Batch Processing Service publish them on Queue as "Unsigned Records"

**SigningService**

The SigningService is responsible for signing batches of records. It is a message-driven microservice that receives batches of records from the Batch Processing Service and then signs them using the appropriate avaible key from keyring. The service uses a distributed architecture to scale to handle large volumes of data.

**Responsibilities**

     * Receive batches of records from the Batch Processing Service.
     * Sign the batches of records using the appropriate key from KeyManagementService
     * publish the signed records on another Queue called "SignedRecords" for further processing.

**Workflow**

    1.The Batch Processing Service publishes a batch of un signed records on the Message queue.
    2.The SigningService subscribes to receive batches of records from the Message queue.
    3.The SigningService make a call to KeyManagementServie to get next available key to signs the batches of records .
    4.The SigningService publish signed records in "Signed Records" queue.
   

**RecordKeepingService**

The Record Keeping Service is responsible for storing the signed records in a database. It is a message-driven microservice that receives signed records from the SigningService and then stores them in the database. The service uses a distributed architecture to scale to handle large volumes of data.

**Responsibilities**

    * Receive signed records from the SigningService.
    * Store the signed records in the database.

**Workflow**

    1.The SigningService publishes a batch of signed records on the Message queue.
    2.The Record Keeping Service subscribes to receive signed records from the Message queue.
    3.The Record Keeping Service stores the signed records in the database.
    


## Screenshots

![Components Interaction](https://github.com/nanibobbara/RecordSigning/blob/main/Components.png)

![Sequence](https://github.com/nanibobbara/RecordSigning/blob/main/Flow.PNG)

![ER](https://github.com/nanibobbara/RecordSigning/blob/main/ER.PNG)

## Installation

C:\RecordSignService

    ├── KeyManagementService
    │   ├── Dockerfile.yml
    ├── BatchProcessingService
    │   ├── Dockerfile.yml
    ├── SigningService
    │   ├── Dockerfile.yml
    └── RecordkeepingService
    ├── ├── Dockerfile.yml
    └── docker-compose.yml


Steps to follow:

1. Git clone https://github.com/nanibobbara/RecordSigning.git
2. Go to root floder from the command promt 
3. Type docker-compose up 
4. Above command create and launch all the docker required containers 
5. Connect local database SQL server run the attacched SQL script which has all necessary schema defenations and seeding scripts


```
    
