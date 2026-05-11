Namespace Persistence

    ''' <summary>
    ''' Marks an entity class as excluded from <c>Sync_Journal</c> appending.
    ''' <para>
    ''' Built-in table exclusions (applied by name regardless of this attribute):
    ''' <list type="bullet">
    '''   <item><description><c>Sync_Journal</c> — journalling it would cause infinite recursion.</description></item>
    '''   <item><description><c>__EFMigrationsHistory</c> — internal EF tracking table, not a domain entity.</description></item>
    ''' </list>
    ''' Entity-level exclusions (applied via this attribute):
    ''' <list type="bullet">
    '''   <item><description><c>Pos_ReceiptIntegrity</c> — immutable integrity chain; sync semantics not yet defined.</description></item>
    '''   <item><description><c>Acc_TamperAuditLog</c> — tamper evidence; must remain local-only until a retention plan is approved.</description></item>
    ''' </list>
    ''' </para>
    ''' </summary>
    <AttributeUsage(AttributeTargets.Class, AllowMultiple:=False, Inherited:=False)>
    Public NotInheritable Class NoSyncAttribute
        Inherits Attribute
    End Class

End Namespace
