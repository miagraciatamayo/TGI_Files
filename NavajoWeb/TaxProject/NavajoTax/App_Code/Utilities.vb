Imports Microsoft.VisualBasic
Imports System.Data
Imports System.Data.OleDb

Public Class Utilities

    Public _fileType As String = String.Empty

    ''' <summary>
    ''' Gets connection string for NCIS_TREASURY database.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property ConnectString As String
        Get
            Return ConfigurationManager.ConnectionStrings("ConnString").ConnectionString
        End Get
    End Property

    Public ReadOnly Property BankConnectString As String
        Get
            Return ConfigurationManager.ConnectionStrings("BankConnString").ConnectionString
        End Get
    End Property

    Public Function checkNull(ByVal myObject As Object) As Boolean
        If (Convert.IsDBNull(myObject)) Then
            Return False
        Else
            Return True
        End If
    End Function


    Public ReadOnly Property CurrentUserName As String
        Get
            Dim UserName As String
            Dim SlashPos As Integer

            SlashPos = InStr(Environment.UserName, "\")

            If SlashPos > 0 Then
                UserName = Mid(Environment.UserName, SlashPos + 1)
            Else
                UserName = Environment.UserName
            End If

            Return UserName
        End Get
    End Property


    Public Function GetDatabaseUserName(ByVal connectionString As String) As String
        Dim builder As New OleDb.OleDbConnectionStringBuilder(connectionString)
        Return CStr(builder("User ID"))
    End Function

    Public Function GetDatabasePassword(ByVal connectionString As String) As String
        Dim builder As New OleDb.OleDbConnectionStringBuilder(connectionString)
        Return CStr(builder("Password"))
    End Function



    ''' <summary>
    ''' Gets formatted yes/no value. 1 is "yes", all others "no".
    ''' </summary>
    ''' <param name="value"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function GetYesNo(value As Object) As String
        If IsNumeric(value) AndAlso CInt(value) = 1 Then
            Return "Yes"
        Else
            Return "No"
        End If
    End Function

    Public Shared Function GetDecimalOrZero(value As String) As Decimal
        If String.IsNullOrEmpty(value) OrElse Not IsNumeric(value) Then
            Return 0
        Else
            Return CDec(value)
        End If
    End Function


    Public Shared Function GetDecimalOrDBNull(value As String) As Object
        If String.IsNullOrEmpty(value) OrElse Not IsNumeric(value) Then
            Return DBNull.Value
        Else
            Return CDec(value)
        End If
    End Function

    Public ReadOnly Property GetUploadFileType(ByVal fileName As String) As String
        Get
            '' Get file type based on extension of filename passed in.

            If Not String.IsNullOrEmpty(fileName) Then
                Dim fileNameLength As Integer = fileName.Length
                Dim extensionPostion As Integer = fileName.LastIndexOf(".")
                Dim extensionLength As Integer = fileNameLength - extensionPostion
                Dim fileExtension As String = fileName.ToLower().Substring(extensionPostion, extensionLength)
                Dim fileType As String = String.Empty

                Select Case fileExtension
                    Case ".jpg", ".jpeg"
                        fileType = "jpg"
                    Case ".xls"
                        fileType = "xls"
                    Case ".xlsx"
                        fileType = "xlsx"
                    Case ".pdf"
                        fileType = "pdf"
                    Case ".doc"
                        fileType = "doc"
                    Case ".docx"
                        fileType = "docx"
                    Case ".tif"
                        fileType = "tif"
                    Case ".dwg"
                        fileType = "dwg"
                End Select

                _fileType = fileType
            End If

            Return _fileType
        End Get
    End Property

    ''' <summary>
    ''' Loads table from database into dataset.
    ''' </summary>
    ''' <param name="container"></param>
    ''' <param name="tableName"></param>
    ''' <param name="query"></param>
    ''' <remarks></remarks>
    Public Sub LoadTable(container As DataSet, tableName As String, query As String)
        Using adt As New OleDbDataAdapter(query, Me.ConnectString)
            adt.Fill(container, tableName)
        End Using
    End Sub

    ''' <summary>
    ''' Gets new value for column from datatable.
    ''' </summary>
    ''' <param name="columnName"></param>
    ''' <param name="table"></param>
    ''' <param name="rowFilter"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Overloads Function GetNewID(columnName As String, table As DataTable, Optional rowFilter As String = Nothing) As Integer
        Dim newID As Object = table.Compute(String.Format("MAX({0})", columnName), rowFilter)

        If IsNumeric(newID) Then
            Return CInt(newID) + 1
        Else
            Return 1
        End If
    End Function



    ''' <summary>
    ''' Gets new value for column from datatable.
    ''' </summary>
    ''' <param name="columnName"></param>
    ''' <param name="tableName"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Overloads Function GetNewID(columnName As String, tableName As String) As Integer
        Dim SQL As String = String.Empty
        Dim newID As String = String.Empty

        SQL = String.Format("SELECT MAX({0}) FROM genii_user.{1} ", columnName, tableName)

        Using conn As New OleDbConnection(Me.ConnectString)
            Dim cmd As New OleDbCommand()

            cmd.CommandText = SQL

            cmd.Connection = conn

            conn.Open()

            newID = IIf(IsDBNull(cmd.ExecuteScalar()), "0", cmd.ExecuteScalar())

        End Using

        If Not String.IsNullOrEmpty(newID) Then
            Return Convert.ToInt32(newID) + 1
        Else
            Return 0
        End If

    End Function
End Class
