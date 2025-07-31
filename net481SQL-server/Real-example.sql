-- =============================================
-- Real Example: Row-by-Row Encryption/Decryption using SQL Server CLR
-- Demonstrates OPENROWSET usage for processing decrypted values within SQL queries
-- =============================================

-- =============================================
-- STEP 1: Create Sample Tables
-- =============================================

-- Create plain data table (원본 평문 데이터 테이블)
-- This is example, you can use your own table and do not drop it.
-- 이건 예시입니다, 실제 테이블을 사용하시고, drop으로 실제 테이블을 삭제치 않도록 주의하세요.
IF OBJECT_ID('dbo.PlainDataTable', 'U') IS NOT NULL
    DROP TABLE dbo.PlainDataTable;
GO

CREATE TABLE dbo.PlainDataTable (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    SSN NVARCHAR(11),
    CreditCard NVARCHAR(20),
    Salary DECIMAL(10,2),
    Address NVARCHAR(200),
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    SomeKeyColumn NVARCHAR(50)
);
GO

-- Create encrypted data table (암호화된 데이터 저장 테이블)
-- 실제로는 미리 만들어진 테이블을 사용하신다고 가정하고, 이 테이블을 삭제하지 않도록 주의하세요.
IF OBJECT_ID('dbo.EncryptedTable', 'U') IS NOT NULL
    DROP TABLE dbo.EncryptedTable;
GO

CREATE TABLE dbo.EncryptedTable (
    ID INT PRIMARY KEY,
    EncryptedData NVARCHAR(MAX) NOT NULL,
    SomeFlag INT DEFAULT 0,
    EncryptedAt DATETIME2 DEFAULT GETDATE(),
    Password NVARCHAR(100) NOT NULL -- 실제 환경에서는 안전하게 저장해야 함
);
GO

-- Insert sample data into plain table
-- 샘플 데이터를 넣습니다.
INSERT INTO dbo.PlainDataTable (CustomerName, Email, Phone, SSN, CreditCard, Salary, Address, SomeKeyColumn)
VALUES 
    ('John Doe', 'john.doe@email.com', '+1-555-0101', '123-45-6789', '4111-1111-1111-1111', 75000.00, '123 Main St, City, State', 'KEY001'),
    ('Jane Smith', 'jane.smith@email.com', '+1-555-0102', '987-65-4321', '4222-2222-2222-2222', 82000.00, '456 Oak Ave, City, State', 'KEY002'),
    ('Bob Johnson', 'bob.johnson@email.com', '+1-555-0103', '456-78-9012', '4333-3333-3333-3333', 65000.00, '789 Pine Rd, City, State', 'KEY003'),
    ('Alice Brown', 'alice.brown@email.com', '+1-555-0104', '789-01-2345', '4444-4444-4444-4444', 95000.00, '321 Elm St, City, State', 'KEY004'),
    ('Charlie Wilson', 'charlie.wilson@email.com', '+1-555-0105', '321-54-6789', '4555-5555-5555-5555', 70000.00, '654 Maple Dr, City, State', 'KEY005');
GO

-- =============================================
-- STEP 2: Encrypt Data Row by Row using CLR
-- =============================================

-- Cursor-based encryption using CLR functions
-- 커서를 사용하여 한 행씩 암호화합니다.
DECLARE @ID INT;
DECLARE @rowXml XML;
DECLARE @encryptedRow NVARCHAR(MAX);
DECLARE @password NVARCHAR(MAX) = 'MySecurePassword123!';
DECLARE @iterations INT = 1500; 
-- 실제 PBKDF2 는 100,000을 순수 비밀번호 저장용으로 권고하나, 이 예제에서는 1500으로 설정했습니다.
-- 열 암호화 복호화 시 열 당 0.1~0.2초 소요되는 기준으로 가정했습니다. 
-- 데이터 암호화 용 키이니 반복횟수가 길어 비밀번호에서 암호화 키 계산하는 데 걸리는 시간이 너무 길 필요가 없습니다.
-- 로그인 권장사항은 0.5초 이상 계산이 필요하여 100,000 혹은 그 이상이 권고되는 점 참고해주세요.
-- Real PBKDF2 using case, 100,000 is recommended. for Password hashing about login process
-- usually login process need 0.5 second or more, so 100,000 or more is recommended.
-- For data encryption, the key is needed, so the number of iterations is not too long.

-- Declare cursor for processing plain data
-- 평문 데이터를 처리하기 위한 커서를 선언합니다.
DECLARE encrypt_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT ID 
    FROM dbo.PlainDataTable 
    ORDER BY ID;

