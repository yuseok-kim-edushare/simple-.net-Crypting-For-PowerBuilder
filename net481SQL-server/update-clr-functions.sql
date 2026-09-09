-- =============================================
-- SQL Server CLR Functions and Procedures In-Place Update Script
-- For SecureLibrary.SQL Assembly
-- 
-- [목적]
-- 기존 설치된 SecureLibrary.SQL 어셈블리를 개체 삭제(DROP) 없이
-- 안전하게 인플레이스(In-Place)로 업데이트(ALTER ASSEMBLY)하고,
-- 권한(GRANT EXECUTE) 및 의존성(Stored Procedure/View/Trigger 등)을 유지합니다.
-- =============================================

PRINT '=============================================================';
PRINT '=== SecureLibrary.SQL CLR 어셈블리 업데이트 설치 시작 ===';
PRINT '=============================================================';
GO

-- =============================================
-- STEP 1: CLR 통합 설정 활성화 확인
-- =============================================
PRINT '';
PRINT '--- STEP 1: CLR 환경 확인 ---';
GO

IF (SELECT CAST(value_in_use AS INT) FROM sys.configurations WHERE name = 'clr enabled') = 0
BEGIN
    PRINT 'CLR이 비활성화되어 있어 활성화합니다...';
    EXEC sp_configure 'show advanced options', 1;
    RECONFIGURE;
    EXEC sp_configure 'clr enabled', 1;
    RECONFIGURE;
    PRINT '✓ CLR Integration 활성화 완료';
END
ELSE
BEGIN
    PRINT '✓ CLR Integration이 이미 활성화되어 있습니다.';
END
GO

-- =============================================
-- STEP 2: 신뢰할 수 있는 어셈블리 관리(기존 해시 정리 및 신규 등록) 및 어셈블리 업데이트 (ALTER ASSEMBLY)
-- =============================================
PRINT '';
PRINT '--- STEP 2: 어셈블리 신뢰 등록 및 인플레이스 업데이트 ---';
GO

-- ★★★ 배포할 DLL 파일 경로 및 어셈블리 식별 설명 ★★★
DECLARE @dllPath NVARCHAR(500) = N'C:\CLR\SecureLibrary-SQL.dll'; 
DECLARE @assemblyDesc NVARCHAR(500) = N'SecureLibrary.SQL Assembly';

DECLARE @sql NVARCHAR(MAX);
DECLARE @newHash VARBINARY(64);

-- 1. DLL 파일로부터 SHA2-512 해시 계산
BEGIN TRY
    SET @sql = N'SELECT @calculatedHash = HASHBYTES(''SHA2_512'', BulkColumn) ' +
               N'FROM OPENROWSET(BULK ''' + REPLACE(@dllPath, '''', '''''') + ''', SINGLE_BLOB) AS x;';
    EXEC sp_executesql @sql, N'@calculatedHash VARBINARY(64) OUTPUT', @calculatedHash = @newHash OUTPUT;
    PRINT '✓ DLL 파일 새 해시(SHA2-512) 계산 성공: ' + CONVERT(NVARCHAR(150), @newHash, 1);
END TRY
BEGIN CATCH
    PRINT '❌ DLL 파일을 읽거나 해시를 계산하는 중 오류가 발생했습니다.';
    PRINT '   경로를 확인하세요: ' + @dllPath;
    PRINT '   오류 메시지: ' + ERROR_MESSAGE();
    RAISERROR('DLL 해시 계산 실패로 인해 업데이트를 중단합니다.', 16, 1);
    RETURN;
END CATCH

