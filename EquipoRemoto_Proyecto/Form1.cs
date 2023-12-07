using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace EquipoRemoto_Proyecto
{
    public partial class Form1 : Form
    {
        private TcpListener servidor;
        private Int32 puerto = 8888;
        private Thread acceptThread;
        private NetworkStream stream;
        private TextBox logTextBox; // TextBox for logging

        public Form1()
        {
            InitializeComponent();
            logTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true //Para no poder editar el TextBox
            };
            Controls.Add(logTextBox);
            Load += Form1_Load; // Bind the Load event
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            StartServer();
        }

        private void StartServer()
        {
            //servidor = new TcpListener(IPAddress.Loopback, 8888);
            servidor = new TcpListener(IPAddress.Any, puerto);
            servidor.Start();
            logTextBox.AppendText("Esperando Comandos...\r\n");

            acceptThread = new Thread(AcceptClients);
            acceptThread.Start();
        }

        private void AcceptClients()
        {
            try
            {
                while (true)
                {
                    TcpClient acceptedClient = servidor.AcceptTcpClient();
                    NetworkStream stream = acceptedClient.GetStream();
                    Thread clientThread = new Thread(() => HandleClient(acceptedClient));
                    clientThread.Start();
                    lbl_conexion.Invoke((MethodInvoker)delegate
                    {
                        lbl_conexion.Text = "Cliente conectado";
                    });
                }
            }
            catch (Exception ex)
            {
                UpdateLog("Servidor detenido: " + ex.Message);
            }
        }


        private void HandleClient(TcpClient acceptedClient)
        {
            NetworkStream clientStream = null;

            try
            {
                clientStream = acceptedClient.GetStream();
                using (StreamReader reader = new StreamReader(clientStream))
                using (StreamWriter writer = new StreamWriter(clientStream))
                {
                    string command;
                    while ((command = reader.ReadLine()) != null)
                    {
                        UpdateLog("Comando: " + command);
                        string response = ExecuteCommand(command);
                        writer.WriteLine(response);
                        writer.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateLog("Error durante la comunicación con el cliente: " + ex.Message);
            }
            finally
            {
                // Cerrar el stream antes de cerrar el cliente
                clientStream?.Close();
                acceptedClient?.Close();

                // Actualizar la interfaz de usuario desde el hilo de la interfaz de usuario

            }
        }


        private string ExecuteCommand(string command)
        {
            if (command == "GET_OS_NAME")
            {
                return "Nombre del Sistema Operativo: " + Environment.OSVersion.VersionString;
            }
            else if (command == "GET_OS_PLATFORM")
            {
                return "Plataforma del Sistema Operativo: " + Environment.OSVersion.Platform;
            }
            else if (command == "GET_OS_VERSION")
            {
                return "Version del Sistema Operativo: " + Environment.OSVersion.VersionString;
            }
            else if (command == "GET_COMPUTER_NAME")
            {
                return "Nombre del equipo: " + Environment.MachineName;
            }
            else if (command == "GET_PROCESSOR_INFO")
            {
                return "Informacion del procesador: " + GetProcessorInfo();
            }
            else if (command == "GET_TOTAL_RAM")
            {
                return "Total RAM (En GB): " + GetTotalRAM();
            }
            else if (command == "GET_LIST_DD")
            {
                return "Lista de unidades del disco duro" + ShowDriveInfo();
            }
            else if (command == "GET_RESOLUTION")
            {
                return "Resolucion pantalla: " + GetResolution();
            }
            else if (command == "GET_USER_NAME")
            {
                return "Nombre del usuario que inicio sesion: " + Environment.UserName;
            }
            else if (command == "GET_TIME_ZONE")
            {
                return "Zona horaria del sistema: " + GetZonaHoraria();
            }
            else if (command == "GET_TIME_DATE")
            {
                return "Fecha y hora del sistema: " + ObtenerFechaHora();
            }
            else if (command == "GET_TOTAL_PROCESS")
            {
                return "Procesos en ejecucion: " + ObtenerListaProcesos();
            }
            else if (command == "TAKE_SCREENSHOT")
            {
                return TakeScreenshot();
            }
            else if (command.StartsWith("SEND_MESSAGE"))
            {
                string[] mensajeSplit = command.Split(' ');
                if (mensajeSplit.Length >= 2)
                {
                    string mensaje = string.Join(" ", mensajeSplit.Skip(1));
                    MostrarMensaje(mensaje);
                    return "Mensaje enviado correctamente";
                }
                return "Error al enviar el mensaje";
            }

            else if (command == "INCREASE_VOLUMEN")
            {
                return SubirVolumen();
            }
            else if (command == "DECREASE_VOLUMEN")
            {
                return BajarVolumen();
            }
            else if (command == "MUTE")
            {
                return Silenciar();
            }
            else if (command == "TURN_OFF")
            {
                return Apagar();
            }
            else if (command == "RESET")
            {
                return Reiniciar();
            }
            else if (command == "CLOSE_SESION")
            {
                return CerrarSesion();
            }
            else if (command.StartsWith("KILL_PROCESS"))
            {
                string[] mensajeSplit = command.Split(' ');
                if (mensajeSplit.Length >= 2)
                {
                    if (int.TryParse(mensajeSplit[1], out int IDprocess))
                    {
                        CerrarProceso(IDprocess);
                        return $"Proceso con ID {IDprocess} finalizado";
                    }
                }
                return "Error al finalizar el proceso";
            }

            else if (command == "DISCONNECT")
            {
                Application.Exit();
                return "Se desconecto exitosamente";
            }
            else
            {
                return "Comando no reconocido";
            }
        }

        //Retorna el string de la zona horaria del sistema
        static String GetZonaHoraria()
        {
            return TimeZoneInfo.Local.Id;
        }

        private string TakeScreenshot()
        {
            try
            {
                Bitmap screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                    Screen.PrimaryScreen.Bounds.Height,
                    PixelFormat.Format32bppArgb);
                Graphics gfxScreenshot = Graphics.FromImage(screenshot);

                gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                    Screen.PrimaryScreen.Bounds.Y,
                    0,
                    0,
                    Screen.PrimaryScreen.Bounds.Size,
                    CopyPixelOperation.SourceCopy);

                string baseFileName = "screenshot";
                string fileExtension = ".png";

                // Genera un nombre de archivo único
                string fileName = GetUniqueScreenshotFileName(baseFileName, fileExtension);

                // Guarda la captura de pantalla en el archivo
                screenshot.Save(fileName, ImageFormat.Png);

                return $"Captura de pantalla guardada en: {fileName}";
            }
            catch (Exception ex)
            {
                return $"Error al tomar la captura de pantalla: {ex.Message}";
            }
        }

        private string GetUniqueScreenshotFileName(string baseFileName, string fileExtension)
        {
            string fileName = $"{baseFileName}_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";

            // Asegurarse de que el nombre de archivo sea único
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fullPath = Path.Combine(desktopPath, fileName);

            int counter = 1;
            while (File.Exists(fullPath))
            {
                // Si el archivo ya existe, incrementa el contador
                fileName = $"{baseFileName}_{DateTime.Now:yyyyMMdd_HHmmss}_{counter}{fileExtension}";
                fullPath = Path.Combine(desktopPath, fileName);
                counter++;
            }

            return fullPath;
        }

        //private string TakeScreenshot()
        //{
        //    try
        //    {
        //        Bitmap screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
        //            Screen.PrimaryScreen.Bounds.Height,
        //            PixelFormat.Format32bppArgb);
        //        Graphics gfxScreenshot = Graphics.FromImage(screenshot);

        //        gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
        //            Screen.PrimaryScreen.Bounds.Y,
        //            0,
        //            0,
        //            Screen.PrimaryScreen.Bounds.Size,
        //            CopyPixelOperation.SourceCopy);

        //        string baseFileName = "screenshot";
        //        string fileExtension = ".png";

        //        // Genera un nombre de archivo único
        //        string fileName = GetUniqueScreenshotFileName(baseFileName, fileExtension);

        //        // Guarda la captura de pantalla en el archivo
        //        screenshot.Save(fileName, ImageFormat.Png);

        //        // Envía la captura de pantalla al equipo controlador
        //        SendScreenshotToController(fileName);

        //        return $"Captura de pantalla guardada en: {fileName}";
        //    }
        //    catch (Exception ex)
        //    {
        //        return $"Error al tomar la captura de pantalla: {ex.Message}";
        //    }
        //}

        //private void SendScreenshotToController(string fileName)
        //{
        //    if (stream != null && stream.CanWrite)
        //    {
        //        try
        //        {
        //            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
        //            {
        //                byte[] buffer = new byte[1024];
        //                int bytesRead;

        //                // Envía el nombre del archivo primero
        //                byte[] fileNameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(fileName));
        //                stream.Write(fileNameBytes, 0, fileNameBytes.Length);

        //                // Luego envía los datos de la imagen
        //                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
        //                {
        //                    stream.Write(buffer, 0, bytesRead);
        //                }

        //                // Asegúrate de que todo se haya enviado
        //                stream.Flush();
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            UpdateLog($"Error al enviar la captura de pantalla al equipo controlador: {ex.Message}");
        //        }
        //    }
        //}

        //private string GetUniqueScreenshotFileName(string baseFileName, string fileExtension)
        //{
        //    string fileName = $"{baseFileName}_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";

        //    // Asegurarse de que el nombre de archivo sea único
        //    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        //    string fullPath = Path.Combine(desktopPath, fileName);

        //    int counter = 1;
        //    while (File.Exists(fullPath))
        //    {
        //        // Si el archivo ya existe, incrementa el contador
        //        fileName = $"{baseFileName}_{DateTime.Now:yyyyMMdd_HHmmss}_{counter}{fileExtension}";
        //        fullPath = Path.Combine(desktopPath, fileName);
        //        counter++;
        //    }

        //    return fullPath;
        //}

        static String ObtenerFechaHora()
        {
            DateTime fechaHoraActual = DateTime.Now;
            int ano = (fechaHoraActual.Year);
            int mes = (fechaHoraActual.Month);
            int dia = (fechaHoraActual.Day);
            int hora = (fechaHoraActual.Hour);
            int minuto = (fechaHoraActual.Minute);
            int segundo = (fechaHoraActual.Second);
            return "Fecha:  \n" + "Ano: " + ano + " \nMes: " + mes + "\nDia: " + dia + " \nHora: " + hora + " " + minuto + " " + segundo;
        }

        private void MostrarMensaje(string mensaje)
        {
            MessageBox.Show(mensaje, "Mensaje del Controlador");
        }

        static string ObtenerListaProcesos()
        {
            Process[] processes = Process.GetProcesses();
            StringBuilder processList = new StringBuilder("Lista de procesos: \n");

            foreach (Process process in processes)
            {
                processList.Append($"Nombre del proceso: {process.ProcessName}, ID: {process.Id} ");
            }

            return processList.ToString();
        }


        private string ShowDriveInfo()
        {
            StringBuilder mensaje = new StringBuilder();
            DriveInfo[] drives = DriveInfo.GetDrives();

            foreach (DriveInfo drive in drives)
            {
                if (drive.IsReady)
                {
                    mensaje.AppendLine($"Letra de unidad: {drive.Name}\n" +
                        $"Tamano total: {drive.TotalSize / (1024.0 * 1024 * 1024):F2} GB\n" +
                        $"Espacio utilizado: {((drive.TotalSize - drive.TotalFreeSpace) / (1024.0 * 1024 * 1024)):F2} GB\n" +
                        $"Espacio disponible: {drive.TotalFreeSpace / (1024.0 * 1024 * 1024):F2} GB\n" +
                        $"Formato del sistema de archivos: {drive.DriveFormat}");
                }
            }

            // Devolver la cadena resultante
            return mensaje.ToString();
        }

        private string GetProcessorInfo()
        {
            string processorInfo = string.Empty;
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    processorInfo += obj["Name"].ToString();
                }
            }
            return processorInfo;
        }

        private string GetTotalRAM()
        {
            try
            {
                ObjectQuery objectQuery = new ObjectQuery("SELECT * FROM Win32_ComputerSystem");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(objectQuery);

                foreach (ManagementObject item in searcher.Get())
                {
                    if (item["TotalPhysicalMemory"] != null)
                    {
                        long totalPhysicalMemory = Convert.ToInt64(item["TotalPhysicalMemory"]);
                        double gbRam = totalPhysicalMemory / (1024.0 * 1024 * 1024);
                        return $"{gbRam:F2} GB de RAM instalada";
                    }
                }

                return "No se pudo obtener la información de RAM";
            }
            catch (Exception ex)
            {
                return "Error al obtener la informaci�n de RAM: " + ex.Message;
            }
        }

        private string GetResolution()
        {
            Screen primaryScreen = Screen.PrimaryScreen;

            int screenWidth = primaryScreen.Bounds.Width;
            int screenHeight = primaryScreen.Bounds.Height;
            return $"La resolucion de la pantalla es {screenWidth} x {screenHeight} pixeles.";
        }
        private String SubirVolumen()
        {
            Comandos("powershell -c \"(New-Object -ComObject WScript.Shell).SendKeys([char]175)\"");

            return "Subiendo el volumen de la computadora...";
        }

        private String BajarVolumen()
        {
            Comandos("powershell -c \"(New-Object -ComObject WScript.Shell).SendKeys([char]174)\"");

            return "Bajando el volumen de la computadora...";
        }

        private String Silenciar()
        {
            Comandos("powershell -c \"(New-Object -ComObject WScript.Shell).SendKeys([char]173)\"");

            return "Se silencio la computadora";
        }

        private string Apagar()
        {
            Comandos("shutdown /s /t 0");

            return "Se apagara la computadora";
        }
        private String Reiniciar()
        {
            Comandos("shutdown /r /t 0");

            return "Se reiniciara la computadora";
        }
        private string CerrarSesion()
        {
            Comandos("shutdown /l /f");

            return "Se cerrara la sesion";
        }

        private String CerrarProceso(int id)
        {
            try
            {
                Comandos($"taskkill /F /PID {id}");

                return ($"Proceso con ID {id} finalizado");
            }
            catch (Exception ex)
            {
                return ($"Error al intentar finalizar el proceso con ID {id}: {ex.Message}");
            }
        }

        private void Comandos(string comando)
        {
            System.Diagnostics.ProcessStartInfo info = null;
            System.Diagnostics.Process proceso = null;

            info = new System.Diagnostics.ProcessStartInfo("cmd", "/c" + comando);
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;

            proceso = new System.Diagnostics.Process();
            proceso.StartInfo = info;
            proceso.Start();
        }

        private void UpdateLog(string message)
        {
            if (this.IsDisposed || this.Disposing) return;

            if (this.InvokeRequired)
            {
                try
                {
                    if (!this.IsHandleCreated) return; // Verifica si el control ha sido creado

                    this.Invoke(new MethodInvoker(delegate
                    {
                        if (!this.IsDisposed && this.IsHandleCreated) // Verifica nuevamente antes de actualizar
                        {
                            UpdateLog(message);
                        }
                    }));
                }
                catch (ObjectDisposedException)
                {
                    // Puede ser lanzada si el formulario se cierra mientras se está ejecutando Invoke
                }
            }
            else
            {
                logTextBox.AppendText(message + "\r\n");
            }
        }


        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            servidor?.Stop();
            stream?.Close();
        }
    }
}