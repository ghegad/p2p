using System;
using System.Net.Sockets;
using System.Net;
using System.Collections;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;

namespace p2p
{

    /// <summary>
    /// Class <c>Serv_Client</c> models Client of Server.
    /// </summary>
    public class Serv_Client
    {
        //index count of client
        static int nrOfInstances = 0;
        //index of client
        public int _id { get; private set; }
        //client name
        public string name;
        //client object
        public TcpClient Tcpclient;
        //client rsa key public
        public string rsakey;
        /// <summary>
        /// constructor <c>Serv_Client</c> implement the client.
        /// </summary>
        //constructor of client
        public Serv_Client(TcpClient tcpclient)
        {
            _id = nrOfInstances;
            nrOfInstances++;
            this.Tcpclient = tcpclient;
        }
    }
    /// <summary>
    /// Class <c>Message</c> models message for send and recive.
    /// </summary>
    public class Message
    {
        // the name of client to send
        public string from { get; set; }
        // the message to send
        public string message { get; set; }
        // the rsa public key or the AES256 key crypted by rsa public key
        public string key { get; set; }
        // the name of client
        public string name { get; set; }
    }
    /// <summary>
    /// Class <c>Server</c> the main Class of TCPSERVER.
    /// </summary>
    class Server
    {
        //// Begin Events
        public delegate void CustomConnectedEventHandler(Serv_Client sender);
        public delegate void CustomDisconnectedEventHandler(Serv_Client sender);
        public delegate void CustomErrorEventHandler(Serv_Client sender, string error);
        public delegate void CustomMessageEventHandler(Serv_Client sender, string message);

        public event CustomConnectedEventHandler OnConnectedCompleted;
        public event CustomDisconnectedEventHandler OnDisconnectedCompleted;
        public event CustomErrorEventHandler OnErrorCompleted;
        public event CustomMessageEventHandler OnMessageCompleted;