-- 2. sp_drop_trusted_assembly / sp_add_trusted_assembly 처리 (SQL Server 2017+ clr strict security 대비)
-- 컴파일마다 바이너리 해시가 변경되므로, 같은 식별 설명(Description)을 가진 이전 해시들을 먼저 조회하여 정리(Drop)합니다.
IF EXISTS (SELECT 1 FROM sys.all_objects WHERE name = 'trusted_assemblies')
BEGIN
    DECLARE @oldHash VARBINARY(64);
    DECLARE @oldDesc NVARCHAR(4000);

    DECLARE cur_old_assemblies CURSOR LOCAL FAST_FORWARD FOR
        SELECT [hash], [description]
        FROM sys.trusted_assemblies
        WHERE ([description] = @assemblyDesc OR [description] LIKE @assemblyDesc + N'%' OR [description] LIKE N'SecureLibrary%SQL%')
          AND [hash] <> @newHash;

    OPEN cur_old_assemblies;
    FETCH NEXT FROM cur_old_assemblies INTO @oldHash, @oldDesc;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            EXEC sys.sp_drop_trusted_assembly @hash = @oldHash;
            PRINT '✓ 이전 버전의 신뢰 어셈블리 해시를 삭제했습니다. (' + ISNULL(@oldDesc, N'') + ' / ' + CONVERT(NVARCHAR(50), @oldHash, 1) + ')';
        END TRY
        BEGIN CATCH
            PRINT '⚠️ 이전 해시 삭제 실패 (무시하고 계속 진행): ' + ERROR_MESSAGE();
        END CATCH

        FETCH NEXT FROM cur_old_assemblies INTO @oldHash, @oldDesc;
    END

    CLOSE cur_old_assemblies;
    DEALLOCATE cur_old_assemblies;

    -- 새 해시가 미등록 상태이면 등록
    IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE [hash] = @newHash)
    BEGIN
        EXEC sys.sp_add_trusted_assembly @hash = @newHash, @description = @assemblyDesc;
        PRINT '✓ 새 어셈블리 해시가 sys.trusted_assemblies 에 신규 등록되었습니다.';
    END
    ELSE
    BEGIN
        PRINT '✓ 새 어셈블리 해시가 이미 sys.trusted_assemblies 에 등록되어 있습니다.';
    END
END

-- 3. 어셈블리 인플레이스 업데이트 (ALTER ASSEMBLY) 또는 신규 생성 (CREATE ASSEMBLY)
-- 주의: SQL Server 내부에서는 CLR 어셈블리 풀네임(clr_name, 예: securelibrary-sql, ...)을 기준으로 충돌을 감지합니다.
-- 따라서 sys.assemblies 에서 SQL 등록명이 다르더라도(예: 'SecureLibrary-SQL' vs 'SecureLibrary.SQL')
-- 동일한 CLR Identity를 가진 어셈블리가 이미 존재할 수 있으므로 이를 함께 확인합니다.
DECLARE @existingAssemblyName sysname = NULL;

-- 3-1. 기존 어셈블리 이름 확인 (CLR clr_name 또는 name 검색)
SELECT TOP 1 @existingAssemblyName = name
FROM sys.assemblies
WHERE name IN ('SecureLibrary.SQL', 'SecureLibrary-SQL')
   OR clr_name LIKE 'securelibrary-sql,%'
   OR clr_name LIKE 'SecureLibrary.SQL,%';

