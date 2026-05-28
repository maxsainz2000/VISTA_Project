Imports System.ComponentModel.DataAnnotations
Imports Microsoft.EntityFrameworkCore

Namespace Spike

    ''' <summary>
    ''' Disposable spike context for INFRA-23. Do not reference from production.
    ''' </summary>
    Public Class SpikeProduct
        Public Property Id As Integer
        Public Property Name As String
        Public Property Batches As ICollection(Of SpikeBatch) = New List(Of SpikeBatch)()
    End Class

    Public Class SpikeBatch
        Public Property Id As Integer
        Public Property ProductId As Integer
        Public Property QuantityRemaining As Integer
        Public Property Product As SpikeProduct
        Public Property RowVersion As DateTime ' Mapped to TIMESTAMP(6) ON UPDATE for concurrency
    End Class

    Public Class SpikeContext
        Inherits DbContext

        Public Property Products As DbSet(Of SpikeProduct)
        Public Property Batches As DbSet(Of SpikeBatch)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            Dim connStr = "Server=localhost;Port=3306;Database=merchsys_central;User Id=root;Password=;"
            optionsBuilder.UseMySQL(connStr)
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of SpikeProduct)().
                ToTable("Spike_Products")

            modelBuilder.Entity(Of SpikeProduct)().
                HasKey(Function(p) p.Id)

            modelBuilder.Entity(Of SpikeBatch)().
                ToTable("Spike_Batches")

            modelBuilder.Entity(Of SpikeBatch)().
                HasKey(Function(b) b.Id)

            modelBuilder.Entity(Of SpikeBatch)().
                HasOne(Function(b) b.Product).
                WithMany(Function(p) p.Batches).
                HasForeignKey(Function(b) b.ProductId)

            ' Optimistic concurrency row version mapped to TIMESTAMP(6)
            modelBuilder.Entity(Of SpikeBatch)().
                Property(Function(b) b.RowVersion).
                IsRowVersion().
                HasColumnType("TIMESTAMP(6)").
                ValueGeneratedOnAddOrUpdate()
        End Sub
    End Class

End Namespace
