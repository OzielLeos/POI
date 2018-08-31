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
using System.Threading;
using System.Text.RegularExpressions;

namespace ConsoleApp1.CHAT
{
    public class Chat
    {
        //crea una estructura que contiene un objeto usuario, un socket y un booleano que indica si el usuario esta conectado
        public struct UserChat
        {
            public User user;
            public Socket socket;
            public bool isConnected;
        };

        //crea las variables usuario
        public UserChat user1;
        public UserChat user2;
        //indica si el chat esta activo
        public bool chatOn;
        //objeto servidor
        public SimpleTcpSrvr serv;

        //constructor del objeto chat
        public Chat(User fUser, User sUser, SimpleTcpSrvr server)
        {
            //guarda los parametros en las variables del objeto e inicializa el estado de conexion de los usuarios
            this.user1.user = fUser;
            this.user1.isConnected = false;
            this.user2.user = sUser;
            this.user2.isConnected = false;
            this.serv = server;
        }

        //prepara los el socket del usuario 1
        public void initU1Socket(int port)
        {
            IPEndPoint tempip;
            IPEndPoint ipep;
            Socket temp;

            //obtiene endpoint del usuario y lo asigna al nuevo socket para que solo el usuario del chat se conecte
            //a este socket
            tempip = (IPEndPoint)user1.user.userSocket.RemoteEndPoint;
            ipep = new IPEndPoint(tempip.Address, port);
            //crea el socket y lo asigna el socket del usuario 1
            temp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            temp.Bind(ipep);

            user1.socket = temp;
        }

        //prepara el socket del usuario 2
        public void initU2Socket(int port)
        {
            IPEndPoint tempip;
            IPEndPoint ipep;
            Socket temp;

            //obtiene endpoint del usuario y lo asigna al nuevo socket para que solo el usuario del chat se conecte
            //a este socket
            temp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tempip = (IPEndPoint)user2.user.userSocket.RemoteEndPoint;
            //crea el socket y lo asigna el socket del usuario 2
            ipep = new IPEndPoint(tempip.Address, port);
            temp.Bind(ipep);

            user2.socket = temp;
        }

        //UNUSED: inicia los thrads de los 2 usuarios al mismo tiempo
        public void startThreads()
        {
            chatOn = true;
            user1.isConnected = true;
            user2.isConnected = true;
            Thread u1Thread = new Thread(WaitingMessageU1);
            Thread u2Thread = new Thread(WaitingMessageU2);
            u1Thread.Start();
            u2Thread.Start();
        }

        //inicia el thread del usuario 1
        public void startU1Thread()
        {
            //escucha la conexion del usuario y prepara los datos
            user1.socket.Listen(10);
            chatOn = true;
            user1.socket = user1.socket.Accept();
            user1.isConnected = true;
            //inicia el thread
            Thread u1Thread = new Thread(WaitingMessageU1);
            u1Thread.Start();
        }

        //inicia el thread del usuario 2
        public void startU2Thread()
        {
            //esucha la conexion del usuario y prepara los datos
            user2.socket.Listen(10);
            chatOn = true;
            user2.socket = user2.socket.Accept();
            user2.isConnected = true;
            //inicia el thread
            Thread u2Thread = new Thread(WaitingMessageU2);
            u2Thread.Start();
        }

        //funcion que ejecuta el thread el usuario 1
        public void WaitingMessageU1()
        {
            //arreglo de bytes que recibe mensaje
            int recv;
            byte[] data = new byte[1024];

            //recube el mensaje
            recv = user1.socket.Receive(data);
            //si recv es diferente de 0 anuncia que se creo la conexion
            if(recv != 0)
            {
                Console.WriteLine(user1.user.userName + " ha iniciado un chat");
                if (user2.isConnected)
                {
                    string welcomeMsg = user1.user.userName + " se ha conectado!";
                    user2.socket.Send(Encoding.ASCII.GetBytes(welcomeMsg));
                }
            }

            //si el booleano de user1 indica que el usuario esta conectado ejecuta el ciclo de escucha
            while (user1.isConnected)
            {
                //incializa arreglo de bytes y recibe datos
                data = new byte[1024];
                recv = user1.socket.Receive(data);

                //si recv es 0 finaliza conexion y thread
                if (recv == 0)
                {
                    break;
                }

                //convierte el arreglo de bytes a cadena y separa el header del mensaje
                string userMsg = Encoding.ASCII.GetString(data, 0, recv);
                string[] splitMsg = Regex.Split(userMsg, SimpleTcpSrvr.splitMsgPattern);

                //si el header es message
                if (splitMsg[0].Equals("message"))
                {
                    //si el usuario 2 esta conectado manda el mensaje al usuario 2
                    if (user2.isConnected)
                    {
                        Console.WriteLine(user1.user.userName + " @" + user2.user.userName + ": " + splitMsg[1]);
                        user2.socket.Send(Encoding.ASCII.GetBytes(splitMsg[1]));
                    }
                    //si usuario 2 no esta conectado solo imprime el mensaje en el servidor
                    else
                    {
                        Console.WriteLine(user1.user.userName + " @" + "??????" + ": " + splitMsg[1]);
                    }
                }
                //si el header es exit indica que el usuario se desconecto del chat, manda mensaje al otro usuario
                //que se desconecto y finaliza la conexion y el thread
                else if (splitMsg[0].Equals("exit"))
                {
                    Console.WriteLine(user1.user.userName + " se ha desconectado del chat");
                    if (user2.isConnected)
                    {
                        user2.socket.Send(Encoding.ASCII.GetBytes(splitMsg[1]));
                    }
                    user1.isConnected = false;
                }
            }

            //cierra el socket
            user1.socket.Close();
        }

        //hace exactamente lo mismo que la funcion de arriba pero invierte los papeles de los usuarios
        public void WaitingMessageU2()
        {
            int recv;
            byte[] data = new byte[1024];

            recv = user2.socket.Receive(data);
            if (recv != 0)
            {
                Console.WriteLine(user2.user.userName + " se ha conectado a un chat con " + user1.user.userName);
                if (user1.isConnected)
                {
                    string welcomeMsg = user2.user.userName + " se ha conectado!";
                    user1.socket.Send(Encoding.ASCII.GetBytes(welcomeMsg));
                }
            }

            while (user2.isConnected)
            {
                data = new byte[1024];
                recv = user2.socket.Receive(data);

                if (recv == 0)
                {
                    break;
                }

                string userMsg = Encoding.ASCII.GetString(data, 0, recv);
                string[] splitMsg = Regex.Split(userMsg, SimpleTcpSrvr.splitMsgPattern);

                if (splitMsg[0].Equals("message"))
                {
                    if (user1.isConnected)
                    {
                        Console.WriteLine(user2.user.userName + " @" + user1.user.userName + ": " + splitMsg[1]);
                        user1.socket.Send(Encoding.ASCII.GetBytes(splitMsg[1]));
                    }
                    else
                    {
                        Console.WriteLine(user2.user.userName + " @" + "??????" + ": " + splitMsg[1]);
                    }
                }
                else if (splitMsg[0].Equals("exit"))
                {
                    Console.WriteLine(user2.user.userName + " se ha desconectado del chat");
                    if (user1.isConnected)
                    {
                        user1.socket.Send(Encoding.ASCII.GetBytes(splitMsg[1]));
                    }
                    user2.isConnected = false;
                }
            }

            user2.socket.Close();
        }
    }
}