IF @existingAssemblyName IS NOT NULL
BEGIN
    BEGIN TRY
        PRINT '기존 어셈블리 [' + @existingAssemblyName + ']를 검색했습니다. ALTER ASSEMBLY를 실행합니다...';
        SET @sql = N'ALTER ASSEMBLY [' + @existingAssemblyName + '] FROM ''' + REPLACE(@dllPath, '''', '''''') + ''' WITH PERMISSION_SET = UNSAFE;';
        EXEC sp_executesql @sql;
        PRINT '✓ ALTER ASSEMBLY 성공: 의존성 및 기존 권한 손실 없이 어셈블리가 성공적으로 갱신되었습니다.';
    END TRY
    BEGIN CATCH
        PRINT '❌ ALTER ASSEMBLY 실패!';
        PRINT '   원인: ' + ERROR_MESSAGE();
        PRINT '   (참고: C# 메서드 시그니처가 호환되지 않는 변경이 있는 경우 전체 재설치가 필요할 수 있습니다.)';
        RAISERROR('어셈블리 업데이트 실패', 16, 1);
        RETURN;
    END CATCH
END
ELSE
BEGIN
    BEGIN TRY
        PRINT '기존 어셈블리가 없습니다. CREATE ASSEMBLY를 통해 신규 등록합니다...';
        SET @sql = N'CREATE ASSEMBLY [SecureLibrary.SQL] FROM ''' + REPLACE(@dllPath, '''', '''''') + ''' WITH PERMISSION_SET = UNSAFE;';
        EXEC sp_executesql @sql;
        PRINT '✓ CREATE ASSEMBLY 성공: 어셈블리가 신규 등록되었습니다.';
    END TRY
    BEGIN CATCH
        PRINT '❌ CREATE ASSEMBLY 실패: ' + ERROR_MESSAGE();
        RAISERROR('어셈블리 신규 생성 실패', 16, 1);
        RETURN;
    END CATCH
END
GO

-- =============================================
-- STEP 3: 스칼라 함수 및 저장 프로시저 동기화 (CREATE OR ALTER)
-- =============================================
PRINT '';
PRINT '--- STEP 3: 스칼라 함수 및 저장 프로시저 동기화 (CREATE OR ALTER) ---';
GO

-- 실제 카탈로그에 등록된 어셈블리 이름 동적 감지 (SecureLibrary.SQL 또는 SecureLibrary-SQL)
DECLARE @asmName sysname;
SELECT TOP 1 @asmName = name
FROM sys.assemblies
WHERE name IN ('SecureLibrary.SQL', 'SecureLibrary-SQL')
   OR clr_name LIKE 'securelibrary-sql,%'
   OR clr_name LIKE 'SecureLibrary.SQL,%';

IF @asmName IS NULL
BEGIN
    RAISERROR('카탈로그에서 어셈블리를 찾을 수 없어 동기화를 중단합니다.', 16, 1);
    RETURN;
END

PRINT '타깃 어셈블리 식별 완료: [' + @asmName + ']';

-- 동기화 대상 메타데이터 정의 (스칼라 함수 21개 + 저장 프로시저 7개 = 총 28개)
DECLARE @objects TABLE (
    Id INT IDENTITY(1,1),
    ObjType NVARCHAR(20),       -- 'FUNCTION' or 'PROCEDURE'
    ObjName sysname,
    Params NVARCHAR(MAX),
    ReturnType NVARCHAR(MAX),   -- NULL for PROCEDURE
    MethodClass NVARCHAR(100),  -- 'SqlCLRFunctions' or 'SqlCLRProcedures'
    MethodName NVARCHAR(100)
);

-- 1. 스칼라 함수 21개 등록
INSERT INTO @objects (ObjType, ObjName, Params, ReturnType, MethodClass, MethodName) VALUES
('FUNCTION', 'HashPassword', '@password NVARCHAR(MAX)', 'NVARCHAR(MAX)', 'SqlCLRFunctions', 'HashPassword'),
('FUNCTION', 'HashPasswordWithWorkFactor', '@password NVARCHAR(MAX), @workFactor INT', 'NVARCHAR(MAX)', 'SqlCLRFunctions', 'HashPasswordWithWorkFactor'),
('FUNCTION', 'VerifyPassword', '@password NVARCHAR(MAX), @hashedPassword NVARCHAR(MAX)', 'BIT', 'SqlCLRFunctions', 'VerifyPassword'),
('FUNCTION', 'GenerateSalt', '@workFactor INT', 'NVARCHAR(MAX)', 'SqlCLRFunctions', 'GenerateSalt'),
('FUNCTION', 'EncryptAesGcm', '@plainText NVARCHAR(MAX), @base64Key NVARCHAR(MAX)', 'NVARCHAR(MAX)', 'SqlCLRFunctions', 'EncryptAesGcm'),
('FUNCTION', 'DecryptAesGcm', '@base64EncryptedData NVARCHAR(MAX), @base64Key NVARCHAR(MAX)', 'NVARCHAR(MAX)', 'SqlCLRFunctions', 'DecryptAesGcm'),
('FUNCTION', 'EncryptAesGcmWithPassword', '@plainText NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT', 'NVARCHAR(MAX)', 'SqlCLRFunctions', 'EncryptAesGcmWithPassword'),
('FUNCTION', 'DecryptAesGcmWithPassword', '@base64EncryptedData NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT', 'NVARCHAR(MAX)', 'SqlCLRFunctions', 'DecryptAesGcmWithPassword'),
('FUNCTION', 'GenerateKey', '@keySizeBits INT', 'NVARCHAR(MAX)', 'SqlCLRFunctions', 'GenerateKey'),
('FUNCTION', 'GenerateNonce', '@nonceSizeBytes INT', 'NVARCHAR(MAX)', 'SqlCLRFunctions', 'GenerateNonce'),
('FUNCTION', 'DeriveKeyFromPassword', '@password NVARCHAR(MAX), @base64Salt NVARCHAR(MAX), @iterations INT, @keySizeBytes INT', 'NVARCHAR(MAX)', 'SqlCLRFunctions', 'DeriveKeyFromPassword'),
('FUNCTION', 'EncryptXml', '@xmlData XML, @password NVARCHAR(MAX), @iterations INT', 'NVARCHAR(MAX)', 'SqlCLRFunctions', 'EncryptXml'),
('FUNCTION', 'DecryptXml', '@base64EncryptedXml NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT', 'XML', 'SqlCLRFunctions', 'DecryptXml'),
('FUNCTION', 'EncryptValue', '@value NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT', 'NVARCHAR(MAX)', 'SqlCLRFunctions', 'EncryptValue'),
('FUNCTION', 'DecryptValue', '@encryptedValue NVARCHAR(MAX), @password NVARCHAR(MAX)', 'NVARCHAR(MAX)', 'SqlCLRFunctions', 'DecryptValue'),
('FUNCTION', 'EncryptBinaryValue', '@binaryValue VARBINARY(MAX), @password NVARCHAR(MAX), @iterations INT', 'NVARCHAR(MAX)', 'SqlCLRFunctions', 'EncryptBinaryValue'),
('FUNCTION', 'DecryptBinaryValue', '@encryptedValue NVARCHAR(MAX), @password NVARCHAR(MAX)', 'VARBINARY(MAX)', 'SqlCLRFunctions', 'DecryptBinaryValue'),
('FUNCTION', 'EncryptTable', '@tableName NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT', 'NVARCHAR(MAX)', 'SqlCLRFunctions', 'EncryptTable'),
('FUNCTION', 'EncryptMultiRowXml', '@multiRowXml XML, @password NVARCHAR(MAX), @iterations INT', 'NVARCHAR(MAX)', 'SqlCLRFunctions', 'EncryptMultiRowXml'),
('FUNCTION', 'DecryptMultiRowXml', '@encryptedXml NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT', 'XML', 'SqlCLRFunctions', 'DecryptMultiRowXml'),
('FUNCTION', 'ValidateEncryptionMetadata', '@metadataXml XML', 'XML', 'SqlCLRFunctions', 'ValidateEncryptionMetadata');

-- 2. 저장 프로시저 7개 등록
INSERT INTO @objects (ObjType, ObjName, Params, ReturnType, MethodClass, MethodName) VALUES
('PROCEDURE', 'EncryptTableWithMetadata', '@tableName NVARCHAR(MAX), @password NVARCHAR(MAX), @iterations INT, @encryptedData NVARCHAR(MAX) OUTPUT', NULL, 'SqlCLRProcedures', 'EncryptTableWithMetadata'),
('PROCEDURE', 'DecryptTableWithMetadata', '@encryptedData NVARCHAR(MAX), @password NVARCHAR(MAX), @targetTableName NVARCHAR(MAX)', NULL, 'SqlCLRProcedures', 'DecryptTableWithMetadata'),
('PROCEDURE', 'EncryptRowWithMetadata', '@rowXml XML, @password NVARCHAR(MAX), @iterations INT, @encryptedRow NVARCHAR(MAX) OUTPUT', NULL, 'SqlCLRProcedures', 'EncryptRowWithMetadata'),
('PROCEDURE', 'DecryptRowWithMetadata', '@encryptedRow NVARCHAR(MAX), @password NVARCHAR(MAX)', NULL, 'SqlCLRProcedures', 'DecryptRowWithMetadata'),
('PROCEDURE', 'EncryptMultiRows', '@rowsXml XML, @password NVARCHAR(MAX), @iterations INT, @encryptedRowsXml NVARCHAR(MAX) OUTPUT', NULL, 'SqlCLRProcedures', 'EncryptMultiRows'),
('PROCEDURE', 'DecryptMultiRows', '@encryptedRowsXml NVARCHAR(MAX), @password NVARCHAR(MAX)', NULL, 'SqlCLRProcedures', 'DecryptMultiRows'),
('PROCEDURE', 'GenerateDecryptionScript', '@encryptedRow NVARCHAR(MAX), @password NVARCHAR(MAX), @tempTableName NVARCHAR(128), @script NVARCHAR(MAX) OUTPUT', NULL, 'SqlCLRProcedures', 'GenerateDecryptionScript');

-- 루프를 통한 동적 DDL 생성 및 실행
DECLARE @curId INT = 1;
DECLARE @maxId INT = (SELECT MAX(Id) FROM @objects);
DECLARE @objType NVARCHAR(20), @objName sysname, @params NVARCHAR(MAX), @retType NVARCHAR(MAX), @cls NVARCHAR(100), @mth NVARCHAR(100);
DECLARE @ddl NVARCHAR(MAX);
DECLARE @funcSuccessCount INT = 0;
DECLARE @procSuccessCount INT = 0;

WHILE @curId <= @maxId
BEGIN
    SELECT @objType = ObjType, @objName = ObjName, @params = Params, @retType = ReturnType, @cls = MethodClass, @mth = MethodName
    FROM @objects
    WHERE Id = @curId;

    IF @objType = 'FUNCTION'
    BEGIN
        SET @ddl = N'CREATE OR ALTER FUNCTION dbo.' + QUOTENAME(@objName) + N'(' + @params + N')' + CHAR(13) + CHAR(10) +
                   N'RETURNS ' + @retType + CHAR(13) + CHAR(10) +
                   N'AS EXTERNAL NAME ' + QUOTENAME(@asmName) + N'.[SecureLibrary.SQL.' + @cls + N'].' + @mth + N';';
    END
    ELSE
    BEGIN
        SET @ddl = N'CREATE OR ALTER PROCEDURE dbo.' + QUOTENAME(@objName) + CHAR(13) + CHAR(10) +
                   @params + CHAR(13) + CHAR(10) +
                   N'AS EXTERNAL NAME ' + QUOTENAME(@asmName) + N'.[SecureLibrary.SQL.' + @cls + N'].' + @mth + N';';
    END

    BEGIN TRY
        EXEC sp_executesql @ddl;
        IF @objType = 'FUNCTION'
            SET @funcSuccessCount = @funcSuccessCount + 1;
        ELSE
            SET @procSuccessCount = @procSuccessCount + 1;
    END TRY
    BEGIN CATCH
        PRINT N'❌ 개체 생성/갱신 실패 [' + @objName + N']: ' + ERROR_MESSAGE();
    END CATCH

    SET @curId = @curId + 1;
END

PRINT '✓ 스칼라 함수 ' + CAST(@funcSuccessCount AS NVARCHAR(10)) + ' / 21개 동기화 완료';
PRINT '✓ 저장 프로시저 ' + CAST(@procSuccessCount AS NVARCHAR(10)) + ' / 7개 동기화 완료';
GO

-- =============================================
-- STEP 5: 설치 및 동기화 상태 검증
-- =============================================
PRINT '';
PRINT '--- STEP 5: 어셈블리 및 개체 검증 ---';
GO

-- 1. 어셈블리 정보 조회
SELECT 
    a.name AS AssemblyName,
    a.clr_name AS ClrIdentity,
    a.permission_set_desc AS PermissionSet,
    a.create_date AS CreateDate,
    a.modify_date AS ModifyDate
FROM sys.assemblies a
WHERE a.name IN ('SecureLibrary.SQL', 'SecureLibrary-SQL')
   OR a.clr_name LIKE 'securelibrary-sql,%'
   OR a.clr_name LIKE 'SecureLibrary.SQL,%';

-- 2. 등록된 CLR 함수 개수 검증 (총 21개)
DECLARE @funcCount INT;
SELECT @funcCount = COUNT(*)
FROM sys.objects o
WHERE o.type = 'FS' AND o.name IN (
    'HashPassword', 'HashPasswordWithWorkFactor', 'VerifyPassword', 'GenerateSalt',
    'EncryptAesGcm', 'DecryptAesGcm', 'EncryptAesGcmWithPassword', 'DecryptAesGcmWithPassword',
    'GenerateKey', 'GenerateNonce', 'DeriveKeyFromPassword',
    'EncryptXml', 'DecryptXml', 'ValidateEncryptionMetadata',
    'EncryptValue', 'DecryptValue', 'EncryptBinaryValue', 'DecryptBinaryValue',
    'EncryptTable', 'EncryptMultiRowXml', 'DecryptMultiRowXml'
);
PRINT '등록된 CLR 함수 개수: ' + CAST(@funcCount AS NVARCHAR(10)) + ' / 21개';

-- 3. 등록된 CLR 프로시저 개수 검증 (총 7개)
DECLARE @procCount INT;
SELECT @procCount = COUNT(*)
FROM sys.objects o
WHERE o.type = 'PC' AND o.name IN (
    'EncryptTableWithMetadata', 'DecryptTableWithMetadata',
    'EncryptRowWithMetadata', 'DecryptRowWithMetadata',
    'EncryptMultiRows', 'DecryptMultiRows', 'GenerateDecryptionScript'
);
PRINT '등록된 CLR 프로시저 개수: ' + CAST(@procCount AS NVARCHAR(10)) + ' / 7개';

IF @funcCount = 21 AND @procCount = 7
BEGIN
    PRINT '✓ 모든 CLR 함수 및 프로시저가 정상적으로 등록/동기화되었습니다.';
END
ELSE
BEGIN
    PRINT '⚠️ 일부 개체가 누락되었을 수 있습니다. 확인이 필요합니다.';
END
GO

-- =============================================
-- STEP 6: 런타임 기능 무결성 테스트
-- =============================================
PRINT '';
PRINT '--- STEP 6: 기능 무결성 테스트 ---';
GO

DECLARE @testPassword NVARCHAR(MAX) = 'UpdateTestPW_2026!';
DECLARE @hashedPassword NVARCHAR(MAX);
DECLARE @isPwValid BIT;
DECLARE @plainText NVARCHAR(MAX) = 'Hello SQL CLR Update Verification';
DECLARE @encValue NVARCHAR(MAX);
DECLARE @decValue NVARCHAR(MAX);
DECLARE @binData VARBINARY(MAX) = 0x4142434445;
DECLARE @encBin NVARCHAR(MAX);
DECLARE @decBin VARBINARY(MAX);

BEGIN TRY
    -- 1. Bcrypt 해싱 테스트
    SET @hashedPassword = dbo.HashPassword(@testPassword);
    SET @isPwValid = dbo.VerifyPassword(@testPassword, @hashedPassword);
    PRINT '1. Bcrypt 패스워드 검증: ' + CASE WHEN @isPwValid = 1 THEN 'PASSED ✓' ELSE 'FAILED ❌' END;

    -- 2. 단일 값 AES-GCM 암/복호화 테스트
    SET @encValue = dbo.EncryptValue(@plainText, @testPassword, 10000);
    SET @decValue = dbo.DecryptValue(@encValue, @testPassword);
    PRINT '2. 단일 값 암복호화 검증: ' + CASE WHEN @decValue = @plainText THEN 'PASSED ✓' ELSE 'FAILED ❌' END;

    -- 3. 바이너리 암/복호화 테스트
    SET @encBin = dbo.EncryptBinaryValue(@binData, @testPassword, 10000);
    SET @decBin = dbo.DecryptBinaryValue(@encBin, @testPassword);
    PRINT '3. 바이너리 암복호화 검증: ' + CASE WHEN @decBin = @binData THEN 'PASSED ✓' ELSE 'FAILED ❌' END;

    PRINT '';
    PRINT '=============================================================';
    PRINT '=== SecureLibrary.SQL 어셈블리 업데이트가 성공적으로 완료되었습니다! ===';
    PRINT '=============================================================';
END TRY
BEGIN CATCH
    PRINT '❌ 런타임 테스트 실패: ' + ERROR_MESSAGE();
END CATCH
GO