        /// <summary>
        /// method <c>OnConnected</c> implement function if client connected to server.
        /// </summary>
        protected virtual void OnConnected(Serv_Client client)
        {
            OnConnectedCompleted?.Invoke(client);
        }
        /// <summary>
        /// method <c>OnDisconnected</c> implement function if client disconnected.
        /// </summary>
        protected virtual void OnDisconnected(Serv_Client client)
        {
            // check if client have name and rsa public key
            if (client.name != null && client.rsakey != null)
            {
                // creat a message for send to all client
                Message new_msg = new Message();
                new_msg.message = "remouveclient";
                new_msg.name = client.name;
                new_msg.key = client.rsakey;
                new_msg.from = "server";
                Brodcast(JsonSerializer.Serialize<Message>(new_msg));
            }
            // delet the client in the list of all clients
            clients_list.Remove(client);
            //implement function client disconnected
            OnDisconnectedCompleted?.Invoke(client);
        }
        /// <summary>
        /// method <c>OnError</c> implement function if client have error or if server have error.
        /// </summary>
        protected virtual void OnError(Serv_Client client, string error) => OnErrorCompleted?.Invoke(client, error);
        /// <summary>
        /// method <c>OnMessage</c> recive the message.
        /// </summary>
        protected virtual void OnMessage(Serv_Client client, string message)
        {
            // convert message to json Message format
            Message msg = JsonSerializer.Deserialize<Message>(message);
            // if message is for server
            if (msg?.from == "server")
            {
                // if message have infos of client
                if (msg?.message == "info")
                {
                    // check if name of client is not server
                    if (msg?.name != "server")
                    {
                        // implement the name and the rsa key of client with his message
                        client.name = msg?.name;
                        client.rsakey = msg?.key;
                        // send message to all client to add the new client in here list clinet
                        Message new_msg = new Message();
                        new_msg.message = "newclient";
                        new_msg.name = msg?.name;
                        new_msg.key = msg?.key;
                        new_msg.from = "server";
                        Brodcast(JsonSerializer.Serialize<Message>(new_msg));
                    }
                    else
                    {
                        // whene the name of client is server send message error to client
                        Message new_msg = new Message();
                        new_msg.from = "server";
                        new_msg.message = "you can't use the name : server !";
                        send_message(client, JsonSerializer.Serialize<Message>(new_msg));
                    }
                }// client want all clients info
                else if (msg?.message == "allusers")
                {
                    foreach (Serv_Client sc in clients_list)
                    {
                        if (sc.name != null && sc.rsakey != null)
                        {
                            Message new_msg = new Message();
                            new_msg.message = "newclient";
                            new_msg.name = sc.name;
                            new_msg.key = sc.rsakey;
                            new_msg.from = "server";
                            send_message(client, JsonSerializer.Serialize<Message>(new_msg));
                        }
                    }
                }// if message is for server implement function OnMessage of server
                else
                {
                    OnMessageCompleted?.Invoke(client, msg?.message);
                }
            }// if message is for client transfer the message to the client
            else
            {
                foreach (Serv_Client cli in clients_list)
                {
                    if (cli.name == msg?.from)
                    {
                        Message new_msg = new Message();
                        // change the name of sender to name of client
                        new_msg.from = client.name;
                        new_msg.message = msg?.message;
                        new_msg.key = msg?.key;
                        new_msg.name = msg?.name;
                        send_message(cli, JsonSerializer.Serialize<Message>(new_msg));
                    }
                }
            }
        }
        //// End Events
        // the server object
        TcpListener server;
        // the list of clients
        ArrayList clients_list = new ArrayList();
        // IP address of server
        IPAddress Ip;
        // port of server
        int Port;
        /// <summary>
        /// constructor <c>Server</c> implement the Server.
        /// </summary>
        public Server(string ip, int port)
        {
            this.Port = port;
            this.Ip = IPAddress.Parse(ip);
            server = new TcpListener(this.Ip, this.Port);
        }
        /// <summary>
        /// method <c>Start</c> to start the Server.
        /// </summary>
        public void Start()
        {
            try
            {
                server.Start();
            }
            catch (Exception e)
            {
                OnError(null, e.Message.ToString());
            }
        }
        /// <summary>
        /// method <c>Stop</c> to stop the Server.
        /// </summary>
        public void Stop()
        {
            if (server.Server.Connected)
            {
                server.Stop();
                clients_list = new ArrayList();
            }
        }
        /// <summary>
        /// method <c>Brodcast</c> to send a message to all Clients in the Server.
        /// </summary>
        private void Brodcast(string message)
        {
            if (server.Server.Connected)
            {
                foreach (Serv_Client client in clients_list)
                {
                    send_message(client, message);
                }
            }
        }
        /// <summary>
        /// method <c>send_message</c> is a private method to send a message string to a client.
        /// </summary>
        private void send_message(Serv_Client client, string message)
        {
            NetworkStream stream = client.Tcpclient.GetStream();
            byte[] list = Encoding.UTF8.GetBytes(message);
            stream.Write(list, 0, list.Length);
        }
        /// <summary>
        /// method <c>send_message</c> is a public method to send a message format Message Class Json to client.
        /// </summary>
        public void send(Serv_Client client, string message)
        {
            Message new_msg = new Message();
            new_msg.from = "server";
            new_msg.message = message;
            NetworkStream stream = client.Tcpclient.GetStream();
            byte[] list = Encoding.UTF8.GetBytes(JsonSerializer.Serialize<Message>(new_msg));
            stream.Write(list, 0, list.Length);
        }
        /// <summary>
        /// method <c>Listener</c> wait client connect.
        /// </summary>
        public void Listener()
        {
            while (true)
            {
                try
                {
                    TcpClient client = server.AcceptTcpClient();
                    client.ReceiveBufferSize = 8192;
                    Serv_Client cl = new Serv_Client(client);
                    //add new client to list of client
                    clients_list.Add(cl);
                    //Listener_connect if client if connected or not
                    _ = Listener_connect(cl);
                    //wait for message of client
                    _ = Listener_message(cl);
                    //call OnConnected event
                    OnConnected(cl);
                }
                catch {
                    //in case of error break
                     break;
                      }
            }
        }
        /// <summary>
        /// method <c>Listener_connect</c> check if client is disconnected.
        /// </summary>
        protected async Task Listener_connect(Serv_Client client)
        {
            while (true)
            {
                try
                {
                    NetworkStream stream = client.Tcpclient.GetStream();
                    stream.Write(new byte[0], 0, 0);
                }
                catch
                {
                    OnDisconnected(client);
                    break;
                }
                await Task.Delay(1000);
            }
        }
        /// <summary>
        /// method <c>Listener_message</c> check if client send a message.
        /// </summary>
        protected async Task Listener_message(Serv_Client client)
        {
            while (true)
            {
                try
                {
                    NetworkStream stream = client.Tcpclient.GetStream();
                    while (true)
                    {
                        try
                        {
                            while (!stream.DataAvailable) await Task.Delay(100);
                            while (client.Tcpclient.Client.Available < 3) await Task.Delay(100);

                            byte[] bytes = new byte[client.Tcpclient.Client.Available];
                            stream.Read(bytes, 0, client.Tcpclient.Client.Available);
                            string s = Encoding.UTF8.GetString(bytes);
                            if (s.Trim() != "")
                                OnMessage(client, s);
                        }
                        catch (Exception e)
                        {
                            OnError(client, e.Message.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    OnError(client, e.Message.ToString());
                }
            }
        }
    }
}
