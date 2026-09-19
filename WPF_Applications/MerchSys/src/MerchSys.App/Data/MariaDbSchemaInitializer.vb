Imports System
Imports System.IO
Imports System.Reflection
Imports System.Security.Cryptography
Imports System.Text
Imports MySqlConnector
Imports Microsoft.Extensions.Logging
Imports MerchSys.App.Services

Namespace Data

    ''' <summary>
    ''' Handles central MariaDB database schema bootstrapping at application startup.
    ''' Enumerates, validates, and runs versioned SQL scripts in lexicographic order.
    ''' Implements strict SHA-256 hash drift detection and aborts launch on tampering.
    ''' </summary>
    Public Module MariaDbSchemaInitializer

        ''' <summary>
        ''' Initializes the central MariaDB database schema and seeds reference/system data.
        ''' Throws a fatal exception if schema drift is detected or a migration fails.
        ''' </summary>
        Public Sub Initialize(connectionString As String, logger As ILogger)
            If logger Is Nothing Then Throw New ArgumentNullException(NameOf(logger))

            logger.LogInformation("MariaDB Schema Bootstrap: Starting schema initialization...")

            Using conn As New MySqlConnection(connectionString)
                Try
                    conn.Open()
                Catch ex As Exception
                    logger.LogCritical(ex, "FATAL: Cannot open connection to MariaDB centralized host.")
                    Throw
                End Try

                ' 1. Ensure __SchemaMigrations table exists
                EnsureSchemaMigrationsTable(conn)

                ' 2. Enumerate and run embedded migrations
                Dim asm = Assembly.GetExecutingAssembly()
                Dim resourceNames = asm.GetManifestResourceNames().
                    Where(Function(r) r.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)).
                    OrderBy(Function(r) r).
                    ToList()

                logger.LogInformation("Found {Count} embedded schema scripts.", resourceNames.Count)

                For Each resName In resourceNames
                    ' Extract filename (e.g. "0001_initial_schema.sql")
                    Dim parts = resName.Split("."c)
                    Dim fileName = parts(parts.Length - 2) & "." & parts(parts.Length - 1)

                    ' Read script content
                    Dim scriptContent As String
                    Using stream = asm.GetManifestResourceStream(resName)
                        If stream Is Nothing Then Continue For
                        Using reader As New StreamReader(stream)
                            scriptContent = reader.ReadToEnd()
                        End Using
                    End Using

                    ' Compute Sha256 hash of the script content
                    Dim currentHash = ComputeSha256(scriptContent)
                    logger.LogInformation("Evaluating script: {FileName} (Hash: {Hash})", fileName, currentHash)

                    ' Check database record
                    Dim dbHash = GetAppliedMigrationHash(conn, fileName)

                    If dbHash Is Nothing Then
                        ' Script is not applied yet — apply it!
                        logger.LogWarning("Applying pending migration: {FileName}", fileName)
                        ApplyMigrationScript(conn, fileName, scriptContent, currentHash, logger)
                        logger.LogInformation("Successfully applied migration: {FileName}", fileName)
                    Else
                        ' Script has been applied — verify hash integrity
                        If dbHash <> currentHash Then
                            logger.LogCritical("CRITICAL: Schema drift detected! The script '{FileName}' has been tampered with. Expected: {Expected}, Actual: {Actual}. ABORTING APP.", fileName, dbHash, currentHash)
                            Throw New InvalidOperationException($"FATAL: Schema drift detected in script '{fileName}'. Aborting startup.")
                        Else
                            logger.LogInformation("Integrity check passed for migration: {FileName}", fileName)
                        End If
                    End If
                Next

                ' 3. Seed user accounts if not present
                EnsureUserAccountsSeeded(conn, logger)

                ' 4. Ensure the internal Developer account exists (idempotent — runs even on already-seeded DBs)
                EnsureDeveloperAccount(conn, logger)

                logger.LogInformation("MariaDB Schema Bootstrap: Initialized successfully with 0 errors.")
            End Using
        End Sub

        Private Sub EnsureSchemaMigrationsTable(conn As MySqlConnection)
            Dim sql = "CREATE TABLE IF NOT EXISTS `__SchemaMigrations` (" &
                      "  `ScriptName` VARCHAR(128) NOT NULL," &
                      "  `AppliedAt`  DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6)," &
                      "  `Sha256`     VARCHAR(64)  NOT NULL," &
                      "  PRIMARY KEY (`ScriptName`)" &
                      ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;"

            Using cmd = conn.CreateCommand()
                cmd.CommandText = sql
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Function GetAppliedMigrationHash(conn As MySqlConnection, scriptName As String) As String
            Dim sql = "SELECT `Sha256` FROM `__SchemaMigrations` WHERE `ScriptName` = @name"
            Using cmd = conn.CreateCommand()
                cmd.CommandText = sql
                cmd.Parameters.AddWithValue("@name", scriptName)
                Dim val = cmd.ExecuteScalar()
                If val Is Nothing OrElse IsDBNull(val) Then Return Nothing
                Return Convert.ToString(val)
            End Using
        End Function

        Private Sub ApplyMigrationScript(conn As MySqlConnection, scriptName As String, scriptContent As String, sha256 As String, logger As ILogger)
            Using tx = conn.BeginTransaction()
                Try
                    ' Parse scriptContent into individual statements, respecting DELIMITER //
                    Dim statements As New List(Of String)()
                    Dim lines = scriptContent.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.None)
                    Dim currentDelimiter As String = ";"
                    Dim sb As New StringBuilder()

                    For Each line In lines
                        Dim trimmed = line.Trim()
                        If trimmed.StartsWith("DELIMITER", StringComparison.OrdinalIgnoreCase) Then
                            ' Extract new delimiter
                            Dim parts = trimmed.Split(New Char() {" "c, vbTab}, StringSplitOptions.RemoveEmptyEntries)
                            If parts.Length > 1 Then
                                currentDelimiter = parts(1)
                            End If
                            Continue For
                        End If

                        If sb.Length > 0 Then
                            sb.Append(vbCrLf)
                        End If
                        sb.Append(line)

                        ' Check if statement ends with current delimiter
                        If trimmed.EndsWith(currentDelimiter) Then
                            Dim stmt = sb.ToString()
                            ' Strip the delimiter at the end for execution
                            If stmt.EndsWith(currentDelimiter) Then
                                stmt = stmt.Substring(0, stmt.Length - currentDelimiter.Length)
                            End If
                            Dim cleaned = stmt.Trim()
                            If Not String.IsNullOrEmpty(cleaned) Then
                                statements.Add(cleaned)
                            End If
                            sb.Clear()
                        End If
                    Next

                    ' Add residual statement if any
                    Dim residual = sb.ToString().Trim()
                    If Not String.IsNullOrEmpty(residual) Then
                        statements.Add(residual)
                    End If

                    ' Execute statements individually
                    For Each stmt In statements
                        Using cmd = conn.CreateCommand()
                            cmd.Transaction = tx
                            cmd.CommandText = stmt
                            cmd.ExecuteNonQuery()
                        End Using
                    Next

                    ' Record execution in __SchemaMigrations table
                    Dim sql = "INSERT INTO `__SchemaMigrations` (`ScriptName`, `Sha256`, `AppliedAt`) VALUES (@name, @hash, UTC_TIMESTAMP(6))"
                    Using cmd = conn.CreateCommand()
                        cmd.Transaction = tx
                        cmd.CommandText = sql
                        cmd.Parameters.AddWithValue("@name", scriptName)
                        cmd.Parameters.AddWithValue("@hash", sha256)
                        cmd.ExecuteNonQuery()
                    End Using

                    tx.Commit()
                Catch ex As Exception
                    tx.Rollback()
                    logger.LogError(ex, "Failed to apply migration: {FileName}. Transaction rolled back.", scriptName)
                    Throw
                End Try
            End Using
        End Sub

        Private Sub EnsureUserAccountsSeeded(conn As MySqlConnection, logger As ILogger)
            ' Check if Sys_UserAccounts table is empty
            Dim count As Integer = 0
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "SELECT COUNT(*) FROM `Sys_UserAccounts`"
                count = Convert.ToInt32(cmd.ExecuteScalar())
            End Using

            If count > 0 Then Return

            logger.LogWarning("Sys_UserAccounts table is empty. Seeding default manager and owner accounts...")

            ' Seed manager and owner using PasswordHashHelper
            Dim defaultHash = PasswordHashHelper.Hash("Vista2026!")
            Dim now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")

            Using tx = conn.BeginTransaction()
                Try
                    ' Seed manager (Role = 1)
                    Using cmd = conn.CreateCommand()
                        cmd.Transaction = tx
                        cmd.CommandText = "INSERT INTO `Sys_UserAccounts` " &
                                          "(Username, PasswordHash, Role, IsActive, FailedLoginAttempts, LockedUntil, LastPasswordChangeAt, CreatedAt, ModifiedAt) " &
                                          "VALUES (@u, @h, @r, 1, 0, NULL, NULL, @t, NULL)"
                        cmd.Parameters.AddWithValue("@u", "manager")
                        cmd.Parameters.AddWithValue("@h", defaultHash)
                        cmd.Parameters.AddWithValue("@r", 1)
                        cmd.Parameters.AddWithValue("@t", now)
                        cmd.ExecuteNonQuery()
                    End Using

                    ' Seed owner (Role = 2, separate salt)
                    Dim ownerHash = PasswordHashHelper.Hash("Vista2026!")
                    Using cmd = conn.CreateCommand()
                        cmd.Transaction = tx
                        cmd.CommandText = "INSERT INTO `Sys_UserAccounts` " &
                                          "(Username, PasswordHash, Role, IsActive, FailedLoginAttempts, LockedUntil, LastPasswordChangeAt, CreatedAt, ModifiedAt) " &
                                          "VALUES (@u, @h, @r, 1, 0, NULL, NULL, @t, NULL)"
                        cmd.Parameters.AddWithValue("@u", "owner")
                        cmd.Parameters.AddWithValue("@h", ownerHash)
                        cmd.Parameters.AddWithValue("@r", 2)
                        cmd.Parameters.AddWithValue("@t", now)
                        cmd.ExecuteNonQuery()
                    End Using

                    tx.Commit()
                    logger.LogInformation("Successfully seeded default manager and owner accounts.")
                Catch ex As Exception
                    tx.Rollback()
                    logger.LogError(ex, "Failed to seed user accounts. Transaction rolled back.")
                    Throw
                End Try
            End Using
        End Sub

        ''' <summary>
        ''' Idempotently provisions the internal Developer account (Role = Developer = 3).
        ''' Runs on every startup so the account is created even when the database was seeded
        ''' before this account existed. Unlike the manager/owner seeds, LastPasswordChangeAt is
        ''' set on creation so the Developer logs in directly with the supplied password (no DA6
        ''' first-login change). The Developer role is a Manager superset with exclusive access to
        ''' the Developer Tools module.
        ''' </summary>
        Private Sub EnsureDeveloperAccount(conn As MySqlConnection, logger As ILogger)
            Dim exists As Integer = 0
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "SELECT COUNT(*) FROM `Sys_UserAccounts` WHERE lower(Username) = 'developer'"
                exists = Convert.ToInt32(cmd.ExecuteScalar())
            End Using

            If exists > 0 Then Return

            logger.LogWarning("Developer account not found. Provisioning internal Developer account...")

            Dim devHash = PasswordHashHelper.Hash("DevPassword")
            Dim now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")

            Using cmd = conn.CreateCommand()
                cmd.CommandText = "INSERT INTO `Sys_UserAccounts` " &
                                  "(Username, PasswordHash, Role, IsActive, FailedLoginAttempts, LockedUntil, LastPasswordChangeAt, CreatedAt, ModifiedAt) " &
                                  "VALUES (@u, @h, @r, 1, 0, NULL, @t, @t, NULL)"
                cmd.Parameters.AddWithValue("@u", "Developer")
                cmd.Parameters.AddWithValue("@h", devHash)
                cmd.Parameters.AddWithValue("@r", 3)
                cmd.Parameters.AddWithValue("@t", now)
                cmd.ExecuteNonQuery()
            End Using

            logger.LogInformation("Successfully provisioned the internal Developer account.")
        End Sub

        Private Function ComputeSha256(text As String) As String
            Using sha As SHA256 = SHA256.Create()
                Dim bytes = Encoding.UTF8.GetBytes(text)
                Dim hashBytes = sha.ComputeHash(bytes)
                Dim sb As New StringBuilder()
                For Each b In hashBytes
                    sb.Append(b.ToString("x2"))
                Next
                Return sb.ToString()
            End Using
        End Function

    End Module

End Namespace
