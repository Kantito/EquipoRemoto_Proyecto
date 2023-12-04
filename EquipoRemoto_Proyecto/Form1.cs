using System;
using System.Diagnostics;
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
            try
            {
                using (NetworkStream clientStream = acceptedClient.GetStream())
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
                UpdateLog("Cliente desconectado: " + ex.Message);
            }
            finally
            {
                acceptedClient?.Close();
            }
        }


        private string ExecuteCommand(string command)
        {
            if (command == "GET_OS_NAME")
            {
                return "Nombre del Sistema Operativo: " + Environment.OSVersion.ToString();
            }
            else if (command == "GET_OS_PLATFORM")
            {
                return "Plataforma del Sistema Operativo: " + Environment.OSVersion.Platform;
            }
            else if (command == "GET_OS_VERSION")
            {
                return "Versión del Sistema Operativo: " + Environment.OSVersion.VersionString;
            }
            else if (command == "GET_COMPUTER_NAME")
            {
                return "Nombre del equipo: " + Environment.MachineName;
            }
            else if (command == "GET_PROCESSOR_INFO")
            {
                return "Información del procesador: " + GetProcessorInfo();
            }
            else if (command == "GET_TOTAL_RAM")
            {
                return "Total RAM (En GB): " + GetTotalRAM();
            }
            else if (command == "GET_LIST_DD")
            {
                return "Lista de unidades del disco duro" + ShowDriveInfo();
            }
            else if (command == "GET_TIME_ZONE")
            {
                return "Zona horaria del sistema: " + GetZonaHoraria();
            }
            else if (command == "GET_TIME_DATE")
            {
                return "Fecha y hora del sistema: " + ObtenerFechaHora();
            }
            else if (command == "GetResolution")
            {
                return "Resolución pantalla: " + GetResolution();
            }
            else
            {
                return "Comando no reconocido";
            }
            //return "Comando ejecutado: " + command;
        }

        //Retorna el string de la zona horaria del sistema
        static String GetZonaHoraria()
        {
            return TimeZoneInfo.Local.Id;
        }
        

        static String ObtenerFechaHora()
        {
            DateTime fechaHoraActual = DateTime.Now;
            int año = (fechaHoraActual.Year);
            int mes = (fechaHoraActual.Month);
            int dia = (fechaHoraActual.Day);
            int hora = (fechaHoraActual.Hour);
            int minuto = (fechaHoraActual.Minute);
            int segundo = (fechaHoraActual.Second);
            return "Fecha:  \n" + "Año: " + año + " \nMes: " + mes + "\nDía: " + dia + " \nHora: " + hora  + " " + minuto + " " + segundo;
        }
        static string ObtenerListaProcesos()
        {
            Process[] processes = Process.GetProcesses();
            StringBuilder processList = new StringBuilder();

            foreach (Process process in processes)
            {
                processList.AppendLine($"Nombre del proceso: {process.ProcessName}, ID: {process.Id}");
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
                        $"Tamaño total: {drive.TotalSize / (1024.0 * 1024 * 1024):F2} GB\n" +
                        $"Espacio utilizado: {((drive.TotalSize - drive.TotalFreeSpace) / (1024.0 * 1024 * 1024)):F2} GB\n" +
                        $"Espacio disponible: {drive.TotalFreeSpace / (1024.0 * 1024 * 1024):F2} GB\n" +
                        $"Formato del sistema de archivos: {drive.DriveFormat}\n");
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
                    processorInfo += obj["Name"].ToString() + "\n";
                }
            }
            return processorInfo;
        }

        private string GetTotalRAM()
        {
            try
            {
                PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available Bytes");
                long ramBytes = Convert.ToInt64(ramCounter.NextValue());
                double gbRam = ramBytes / (1024 * 1024 * 1024);
                return gbRam.ToString("F2") + " GB";
            }
            catch (Exception ex)
            {
                return "Error al obtener la información de RAM: " + ex.Message;
            }
        }

        private string GetResolution()
        {
            Screen primaryScreen = Screen.PrimaryScreen;

            int screenWidth = primaryScreen.Bounds.Width;
            int screenHeight = primaryScreen.Bounds.Height;
            return $"La resolución de la pantalla es {screenWidth} x {screenHeight} píxeles.";
        }

        private void UpdateLog(string message)
        {
            if (this.IsDisposed) return;

            if (this.InvokeRequired)
            {
                try
                {
                    this.Invoke(new MethodInvoker(delegate { UpdateLog(message); }));
                }
                catch (ObjectDisposedException)
                {
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
