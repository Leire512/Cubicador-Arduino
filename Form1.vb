Imports System.IO.Ports
Imports System.Threading
Imports System.Data.SQLite
Imports System.Runtime.InteropServices

Public Class Form1

#Region "FUNCIONALIDADES DEL FORMULARIO"
    'RESIZE DEL FORMULARIO- CAMBIAR TAMAÑO
    Dim cGrip As Integer = 10

    Protected Overrides Sub WndProc(ByRef m As Message)
        If (m.Msg = 132) Then
            Dim pos As Point = New Point((m.LParam.ToInt32 And 65535), (m.LParam.ToInt32 + 16))
            pos = Me.PointToClient(pos)
            If ((pos.X _
                        >= (Me.ClientSize.Width - cGrip)) _
                        AndAlso (pos.Y _
                        >= (Me.ClientSize.Height - cGrip))) Then
                m.Result = CType(17, IntPtr)
                Return
            End If
        End If
        MyBase.WndProc(m)
    End Sub
    '----------------DIBUJAR RECTANGULO / EXCLUIR ESQUINA PANEL 
    Dim sizeGripRectangle As Rectangle
    Dim tolerance As Integer = 15

    Protected Overrides Sub OnSizeChanged(ByVal e As EventArgs)
        MyBase.OnSizeChanged(e)
        Dim region = New Region(New Rectangle(0, 0, Me.ClientRectangle.Width, Me.ClientRectangle.Height))
        sizeGripRectangle = New Rectangle((Me.ClientRectangle.Width - tolerance), (Me.ClientRectangle.Height - tolerance), tolerance, tolerance)
        region.Exclude(sizeGripRectangle)
        Me.panelContenedor.Region = region
        Me.Invalidate()
    End Sub

    '----------------COLOR Y GRIP DE RECTANGULO INFERIOR
    Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
        Dim blueBrush As SolidBrush = New SolidBrush(Color.FromArgb(244, 244, 244))
        e.Graphics.FillRectangle(blueBrush, sizeGripRectangle)
        MyBase.OnPaint(e)
        ControlPaint.DrawSizeGrip(e.Graphics, Color.Transparent, sizeGripRectangle)
    End Sub

    <DllImport("user32.DLL", EntryPoint:="ReleaseCapture")>
    Private Shared Sub ReleaseCapture()
    End Sub

    <DllImport("user32.DLL", EntryPoint:="SendMessage")>
    Private Shared Sub SendMessage(ByVal hWnd As System.IntPtr, ByVal wMsg As Integer, ByVal wParam As Integer, ByVal lParam As Integer)
    End Sub

    Private Sub PanelBarraTitulo_MouseMove(sender As Object, e As MouseEventArgs) Handles PanelBarraTitulo.MouseMove
        ReleaseCapture()
        SendMessage(Me.Handle, &H112&, &HF012&, 0)
    End Sub

    Private Sub BtnCerrar_Click(sender As Object, e As EventArgs) Handles btnCerrar.Click
        Application.Exit()
    End Sub

    'capturar valores originales del formulario antes de maximizar
    Dim lx, ly As Integer
    Dim sw, sh As Integer

    Private Sub BtnMinimizar_Click(sender As Object, e As EventArgs) Handles btnMinimizar.Click
        Me.WindowState = FormWindowState.Minimized
    End Sub
    Private Sub BtnMaximizar_Click(sender As Object, e As EventArgs) Handles btnMaximizar.Click

        lx = Me.Location.X
        ly = Me.Location.Y
        sw = Me.Size.Width
        sh = Me.Size.Height

        btnMaximizar.Visible = False
        btnRestaurar.Visible = True

        Me.Size = Screen.PrimaryScreen.WorkingArea.Size
        Me.Location = Screen.PrimaryScreen.WorkingArea.Location

    End Sub


    Private Sub BtnRestaurar_Click(sender As Object, e As EventArgs) Handles btnRestaurar.Click
        Me.Size = New Size(sw, sh)
        Me.Location = New Point(lx, ly)

        btnMaximizar.Visible = True
        btnRestaurar.Visible = False

    End Sub

