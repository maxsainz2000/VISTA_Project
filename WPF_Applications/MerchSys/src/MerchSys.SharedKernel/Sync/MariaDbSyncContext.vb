Imports System.Collections.Concurrent
Imports System.Data
Imports System.Reflection
Imports System.Threading
Imports MySqlConnector

Namespace Sync

    ''' <summary>
    ''' Lightweight connection wrapper for the central MariaDB 11.4.x instance.
    ''' Replaces the former Pomelo-backed <c>DbContext</c> with raw <see cref="MySqlConnection"/>
    ''' ADO.NET access via <c>MySqlConnector</c>. This avoids the EF Core 10/Pomelo 9 binary
    ''' incompatibility (<c>MissingMethodException</c> on <c>AbstractionsStrings.ArgumentIsEmpty</c>).
    ''' <para>
    ''' SQL generation uses reflection on Remote POCO types (e.g. <c>RemoteVendor</c>,
    ''' <c>RemoteProduct</c>) to build parameterized INSERT/UPDATE statements. Generated SQL
    ''' text is cached per type so reflection runs only once per table per app lifetime.
    ''' </para>
    ''' <para>
    ''' Connection string source: <c>ConnectionStringLoader.GetMariaDbConnectionString</c>
    ''' from the production overlay at <c>%LOCALAPPDATA%\VISTA\appsettings.Production.json</c>.
    ''' </para>
    ''' </summary>
    Public Class MariaDbSyncContext
        Implements IDisposable

        Private ReadOnly _connectionString As String

        ' ── SQL cache (per POCO type) ──────────────────────────────────────────────────
        Private Shared ReadOnly _insertSqlCache As New ConcurrentDictionary(Of String, String)()
        Private Shared ReadOnly _updateSqlCache As New ConcurrentDictionary(Of String, String)()
        Private Shared ReadOnly _propsCache As New ConcurrentDictionary(Of Type, PropertyInfo())()

        Public Sub New(connectionString As String)
            _connectionString = connectionString
        End Sub

        ''' <summary>
        ''' Returns True when a valid connection string was provided at construction time.
        ''' When False, the sync worker should skip transmission (local-only mode).
        ''' </summary>
        Public ReadOnly Property IsConfigured As Boolean
            Get
                Return Not String.IsNullOrWhiteSpace(_connectionString)
            End Get
        End Property

        ''' <summary>
        ''' Creates and opens a new <see cref="MySqlConnection"/>. The caller owns and must
        ''' dispose this connection.
        ''' </summary>
        Public Async Function CreateConnectionAsync(Optional ct As CancellationToken = Nothing) As Task(Of MySqlConnection)
            Dim conn As New MySqlConnection(_connectionString)
            Await conn.OpenAsync(ct)
            Return conn
        End Function

        ' ── Remote row snapshot (conflict resolution) ──────────────────────────────────

        ''' <summary>
        ''' Queries the remote MariaDB for an existing row in <paramref name="tableName"/>
        ''' with the given <paramref name="entityId"/> (integer PK from the sync journal).
        ''' Returns a <see cref="RemoteRowSnapshot"/> used by <see cref="IConflictResolver"/>
        ''' to apply the per-table conflict policy.
        ''' Uses raw ADO.NET to avoid type-specific DbSet casts.
        ''' </summary>
        Public Async Function FetchRemoteRowAsync(tableName As String, entityId As Long) As Task(Of RemoteRowSnapshot)
            If Not IsConfigured Then
                Return New RemoteRowSnapshot With {.Exists = False}
            End If

            Dim conn = Await CreateConnectionAsync()
            Dim wasCreated = True

            Try
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = $"SELECT `ModifiedAt` FROM `{tableName}` WHERE `Id` = @id LIMIT 1"
                    cmd.Parameters.AddWithValue("@id", entityId)

                    Using reader = Await cmd.ExecuteReaderAsync()
                        If Await reader.ReadAsync() Then
                            Dim modAt As DateTime? = Nothing
                            If Not reader.IsDBNull(0) Then modAt = reader.GetDateTime(0)
                            Return New RemoteRowSnapshot With {.Exists = True, .ModifiedAt = modAt}
                        End If
                    End Using
                End Using
                Return New RemoteRowSnapshot With {.Exists = False}
            Finally
                If wasCreated Then conn.Dispose()
            End Try
        End Function

        ' ── CRUD helpers (parameterized SQL) ───────────────────────────────────────────

        ''' <summary>
        ''' Executes a parameterized INSERT into <paramref name="tableName"/> using the
        ''' properties of <paramref name="entity"/> (a Remote POCO).
        ''' </summary>
        Public Async Function ExecuteInsertAsync(tableName As String,
                                                  entity As Object,
                                                  conn As MySqlConnection,
                                                  tx As MySqlTransaction,
                                                  Optional ct As CancellationToken = Nothing) As Task
            Dim entityType = entity.GetType()
            Dim props = GetCachedProperties(entityType)
            Dim sql = _insertSqlCache.GetOrAdd(tableName, Function(tbl) BuildInsertSql(tbl, props))

            Using cmd As New MySqlCommand(sql, conn, tx)
                AddParameters(cmd, props, entity)
                Await cmd.ExecuteNonQueryAsync(ct)
            End Using
        End Function

        ''' <summary>
        ''' Executes a parameterized UPDATE on <paramref name="tableName"/> for the row
        ''' matching the <c>Id</c> property of <paramref name="entity"/>.
        ''' </summary>
        Public Async Function ExecuteUpdateAsync(tableName As String,
                                                  entity As Object,
                                                  conn As MySqlConnection,
                                                  tx As MySqlTransaction,
                                                  Optional ct As CancellationToken = Nothing) As Task
            Dim entityType = entity.GetType()
            Dim props = GetCachedProperties(entityType)
            Dim sql = _updateSqlCache.GetOrAdd(tableName, Function(tbl) BuildUpdateSql(tbl, props))

            Using cmd As New MySqlCommand(sql, conn, tx)
                AddParameters(cmd, props, entity)
                Await cmd.ExecuteNonQueryAsync(ct)
            End Using
        End Function

        ''' <summary>
        ''' Executes a parameterized DELETE from <paramref name="tableName"/> where <c>Id</c>
        ''' matches <paramref name="entityId"/>.
        ''' </summary>
        Public Async Function ExecuteDeleteAsync(tableName As String,
                                                  entityId As Long,
                                                  conn As MySqlConnection,
                                                  tx As MySqlTransaction,
                                                  Optional ct As CancellationToken = Nothing) As Task
            Dim sql = $"DELETE FROM `{tableName}` WHERE `Id` = @Id"
            Using cmd As New MySqlCommand(sql, conn, tx)
                cmd.Parameters.AddWithValue("@Id", entityId)
                Await cmd.ExecuteNonQueryAsync(ct)
            End Using
        End Function

        ' ── SQL builder (reflection, cached) ───────────────────────────────────────────

        Private Shared Function GetCachedProperties(entityType As Type) As PropertyInfo()
            Return _propsCache.GetOrAdd(entityType,
                Function(t) t.GetProperties(BindingFlags.Public Or BindingFlags.Instance).
                              Where(Function(p) p.CanRead).
                              ToArray())
        End Function

        ''' <summary>
        ''' Builds: INSERT INTO `table` (`Col1`, `Col2`, ...) VALUES (@Col1, @Col2, ...)
        ''' </summary>
        Private Shared Function BuildInsertSql(tableName As String, props As PropertyInfo()) As String
            Dim cols = String.Join(", ", props.Select(Function(p) $"`{p.Name}`"))
            Dim parms = String.Join(", ", props.Select(Function(p) $"@{p.Name}"))
            Return $"INSERT INTO `{tableName}` ({cols}) VALUES ({parms})"
        End Function

        ''' <summary>
        ''' Builds: UPDATE `table` SET `Col1` = @Col1, `Col2` = @Col2, ... WHERE `Id` = @Id
        ''' </summary>
        Private Shared Function BuildUpdateSql(tableName As String, props As PropertyInfo()) As String
            Dim setClauses = String.Join(", ",
                props.Where(Function(p) p.Name <> "Id").
                      Select(Function(p) $"`{p.Name}` = @{p.Name}"))
            Return $"UPDATE `{tableName}` SET {setClauses} WHERE `Id` = @Id"
        End Function

        ''' <summary>
        ''' Adds a <see cref="MySqlParameter"/> for every property on the entity. Nullable
        ''' properties with a Nothing value are sent as <see cref="DBNull.Value"/>.
        ''' </summary>
        Private Shared Sub AddParameters(cmd As MySqlCommand, props As PropertyInfo(), entity As Object)
            For Each prop In props
                Dim value = prop.GetValue(entity)
                If value Is Nothing Then
                    cmd.Parameters.AddWithValue($"@{prop.Name}", DBNull.Value)
                Else
                    cmd.Parameters.AddWithValue($"@{prop.Name}", value)
                End If
            Next
        End Sub

        ' ── IDisposable ────────────────────────────────────────────────────────────────

        Public Sub Dispose() Implements IDisposable.Dispose
            ' No persistent connection held; connections are created per-operation.
        End Sub

    End Class

End Namespace
