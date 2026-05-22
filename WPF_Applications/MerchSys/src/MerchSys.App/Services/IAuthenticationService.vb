Imports System.Security.Cryptography
Imports System.Text
Imports Konscious.Security.Cryptography
Imports Microsoft.Data.Sqlite
Imports MerchSys.SharedKernel.Entities
Imports MerchSys.SharedKernel.Enums

Namespace Services

    ''' <summary>
    ''' Validates credentials, manages account lockout (DA2 — 5 failures = 15 min lock),
    ''' and performs password changes. Uses Argon2id (m=19456 KiB, t=2, p=1, 32-byte hash) per DA4.
    ''' Failure messages never reveal whether a username exists (OWASP guidance).
    ''' </summary>
    Public Interface IAuthenticationService

        ''' <summary>
        ''' Validates credentials. Resets FailedLoginAttempts on success; increments on failure.
        ''' Locks account for 15 minutes after 5 consecutive failures.
        ''' Returns AuthenticationResult.User = Nothing on any failure path.
        ''' </summary>
        Function AuthenticateAsync(username As String, password As String) As Task(Of AuthenticationResult)

        ''' <summary>
        ''' Changes password after verifying currentPassword. Sets LastPasswordChangeAt (clears DA6 first-login flag).
        ''' New password must be &gt;= 8 chars and differ from the current one.
        ''' </summary>
        Function ChangePasswordAsync(userId As Integer, currentPassword As String, newPassword As String) As Task(Of PasswordChangeResult)

    End Interface

    Public Class AuthenticationResult
        Public Property Success As Boolean
        Public Property User As UserAccount
        Public Property FailureReason As String
        Public Property RemainingLockoutMinutes As Integer?
    End Class

    Public Class PasswordChangeResult
        Public Property Success As Boolean
        Public Property ValidationErrors As IReadOnlyList(Of String)
    End Class

    ' ─────────────────────────────────────────────────────────────────────────────
    ' Implementation
    ' ─────────────────────────────────────────────────────────────────────────────

    Public Class AuthenticationService
        Implements IAuthenticationService

        Private ReadOnly _connectionString As String

        Public Sub New(connectionString As String)
            _connectionString = connectionString
        End Sub

        ' ── AuthenticateAsync ──────────────────────────────────────────────────

        Public Function AuthenticateAsync(username As String, password As String) As Task(Of AuthenticationResult) Implements IAuthenticationService.AuthenticateAsync
            Return Task.Run(Function() AuthenticateCore(username, password))
        End Function

        Private Function AuthenticateCore(username As String, password As String) As AuthenticationResult
            Dim user As UserAccount = Nothing

            Using conn As New SqliteConnection(_connectionString)
                conn.Open()
                user = ReadUserByUsername(conn, username)
            End Using

            If user Is Nothing OrElse Not user.IsActive Then
                Return New AuthenticationResult With {
                    .Success = False,
                    .FailureReason = "Invalid credentials"
                }
            End If

            If user.LockedUntil.HasValue AndAlso user.LockedUntil.Value > DateTime.UtcNow Then
                Dim remaining = CInt(Math.Ceiling((user.LockedUntil.Value - DateTime.UtcNow).TotalMinutes))
                Return New AuthenticationResult With {
                    .Success = False,
                    .FailureReason = $"Account locked. Try again in {remaining} minute(s).",
                    .RemainingLockoutMinutes = remaining
                }
            End If

            Dim passwordOk = PasswordHashHelper.Verify(password, user.PasswordHash)

            Using conn As New SqliteConnection(_connectionString)
                conn.Open()
                If passwordOk Then
                    ClearFailedAttempts(conn, user.Id)
                Else
                    BumpFailedAttempts(conn, user.Id, user.FailedLoginAttempts)
                End If
            End Using

            If Not passwordOk Then
                Return New AuthenticationResult With {
                    .Success = False,
                    .FailureReason = "Invalid credentials"
                }
            End If

            Return New AuthenticationResult With {.Success = True, .User = user}
        End Function

        ' ── ChangePasswordAsync ────────────────────────────────────────────────

        Public Function ChangePasswordAsync(userId As Integer, currentPassword As String, newPassword As String) As Task(Of PasswordChangeResult) Implements IAuthenticationService.ChangePasswordAsync
            Return Task.Run(Function() ChangePasswordCore(userId, currentPassword, newPassword))
        End Function

        Private Function ChangePasswordCore(userId As Integer, currentPassword As String, newPassword As String) As PasswordChangeResult
            Dim errs As New List(Of String)()

            If newPassword.Length < 8 Then
                errs.Add("New password must be at least 8 characters.")
            End If
            If errs.Count > 0 Then
                Return New PasswordChangeResult With {.Success = False, .ValidationErrors = errs}
            End If

            Dim storedHash As String = Nothing
            Using conn As New SqliteConnection(_connectionString)
                conn.Open()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT PasswordHash FROM Sys_UserAccounts WHERE Id=@id"
                    cmd.Parameters.AddWithValue("@id", userId)
                    Dim scalar = cmd.ExecuteScalar()
                    If scalar Is Nothing OrElse IsDBNull(scalar) Then
                        Return New PasswordChangeResult With {
                            .Success = False,
                            .ValidationErrors = New List(Of String) From {"User not found."}
                        }
                    End If
                    storedHash = CStr(scalar)
                End Using
            End Using

            If Not PasswordHashHelper.Verify(currentPassword, storedHash) Then
                Return New PasswordChangeResult With {
                    .Success = False,
                    .ValidationErrors = New List(Of String) From {"Current password is incorrect."}
                }
            End If

            If PasswordHashHelper.Verify(newPassword, storedHash) Then
                Return New PasswordChangeResult With {
                    .Success = False,
                    .ValidationErrors = New List(Of String) From {"New password must differ from the current password."}
                }
            End If

            Dim newHash = PasswordHashHelper.Hash(newPassword)
            Using conn As New SqliteConnection(_connectionString)
                conn.Open()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText =
                        "UPDATE Sys_UserAccounts SET PasswordHash=@h, LastPasswordChangeAt=@t, ModifiedAt=@t WHERE Id=@id"
                    cmd.Parameters.AddWithValue("@h", newHash)
                    cmd.Parameters.AddWithValue("@t", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
                    cmd.Parameters.AddWithValue("@id", userId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using

            Return New PasswordChangeResult With {.Success = True, .ValidationErrors = New List(Of String)()}
        End Function

        ' ── Private ADO.NET helpers ────────────────────────────────────────────

        Private Shared Function ReadUserByUsername(conn As SqliteConnection, username As String) As UserAccount
            Using cmd = conn.CreateCommand()
                cmd.CommandText =
                    "SELECT Id, Username, PasswordHash, Role, IsActive, FailedLoginAttempts, " &
                    "LockedUntil, LastPasswordChangeAt, CreatedAt, ModifiedAt " &
                    "FROM Sys_UserAccounts WHERE lower(Username)=lower(@u) LIMIT 1"
                cmd.Parameters.AddWithValue("@u", username)
                Using rdr = cmd.ExecuteReader()
                    If Not rdr.Read() Then Return Nothing
                    Dim u As New UserAccount()
                    u.Id = CInt(rdr("Id"))
                    u.Username = CStr(rdr("Username"))
                    u.PasswordHash = CStr(rdr("PasswordHash"))
                    u.Role = CType(CInt(rdr("Role")), UserRole)
                    u.IsActive = CInt(rdr("IsActive")) = 1
                    u.FailedLoginAttempts = CInt(rdr("FailedLoginAttempts"))
                    u.LockedUntil = If(IsDBNull(rdr("LockedUntil")), Nothing,
                                       CType(DateTime.Parse(CStr(rdr("LockedUntil"))), DateTime?))
                    u.LastPasswordChangeAt = If(IsDBNull(rdr("LastPasswordChangeAt")), Nothing,
                                                CType(DateTime.Parse(CStr(rdr("LastPasswordChangeAt"))), DateTime?))
                    u.CreatedAt = DateTime.Parse(CStr(rdr("CreatedAt")))
                    u.ModifiedAt = If(IsDBNull(rdr("ModifiedAt")), Nothing,
                                      CType(DateTime.Parse(CStr(rdr("ModifiedAt"))), DateTime?))
                    Return u
                End Using
            End Using
        End Function

        Private Shared Sub ClearFailedAttempts(conn As SqliteConnection, userId As Integer)
            Using cmd = conn.CreateCommand()
                cmd.CommandText =
                    "UPDATE Sys_UserAccounts SET FailedLoginAttempts=0, LockedUntil=NULL, ModifiedAt=@now WHERE Id=@id"
                cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
                cmd.Parameters.AddWithValue("@id", userId)
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Shared Sub BumpFailedAttempts(conn As SqliteConnection, userId As Integer, current As Integer)
            Dim next_ = current + 1
            Using cmd = conn.CreateCommand()
                If next_ >= 5 Then
                    cmd.CommandText =
                        "UPDATE Sys_UserAccounts SET FailedLoginAttempts=@a, LockedUntil=@l, ModifiedAt=@now WHERE Id=@id"
                    cmd.Parameters.AddWithValue("@l", DateTime.UtcNow.AddMinutes(15).ToString("yyyy-MM-dd HH:mm:ss"))
                Else
                    cmd.CommandText =
                        "UPDATE Sys_UserAccounts SET FailedLoginAttempts=@a, LockedUntil=NULL, ModifiedAt=@now WHERE Id=@id"
                End If
                cmd.Parameters.AddWithValue("@a", next_)
                cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
                cmd.Parameters.AddWithValue("@id", userId)
                cmd.ExecuteNonQuery()
            End Using
        End Sub

    End Class

End Namespace

' ─────────────────────────────────────────────────────────────────────────────
' Argon2id password hashing helper — Friend so DatabaseInitializer can seed.
' Params: m=19456 KiB, t=2 iterations, p=1, 32-byte hash, 16-byte random salt.
' Format: $argon2id$v=19$m=19456,t=2,p=1$<base64Salt>$<base64Hash>
' NOTE: PasswordBox.SecureString is converted to String before reaching here;
' this is the standard WPF trade-off — Argon2id does not accept SecureString.
' ─────────────────────────────────────────────────────────────────────────────
Friend Module PasswordHashHelper

    Public Function Hash(password As String) As String
        Dim salt(15) As Byte
        RandomNumberGenerator.Fill(salt)
        Dim hashBytes = ComputeHash(Encoding.UTF8.GetBytes(password), salt)
        Return $"$argon2id$v=19$m=19456,t=2,p=1${Convert.ToBase64String(salt)}${Convert.ToBase64String(hashBytes)}"
    End Function

    Public Function Verify(password As String, storedHash As String) As Boolean
        If String.IsNullOrEmpty(storedHash) Then Return False
        Dim parts = storedHash.Split("$"c)
        ' Expected: ["", "argon2id", "v=19", "m=19456,t=2,p=1", "<salt>", "<hash>"]
        If parts.Length < 6 Then Return False

        Dim salt As Byte()
        Dim expected As Byte()
        Dim parseOk = True
        Try
            salt = Convert.FromBase64String(parts(4))
            expected = Convert.FromBase64String(parts(5))
        Catch ex As FormatException
            parseOk = False
            salt = New Byte(0) {}
            expected = New Byte(0) {}
        End Try

        If Not parseOk Then Return False

        Dim computed = ComputeHash(Encoding.UTF8.GetBytes(password), salt)
        Return CryptographicOperations.FixedTimeEquals(computed, expected)
    End Function

    Private Function ComputeHash(passwordBytes As Byte(), salt As Byte()) As Byte()
        Using argon2 As New Argon2id(passwordBytes)
            argon2.Salt = salt
            argon2.MemorySize = 19456
            argon2.Iterations = 2
            argon2.DegreeOfParallelism = 1
            Return argon2.GetBytes(32)
        End Using
    End Function

End Module
