Imports CommunityToolkit.Mvvm.ComponentModel
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Services

Namespace ViewModels

    ''' <summary>
    ''' Form state for the vendor create/edit panel.
    ''' Validation runs client-side; duplicate-name errors bubble up from the service.
    ''' </summary>
    Public Class VendorEditorViewModel
        Inherits ObservableObject

        Public Property IsNewVendor As Boolean = True
        Public Property EditingVendorId As Integer?

        Public ReadOnly Property EditorTitle As String
            Get
                Return If(IsNewVendor, "New Vendor", "Edit Vendor")
            End Get
        End Property

        Private _name As String = String.Empty
        Public Property Name As String
            Get
                Return _name
            End Get
            Set(value As String)
                SetProperty(_name, value)
            End Set
        End Property

        Private _contactPerson As String = String.Empty
        Public Property ContactPerson As String
            Get
                Return _contactPerson
            End Get
            Set(value As String)
                SetProperty(_contactPerson, value)
            End Set
        End Property

        Private _phone As String = String.Empty
        Public Property Phone As String
            Get
                Return _phone
            End Get
            Set(value As String)
                SetProperty(_phone, value)
            End Set
        End Property

        Private _email As String = String.Empty
        Public Property Email As String
            Get
                Return _email
            End Get
            Set(value As String)
                SetProperty(_email, value)
            End Set
        End Property

        Private _address As String = String.Empty
        Public Property Address As String
            Get
                Return _address
            End Get
            Set(value As String)
                SetProperty(_address, value)
            End Set
        End Property

        Private _defaultLeadTimeDays As Integer = 1
        Public Property DefaultLeadTimeDays As Integer
            Get
                Return _defaultLeadTimeDays
            End Get
            Set(value As Integer)
                SetProperty(_defaultLeadTimeDays, value)
            End Set
        End Property

        Private _notes As String = String.Empty
        Public Property Notes As String
            Get
                Return _notes
            End Get
            Set(value As String)
                SetProperty(_notes, value)
            End Set
        End Property

        Private _validationError As String = String.Empty
        Public Property ValidationError As String
            Get
                Return _validationError
            End Get
            Set(value As String)
                SetProperty(_validationError, value)
            End Set
        End Property

        Public Sub PrepareForNew()
            IsNewVendor = True
            EditingVendorId = Nothing
            Name = String.Empty
            ContactPerson = String.Empty
            Phone = String.Empty
            Email = String.Empty
            Address = String.Empty
            DefaultLeadTimeDays = 1
            Notes = String.Empty
            ValidationError = String.Empty
            OnPropertyChanged(NameOf(EditorTitle))
        End Sub

        Public Sub LoadFromVendor(vendor As Vendor)
            IsNewVendor = False
            EditingVendorId = vendor.Id
            Name = If(vendor.Name, String.Empty)
            ContactPerson = If(vendor.ContactPerson, String.Empty)
            Phone = If(vendor.Phone, String.Empty)
            Email = If(vendor.Email, String.Empty)
            Address = If(vendor.Address, String.Empty)
            DefaultLeadTimeDays = vendor.DefaultLeadTimeDays
            Notes = If(vendor.Notes, String.Empty)
            ValidationError = String.Empty
            OnPropertyChanged(NameOf(EditorTitle))
        End Sub

        Public Function Validate() As Boolean
            If String.IsNullOrWhiteSpace(Name) Then
                ValidationError = "Vendor name is required."
                Return False
            End If
            If String.IsNullOrWhiteSpace(Phone) Then
                ValidationError = "Phone number is required."
                Return False
            End If
            If DefaultLeadTimeDays <= 0 Then
                ValidationError = "Lead time must be greater than 0 days."
                Return False
            End If
            ValidationError = String.Empty
            Return True
        End Function

        Public Function ToCreateDto() As CreateVendorDto
            Return New CreateVendorDto With {
                .Name = Name.Trim(),
                .ContactPerson = ContactPerson.Trim(),
                .Phone = Phone.Trim(),
                .Email = Email.Trim(),
                .Address = Address.Trim(),
                .DefaultLeadTimeDays = DefaultLeadTimeDays,
                .Notes = Notes.Trim()
            }
        End Function

        Public Function ToUpdateDto() As UpdateVendorDto
            Return New UpdateVendorDto With {
                .Name = Name.Trim(),
                .ContactPerson = ContactPerson.Trim(),
                .Phone = Phone.Trim(),
                .Email = Email.Trim(),
                .Address = Address.Trim(),
                .DefaultLeadTimeDays = DefaultLeadTimeDays,
                .Notes = Notes.Trim()
            }
        End Function

    End Class

End Namespace