#End Region

    Dim dato As String 'variable que lee el dato del puerto serial  
    Dim rutadb As String
    Dim con As New SQLiteConnection

    Public Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        rutadb = System.AppDomain.CurrentDomain.BaseDirectory

        con = New SQLiteConnection("Data Source=" & rutadb & "\Cubicador.db; Version=3")

        buscarpuerto()
        datagridview()

        ''''''''''''''''''''''
        txt_material.Select()
        SerialPort1.Close()
        con.Close()

    End Sub

    Private Sub buscarpuerto()
        Try
            ComboBox1.Items.Clear()
            For Each puerto As String In My.Computer.Ports.SerialPortNames
                ComboBox1.Items.Add(puerto)
            Next
            If ComboBox1.Items.Count > 0 Then

                ComboBox1.SelectedIndex = 0
            Else
                MsgBox("No hay puertos disponibles")
            End If
        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Critical)
        End Try
    End Sub

    Private Sub SerialPort1_DataReceived(sender As Object, e As SerialDataReceivedEventArgs) Handles SerialPort1.DataReceived

        dato = SerialPort1.ReadLine() ' la variable dato recive la cadena de caracteres del arudino

        'valores originales
        'ancho y largo miden 36 app

        Dim medidas As String() = Split(dato, ",")

        'El orden de los sensores es ancho, largo y alto.

        Dim largo As Double
        Dim ancho As Double
        Dim alto As Double

        largo = CDbl(Val(medidas(0)))
        ancho = CDbl(Val(medidas(1)))
        alto = CDbl(Val(medidas(2)))

        ' MsgBox(alto)

        CheckForIllegalCrossThreadCalls = False

        lbl_largo.Text = 36 - largo
        lbl_ancho.Text = 36 - ancho
        lbl_alto.Text = 32 - alto


    End Sub

    Public Sub Btn_desconectar_Click(sender As Object, e As EventArgs) Handles btn_desconectar.Click

        SerialPort1.Close()
        lbl_status.Text = "Desconectado"
        lbl_status.ForeColor = Color.DarkRed
        con.Close()

    End Sub

    Public Sub Btn_conectar_Click(sender As Object, e As EventArgs) Handles btn_conectar.Click

        Try

            With SerialPort1
                .BaudRate = 9600
                .DataBits = 8
                .Parity = IO.Ports.Parity.None
                .StopBits = 1
                .PortName = ComboBox1.Text
                .Open()

                If .IsOpen Then
                    lbl_status.Text = "Conectado"
                    lbl_status.ForeColor = Color.DarkGreen

                    txt_material.Select()
                Else
                    MsgBox("Conexion Fallida", MsgBoxStyle.Critical)
                End If
            End With
        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Critical)
        End Try


    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles btn_insertar.Click

        If txt_material.Text = "" Or txt_peso.Text = "" Then

            MsgBox("Debe ingresar el Material y el Peso")

        Else

            Try
                Dim query As String
                con.Open()
                query = "INSERT INTO Medidas (SKU, Peso, Largo, Ancho, Alto) VALUES ('" & txt_material.Text & "'," & txt_peso.Text & ",'" & lbl_largo.Text & "','" & lbl_ancho.Text & "','" & lbl_alto.Text & "')"
                Dim cmd As New SQLiteCommand(query, con)
                cmd.ExecuteNonQuery()

                MsgBox("Datos Ingresados", MsgBoxStyle.OkOnly)
                con.Close()
                datagridview()

                'limpio celdas
                txt_material.Text = ""
                txt_peso.Text = ""

                lbl_largo.Text = "Esperando datos..."
                lbl_ancho.Text = "Esperando datos..."
                lbl_alto.Text = "Esperando datos..."

                txt_material.Select()

            Catch ex As Exception
                MsgBox("Se produjo el error: " & ex.Message, MsgBoxStyle.Exclamation)
                con.Close()
            End Try

        End If

    End Sub

    Private Sub Label3_Click(sender As Object, e As EventArgs) Handles Label3.Click

    End Sub

    Private Sub txt_material_LostFocus(sender As Object, e As EventArgs) Handles txt_material.LostFocus

        If txt_material.Text <> "" And Microsoft.VisualBasic.Left(txt_material.Text, 4) = "QR01" Then

            Dim qr() As String = Split(txt_material.Text, "03")

            Dim caracter As Integer = InStr(1, qr(1), "·")

            If caracter > 1 Then
                Dim sku() As String = Split(qr(1), "·")
                txt_material.Text = sku(0)
            Else
                Dim sku() As String = Split(qr(1), "#")
                txt_material.Text = sku(0)
            End If

        End If
    End Sub

    Private Sub datagridview()
        Try
            con.Open()
            Dim sql = "SELECT * FROM Medidas ORDER BY ID DESC"
            Dim cmdDGV As SQLiteCommand = New SQLiteCommand(sql, con)

            Dim da As New SQLiteDataAdapter
            da.SelectCommand = cmdDGV

            Dim dt As New DataTable
            da.Fill(dt)

            DataGridView1.DataSource = dt

            Dim readerDG As SQLiteDataReader = cmdDGV.ExecuteReader()
            con.Close()

        Catch ex As Exception

            MsgBox(ex.ToString())

        End Try
    End Sub
#Region "ARRASTRAR EL FORMULARIO"


#End Region
End Class
