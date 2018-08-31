using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.IO;

namespace ConsoleApp1.USER
{
    public class User
    {
        //nombre del usuario
        public string userName;
        //estado del usuario
        public UserState userState;
        //endpoint del socket del usuario
        public IPEndPoint userEP;
        //socket conectado al usuario
        public Socket userSocket;
        //indica si el servidor esta ejecutandose
        public bool serverOn;
        //indica si el usuario esta conectado
        public bool userIsAlive;
        //numero identificador del usuario
        int userNo;
        //objeto servidor
        SimpleTcpSrvr server;

        //funcion que inicializa el objeto e inicia el thread que escucha las acciones del usuario
        public void startClient(Socket inClientSocket, int clientNo, SimpleTcpSrvr Serv)
        {
            //mueve parametros a las variables dentro del objeto
            server = Serv;
            serverOn = true;
            userSocket = inClientSocket;
            userNo = clientNo;
            //inicia el thread que escucha acciones
            Thread ctThread = new Thread(WaitingMessage);
            ctThread.Start();
            userIsAlive = true;
        }

        //funcion que ejecuta el thread que escucha acciones
        public void WaitingMessage()
        {
            //crea el arreglo de bytes que recibira el mensaje, el entero segun yo indica el estado del mensaje recibido
            //pero no estoy 100% seguro de como funciona
            int recv;
            byte[] data = new byte[1024];
            //inf que recibe
            //inicializa el arreglo de bytes y guarda el mensaje recibido en el socket en el arreglo data
            data = new byte[1024];
            recv = this.userSocket.Receive(data);
            //convierte el arreglo de bytes a una cadena ASCII y la guarda en la cadena "userD"
            string userD = Encoding.ASCII.GetString(data, 0, recv);
            //busca un patron para separar la cadena en un arreglo de cadenas usando la cadena estatica declarada
            //en la clase SimpleTcpSrvr
            string[] userData = Regex.Split(userD, SimpleTcpSrvr.splitMsgPattern);
            //el header del mensaje contiene el nombre del usuario y lo asigna a la cadena dentro del objeto User
            this.userName = userData[0];
            //el contenido del mensaje del contiene el estado en el que esta el usuario y se lo asigna al objeto
            switch (userData[1])
            {
                case "Connected":
                    this.userState = UserState.Connected;
                    break;
                case "Absent":
                    this.userState = UserState.Absent;
                    break;
                case "Busy":
                    this.userState = UserState.Busy;
                    break;
            }

            //responde al usuario mandando una cadena que le de la bienvenida al servidor
            //crea la cadena usando el nombre del usuario registrado
            string welcome = "Bienvenido " + this.userName;

            //Crea arreglo de bytes con el tamaño del mensage que se va a enviar
            Byte[] byteslen = BitConverter.GetBytes(welcome.Length);
            //Envia el tamaño del mensaje
            this.userSocket.Send(byteslen, byteslen.Length, SocketFlags.None);

            //convierte la cadena a bytes
            data = Encoding.ASCII.GetBytes(welcome);
            //manda el arreglo de bytes al cliente
            this.userSocket.Send(data, data.Length, SocketFlags.None);

            //mientras el servidor este ejecutandoce estara escuchando mediante este socket
            while (serverOn)
            {
                //recibe el mensaje de accion del cliente
                recv = this.userSocket.Receive(data);
                //si la funcion regresa 0 termina el ciclo para finalizar el thread
                if (recv == 0)
                {
                    break;
                }

                //convierte los bytes recibidos a una cadena
                string msg = Encoding.ASCII.GetString(data, 0, recv);

                //separa la cadena en un arreglo de cadenas
                string[] splitMsg = Regex.Split(msg, SimpleTcpSrvr.splitMsgPattern);

                //si el header del mensaje incluye chat inicia un chat
                if (splitMsg[0].Equals("chat"))
                {
                    //el mensaje dentro de la cadena contiene el nombre del usuario con el que quiere chatear
                    //busca si el usuario esta activo en el servidor si lo esta guarda el usuario en "temp"
                    //si no, lo deja en null
                    User temp = null;
                    for (int i = 0; i < server.countClient; i++)
                    {
                        if (server.client[i].userName.Equals(splitMsg[1]))
                        {
                            temp = server.client[i];
                        }
                    }
                    //manda al usuario el puerto de conexion para el socket del chat
                    userSocket.Send(BitConverter.GetBytes(server.chatPort));
                    //ejecuta la funcion que inicia el thread del chat
                    server.initChat(this, temp, server.chatPort);
                    //cambia el puerto del chat para que no cree conflictos con las demas conexiones del servidor
                    server.chatPort++;
                }
                //si el header contiene el mensaje exit termina el ciclo para finalizar el thread
                else if (splitMsg[0].Equals("exit"))
                {
                    break;
                }

                //OBSOLETO: chat general que responde el mensaje a todos
                //string msg = userName + ": " + Encoding.ASCII.GetString(data, 0, recv);
                //
                //Console.WriteLine(msg);
                //
                //for(int i = 0; i < server.countClient; i++)
                //{
                //    if(i != this.userNo)
                //    {
                //        server.client[i].userSocket.Send(Encoding.ASCII.GetBytes(msg));
                //    }
                //}
                //
                //if (Encoding.ASCII.GetString(data, 0, recv) == "exit")
                //{
                //    break;
                //}
            }

            //indica que el usuario se desconecto
            Console.WriteLine("Desconectado de " + this.userName);
            //cierra socket con el cliente
            this.userSocket.Close();
            //cambia su estado a desconectado
            this.userIsAlive = false;
        }
    }
}
