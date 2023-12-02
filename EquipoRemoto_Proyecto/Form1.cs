using System.Net;
using System.Net.Sockets;

namespace EquipoRemoto_Proyecto
{
    public partial class Form1 : Form
    {
        private TcpListener servidor;
        private TcpClient cliente;
        private NetworkStream stream;

        public Form1()
        {
            InitializeComponent();
        }

        private void IniciarServidor()
        {
            try
            {
                int puerto = 8888; // Puerto en el que escuchará el servidor
                IPAddress direccionIP = IPAddress.Any;
                servidor = new TcpListener(direccionIP, puerto);
                servidor.Start();

                Console.WriteLine("Esperando conexión...");

                while (true)
                {
                    cliente = servidor.AcceptTcpClient();
                    Console.WriteLine("Conexión establecida con el equipo controlador.");

                    stream = cliente.GetStream();
                    Thread hiloCliente = new Thread(AtenderCliente);
                    hiloCliente.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private void AtenderCliente()
        {
            try
            {
                BinaryReader lector = new BinaryReader(stream);

                string mensajeRecibido = lector.ReadString();
                Console.WriteLine("Mensaje recibido: " + mensajeRecibido);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al recibir datos: " + ex.Message);
            }
        }

        private void MostrarMensaje(string mensaje)
        {
            // Método para mostrar mensajes en la interfaz de usuario
            
            // creaarTextBox.AppendText(mensaje + Environment.NewLine);
        }

        private void FormEquipoRemoto_Load(object sender, EventArgs e)
        {
            // Al cargar el formulario, comienza a escuchar conexiones entrantes
            Thread hiloServidor = new Thread(IniciarServidor);
            hiloServidor.IsBackground = true;
            hiloServidor.Start();
        }

    }
}