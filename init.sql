-- Create the database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'RecordSign')
    CREATE DATABASE [RecordSign];
GO

-- Use the database
USE [RecordSign];
GO

IF OBJECT_ID('SignedRecords', 'U') IS NOT NULL
    DROP TABLE SignedRecords;
IF OBJECT_ID('Keys', 'U') IS NOT NULL
    DROP TABLE Keys;
-- Drop existing objects if they exist
IF OBJECT_ID('Records', 'U') IS NOT NULL
    DROP TABLE Records;

-- Create the tables

CREATE TABLE Keys (
    key_id INT IDENTITY(1,1) PRIMARY KEY,
    key_name VARCHAR(250),
    key_data varbinary(max),
    is_in_use BIT DEFAULT (0),
    last_used_at DATETIME DEFAULT (getdate())
);

CREATE TABLE Records (
    record_id INT IDENTITY(1,1) PRIMARY KEY,
    batch_id INT DEFAULT (0),
    record_data VARCHAR(max),
    is_signed BIT DEFAULT (0)
);

CREATE TABLE SignedRecords (
    record_id INT PRIMARY KEY FOREIGN KEY REFERENCES Records(record_id),
    batch_id INT,
    key_name VARCHAR(250),
    signature_data VARCHAR(max),
    signed_timestamp DATETIME DEFAULT (getdate())
);

GO

--- Seed with some random data in Records table

-- Use the database
USE [RecordSign];
GO

-- Drop the temporary table if it already exists
IF OBJECT_ID('tempdb..#BlockchainTechnologies') IS NOT NULL
    DROP TABLE #BlockchainTechnologies;

-- Create the temporary table to hold the dictionary of blockchain technologies
CREATE TABLE #BlockchainTechnologies (
    id INT IDENTITY(1,1) PRIMARY KEY,
    TechnologyName VARCHAR(50),
    TokenSymbol VARCHAR(10),
    UseCaseDescription VARCHAR(100)
);

-- Insert the dictionary of blockchain technologies
INSERT INTO #BlockchainTechnologies (TechnologyName, TokenSymbol, UseCaseDescription)
VALUES
    ('Bitcoin', 'BTC', 'Decentralized digital currency'),
    ('Ethereum', 'ETH', 'Smart contracts and decentralized applications'),
    ('Ripple', 'XRP', 'Real-time gross settlement system and remittance network'),
    ('Cardano', 'ADA', 'Platform for building decentralized applications'),
    ('Polkadot', 'DOT', 'Multi-chain interoperability protocol'),
    ('Chainlink', 'LINK', 'Decentralized oracle network for smart contracts'),
    ('Stellar', 'XLM', 'Fast and low-cost cross-border transactions'),
    ('Tezos', 'XTZ', 'Self-amending blockchain with on-chain governance'),
    ('Filecoin', 'FIL', 'Decentralized storage network'),
    ('VeChain', 'VET', 'Blockchain-based supply chain management');

-- Generate and insert random data into the "Records" table
DECLARE @Counter INT = 0;

WHILE @Counter < 20
BEGIN
    SET @Counter = @Counter + 1
    
    -- Generate random values for each field
    DECLARE @RandomIndex INT = CAST((RAND() * (SELECT MAX(id) FROM #BlockchainTechnologies)) + 1 AS INT);
    DECLARE @RecordData VARCHAR(100);
    
    SELECT @RecordData = TechnologyName + ' - ' + UseCaseDescription + ' - ' + TokenSymbol + '_' + CAST(NEWID() AS VARCHAR(100))
    FROM #BlockchainTechnologies
    WHERE id = @RandomIndex;

    -- Insert the generated data into the "Records" table
    INSERT INTO Records (record_data)
    VALUES (@RecordData);
END

-- Clean up the temporary table
DROP TABLE #BlockchainTechnologies;
