//SERVIDOR
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using ConsoleApp1.USER;
using ConsoleApp1.CHAT;
using System.Threading;
using System.Runtime.InteropServices;

public class SimpleTcpSrvr
{
    //listas de clientes y chats
    public List<User> client;
    public List<Chat> chats;
    //socket principal
    Socket serverSocket;
    //thread que escucha conecciones
    Thread waitClient;
    //booleano que indica si el servidor esta ejecutandoce
    bool serverOn;
    //puerto del socket principal
    public int port = 9050;
    //puerto para los sockets de chat (este valor estara cambiando cada socket)
    public int chatPort = 10050;

    //esta variable es la manera en la que puedo separar la cadena que reciba del socket a un arreglo de cadenas
    //mediante el cual separare el header del mensaje(indicara que accion tomar con el mensaje) y el mensaje
    public static string splitMsgPattern = "\r\n";

    //variable que tiene el tamaño de la lista "client"
    public int countClient;
    //variable que tiene el tamaño de la lista "chats"
    public int chatCount;

    //funcion que inicia un objeto chat entre 2 usuarios y crea los threads de listeners para cada usuario
    public void initChat(User user1, User user2, int chatPort)
    {
        //variable booleana que se usa para checar si ya existe un chat con el usuario 2
        bool exists = false;

        //busca un chat donde el usuario 2 se el usuario 1
        for (int i = 0; i < chatCount; i++)
        {
            //si el usuario 2 existe en otro chat agrega usuario 1 al chat ya existente
            if (user2 != null && chats[i].user2.user == null && chats[i].user1.user.userName.Equals(user2.userName))
            {
                exists = true;
                chats[i].user2.user = user1;
                chats[i].initU2Socket(chatPort);
                chats[i].startU2Thread();
            }
        }

        //si no existe un chat con usuario 2 o usuario 2 es null crea un chat nuevo con usuario 1
        if (!exists)
        {
            Chat chatin = new Chat(user1, user2, this);
            if (chatin.user1.user != null)
            {
                int iterator = chatCount;
                chatin.initU1Socket(chatPort);
                chatin.startU1Thread();
                chats.Add(chatin);
                chatCount++;
            }
        }
    }

    //inicializa la lista y prepara el thread principal
    public void init()
    {
        client = new List<User>();
        chats = new List<Chat>();
        waitClient = new Thread(new ThreadStart(WaitingClient));
        serverOn = true;
    }

    //prepara la lista "client" para esperar usuarios e inicia el thread que los espera
    public void load()
    {
        client.Add(new User());
        waitClient.Start();
    }

    //funcion que ejecuta el thread que escucha usuarios
    public void WaitingClient()
    {
        //arreglo que guarda los datos que recibe el socket
        byte[] data = new byte[1024];

        //variable tipo IPEndpoint, a esa variable se le asigna la direccion IP del otro socket y el puerto por el que se conectara
        IPEndPoint ipep = new IPEndPoint(IPAddress.Any, port);

        //inicializa socket, familia de direccion es red de internet, el tipo de socket es stream y el protocolo es TCP
        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //se le asigna el endpoint al socket
        serverSocket.Bind(ipep);

        //este while mantiene el thread ejecutandoce mientras el servidor este activo
        while (serverOn)
        {
            //socket escucha si hay una conexion entrante
            serverSocket.Listen(10);
            Console.WriteLine("Esperando un cliente...");
            
            //try por si algo sale mal durante la conexion
            try
            {
                //llama la funcion que inicia el thread que escucha los mensajes de accion
                client[countClient].startClient(serverSocket.Accept(), countClient, this);
                //guarda el endpoint de la conexion entrante en el objeto User
                client[countClient].userEP = (IPEndPoint)client[countClient].userSocket.RemoteEndPoint;

                //escribe en consola la ip y el puerto entrante
                Console.WriteLine("Conectando con " + client[countClient].userEP.Address + " por el puerto " +
                        client[countClient].userEP.Port + "\n");

                //incrementa la variable que contiene el tamaño de la lista "client" y agrega un nuevo objeto vacio
                //a la lista para esperar a otro usuario
                countClient++;
                client.Add(new User());
            }
            catch(SocketException e)
            {
                Thread.Sleep(0);
            }
        }
    }

    //funcion que cierra el socket TCP que escucha clientes y termina el thread que escucha
    private void closeServer()
    {
        //termina los ciclos de escuchar de cada cliente
        for (int i = 0; i < countClient; i++)
        {
            client[i].serverOn = false;
        }

        //cierra socket y termina thread
        serverSocket.Close();
        waitClient.Join();
    }

    //detiene el ciclo de escucha nuevos usuarios y ejecuta la funcion closeServer
    private void serverClosing()
    {
        serverOn = false;
        closeServer();
    }

    //funcion principal que ejecuta la consola
    public static void Main()
    {
        //como la funcion es estatica ocupa un objeto de la clase principal para poder ejecutar sus funciones
        SimpleTcpSrvr server = new SimpleTcpSrvr();

        //ejecuta las funciones que inician el thread que escucha
        server.init();
        server.load();
        
        //mantiene el thread principal ejecutandose
        while (server.serverOn)
        {
            
        }
        //finaliza las conexiones
        server.serverClosing();
    }

    //esta funcion no esa, pera estaba planeada para que ejecutara las funciones de terminar conexion al cerrar la ventana de consola
    static bool ConsoleEventCallback(int eventType)
    {
        if (eventType == 2)
        {
            Console.WriteLine("Console window closing, death imminent");
        }
        return false;
    }
    static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected
    // Pinvoke
    private delegate bool ConsoleEventDelegate(int eventType);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
}