OPEN encrypt_cursor;
FETCH NEXT FROM encrypt_cursor INTO @ID;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Get single row as XML (PowerBuilder style - always use TOP 1)
    -- 평문 데이터를 열 단위로 XML로 변환합니다.
    SET @rowXml = (
        SELECT (
            SELECT TOP 1 * 
            FROM dbo.PlainDataTable 
            WHERE ID = @ID 
            FOR XML RAW('Row'), ELEMENTS XSINIL, BINARY BASE64, XMLSCHEMA, TYPE
        ) AS 'RowData'
        FOR XML PATH('root'), TYPE
    );

    -- Encrypt the row using CLR procedure
    -- 평문 데이터를 암호화합니다.
    EXEC dbo.EncryptRowWithMetadata 
        @rowXml = @rowXml,
        @password = @password,
        @iterations = @iterations,
        @encryptedRow = @encryptedRow OUTPUT;

    -- Store encrypted row
    -- 암호화된 데이터를 저장합니다.
    INSERT INTO dbo.EncryptedTable (ID, EncryptedData, SomeFlag, Password)
    VALUES (@ID, @encryptedRow, 1, @password);

    FETCH NEXT FROM encrypt_cursor INTO @ID;
END

CLOSE encrypt_cursor;
DEALLOCATE encrypt_cursor;
GO

-- =============================================
-- STEP 3: Decrypt Data using Stored Procedure Result Set and Process Results
-- =============================================

-- 1) PlainDataTable과 동일한 스키마의 빈 임시테이블 생성 (모든 열 포함)
-- 1) create empty table with the same schema as PlainDataTable (including all columns)
SELECT TOP 0 *
INTO #tmp
FROM dbo.PlainDataTable;
GO

-- IDENTITY_INSERT를 ON으로 설정하여 ID 열에 명시적 값 삽입 가능하게 함
-- Set IDENTITY_INSERT ON to allow explicit values for ID column
SET IDENTITY_INSERT #tmp ON;
GO

-- 2) 복호화할 원본 테이블에서 커서로 한 행씩 처리
-- 2) process one row at a time from the original table to decrypt
DECLARE
    @ID            INT,
    @EncryptedData NVARCHAR(MAX),
    @password      NVARCHAR(MAX);

DECLARE decrypt_cur CURSOR LOCAL FAST_FORWARD FOR
SELECT ID, EncryptedData, Password
FROM dbo.EncryptedTable
WHERE SomeFlag = 1;

OPEN decrypt_cur;
FETCH NEXT FROM decrypt_cur INTO @ID, @EncryptedData, @password;
WHILE @@FETCH_STATUS = 0
BEGIN
    
    -- Stored Procedure 결과를 직접 INSERT (작동하는 방법)
    -- Direct INSERT from stored procedure result set (working method)
    INSERT INTO #tmp (ID, CustomerName, Email, Phone, SSN, CreditCard, Salary, Address, CreatedDate, SomeKeyColumn)
    EXEC dbo.DecryptRowWithMetadata 
        @encryptedRow = @EncryptedData,
        @password = @password;

    FETCH NEXT FROM decrypt_cur INTO @ID, @EncryptedData, @password;
END

CLOSE decrypt_cur;
DEALLOCATE decrypt_cur;
GO

-- IDENTITY_INSERT를 OFF로 설정
-- Set IDENTITY_INSERT OFF
SET IDENTITY_INSERT #tmp OFF;
GO

-- 3) 평문 데이터도 같은 테이블에 INSERT
-- 3) insert plain text data into the same table
INSERT INTO #tmp
SELECT *
FROM dbo.PlainDataTable
WHERE CreatedDate >= '2025-07-01';
GO

-- 4) 누적된 결과 확인
-- 4) check the accumulated result
SELECT *
FROM #tmp
ORDER BY SomeKeyColumn;
GO

-- 5) clean up
DROP TABLE #tmp;
GO

-- =============================================
-- CLEANUP: Remove Created Objects
-- =============================================
PRINT '';
PRINT '=== CLEANUP: Removing Created Objects ===';
GO

-- Drop tables
IF OBJECT_ID('dbo.EncryptedTable', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.EncryptedTable;
    PRINT '✓ Dropped table: dbo.EncryptedTable';
END
GO

IF OBJECT_ID('dbo.PlainDataTable', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.PlainDataTable;
    PRINT '✓ Dropped table: dbo.PlainDataTable';
END
GO

PRINT '';
PRINT '=== CLEANUP COMPLETED ===';
PRINT 'All example objects have been removed.';
PRINT 'Note: In real scenarios, you would keep your actual data tables.';
PRINT 'These cleanup statements are only for example purposes.';
GO
