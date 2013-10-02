<%@ WebHandler Language="VB" Class="GetBlobFromDB" %>


Imports System
Imports System.Configuration
Imports System.IO
Imports System.Text
Imports System.Web
Imports System.Data
Imports System.Data.OleDb
Imports Genesis

Public Class GetBlobFromDB : Implements IHttpHandler
    
    Private _connectString As String = String.Empty
    Private _context As HttpContext

#Region "Properties"
    Public Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
        _context = context
        Render()
    End Sub
 
    Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
    
    Private ReadOnly Property ConnectString() As String
        Get
            Return ConfigurationManager.ConnectionStrings("ConnString").ConnectionString
        End Get
    End Property

    Public ReadOnly Property tableName() As String
        Get
            Return _context.Request.QueryString("tabname")
        End Get
    End Property

    Public ReadOnly Property columnName() As String
        Get
            Return _context.Request.QueryString("colname")
        End Get
    End Property

    Public ReadOnly Property primaryKeyNames() As String
        Get
            Return _context.Request.QueryString("pkNames")
        End Get
    End Property

    Public ReadOnly Property primaryKeyValues() As String
        Get
            Return _context.Request.QueryString("pkValues")
        End Get
    End Property

    Public ReadOnly Property imageHeight() As String
        Get
            Return _context.Request.QueryString("height")
        End Get
    End Property

    Public ReadOnly Property ImageWidth() As String
        Get
            Return _context.Request.QueryString("width")
        End Get
    End Property

    Public ReadOnly Property fileType() As String
        Get
            Return _context.Request.QueryString("filetype")
        End Get
    End Property

    Private Function RowFilter(colNames As String, colValues As String) As String
        Dim filter As New StringBuilder()

        Dim colNameArray As String() = colNames.Split(","c)
        Dim colValueArray As String() = colValues.Split(","c)

        For count As Integer = 0 To colNameArray.Length - 1
            filter.AppendFormat("({0} = {1}) AND ", colNameArray(count), colValueArray(count))
        Next

        If filter.Length > 0 Then
            filter = filter.Remove(filter.Length - 4, 4)
        End If

        Return filter.ToString()
    End Function

    ''' <summary>
    ''' GetRow - Returns the BLOB data from the Database
    ''' </summary>
    ''' <returns></returns>
    Private Function GetRow() As DataRow
        Try
            Dim sql As String = [String].Format("SELECT {0} FROM {1} WHERE {2}", Me.columnName, Me.tableName, RowFilter(Me.primaryKeyNames, Me.primaryKeyValues))

            Dim dt As New DataTable()

            Using adt As New OleDbDataAdapter(sql, Me.ConnectString)
                adt.Fill(dt)
            End Using

            If dt.Rows.Count > 0 Then
                Return dt.Rows(0)
            Else
                Return Nothing

            End If
        Catch ex As Exception
            Throw New Exception(ex.Message)
        End Try
    End Function


#End Region


    Private Sub Render()
        Dim row As DataRow = GetRow()
        Dim fileName As String = String.Empty
        Dim contentType As String = String.Empty
        Dim rawData As Byte() = Nothing

        Try
            If String.IsNullOrEmpty(fileType) Then
                _context.Response.Write("Document could not be loaded. Unable to determine file type.")
            Else
                If Not row.IsNull(columnName) Then
                    rawData = DirectCast(row(columnName), Byte())

                Else
                    '' Alert user their document could not be loaded
                End If

                If (rawData IsNot Nothing) And (rawData.Length > 0) Then
                    ' Get the file type from the QueryString
                    If Not String.IsNullOrEmpty(fileType) Then
                        Select Case fileType.ToLower()
                            Case "jpg"
                                contentType = "image/jpeg"
                                fileName = "download.jpg"
                                Exit Select
                            Case "xls"
                                contentType = "application/vnd.ms-excel"
                                fileName = "download.xls"
                                Exit Select
                            Case "xlsx"
                                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                                fileName = "download.xlsx"
                                Exit Select
                            Case "doc"
                                contentType = "application/msword"
                                fileName = "download.doc"
                                Exit Select
                            Case "docx"
                                contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                fileName = "download.docx"
                                Exit Select
                            Case "pdf"
                                contentType = "application/pdf"
                                fileName = "download.pdf"
                                Exit Select
                            Case "dwg"
                                contentType = "image/vnd.dwg"
                                fileName = "download.dwg"
                            Case "tif"
                                contentType = "image/tif"
                                fileName = "download.tif"
                        End Select
                    End If

                    ' Send blob data to response.
                    Genesis.Common.BlobField.Send(_context.Response, contentType, fileName, rawData)
                Else
                    'throw new Exception("Document could not be loaded.");
                    _context.Response.Write("Document could not be loaded.")
                End If
            End If
            'Happens when response is flushed. Do nothing.
        Catch ex As System.Threading.ThreadAbortException
        Catch ex As Exception

            Throw New Exception(ex.Message)
        End Try
    End Sub
End Class

