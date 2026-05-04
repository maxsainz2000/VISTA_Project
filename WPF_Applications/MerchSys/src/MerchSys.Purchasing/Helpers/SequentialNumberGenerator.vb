Namespace Helpers

    ''' <summary>
    ''' Generates sequential identifiers in PREFIX-YYYY-XXXX format.
    ''' Used for PO numbers (PO-YYYY-XXXX) and reusable for GR numbers (GR-YYYY-XXXX).
    ''' </summary>
    Public Class SequentialNumberGenerator

        ''' <summary>
        ''' Returns the next sequential number for the given prefix and year, derived from
        ''' the current maximum sequence found in <paramref name="existingNumbers"/>.
        ''' The DB unique index is the authoritative collision guard.
        ''' </summary>
        Public Shared Function Generate(prefix As String, year As Integer, existingNumbers As IEnumerable(Of String)) As String
            Dim pattern As String = $"{prefix}-{year}-"
            Dim maxSeq As Integer = existingNumbers.
                Where(Function(n) n IsNot Nothing AndAlso n.StartsWith(pattern, StringComparison.Ordinal)).
                Select(Function(n)
                           Dim seq As Integer = 0
                           Integer.TryParse(n.Substring(pattern.Length), seq)
                           Return seq
                       End Function).
                DefaultIfEmpty(0).
                Max()
            Return $"{prefix}-{year}-{(maxSeq + 1).ToString("D4")}"
        End Function

    End Class

End Namespace
