using System;
using System.Net.Sockets;
using System.Net;
using System.Collections;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;

namespace p2p
{
    public class Serv_Client{
        static int nrOfInstances = 0;
        public int _id {get; private set;}
        public string name;
        public TcpClient Tcpclient;
        public string rsakey;
        public Serv_Client(TcpClient tcpclient)
        {
            _id = nrOfInstances;
            nrOfInstances++;
            this.Tcpclient = tcpclient;
        }
    } 

    public class Message
    {
        public string from { get; set; }
        public string message { get; set; }
        public string key { get; set; }
        public string name { get; set; }
    }
    class Server
    {
        //// Begin Events

        public delegate void CustomConnectedEventHandler(Serv_Client sender);
        public delegate void CustomDisconnectedEventHandler(Serv_Client sender);
        public delegate void CustomErrorEventHandler(Serv_Client sender,string error);
        public delegate void CustomMessageEventHandler(Serv_Client sender,string message);
        
        public event CustomConnectedEventHandler OnConnectedCompleted;
        public event CustomDisconnectedEventHandler OnDisconnectedCompleted;
        public event CustomErrorEventHandler OnErrorCompleted;
        public event CustomMessageEventHandler OnMessageCompleted;

        protected virtual void OnConnected(Serv_Client client){ 
            OnConnectedCompleted?.Invoke(client);
        }
        protected virtual void OnDisconnected(Serv_Client client){ 
            if(client.name != null && client.rsakey != null){
            Message new_msg = new Message();
            new_msg.message = "remouveclient";
            new_msg.name = client.name;
            new_msg.key = client.rsakey;
            new_msg.from = "server";
            Brodcast(JsonSerializer.Serialize<Message>(new_msg));
            }
            clients_list.Remove(client);
            OnDisconnectedCompleted?.Invoke(client);
        }
        protected virtual void OnError(Serv_Client client, string error) => OnErrorCompleted?.Invoke(client, error);
        protected virtual void OnMessage(Serv_Client client, string message)
        {
            Message msg = JsonSerializer.Deserialize<Message>(message);
            if(msg?.from == "server")
            {
                if(msg?.message == "info")
                {
                    if(msg?.name != "server")
                    {
                        client.name = msg?.name;
                        client.rsakey = msg?.key;
                        Message new_msg = new Message();
                        new_msg.message = "newclient";
                        new_msg.name = msg?.name;
                        new_msg.key = msg?.key;
                        new_msg.from = "server";
                        Brodcast(JsonSerializer.Serialize<Message>(new_msg));
                    }
                    else
                    {
                        Message new_msg = new Message();
                        new_msg.from = "server";
                        new_msg.message = "you cant use the name : server !";
                        send_message(client,JsonSerializer.Serialize<Message>(new_msg));
                    }
                }else if(msg?.message == "allusers"){
                        foreach(Serv_Client sc in clients_list)
                        {
                            if(sc.name != null && sc.rsakey != null){
                            Message new_msg = new Message();
                            new_msg.message = "newclient";
                            new_msg.name = sc.name;
                            new_msg.key = sc.rsakey;
                            new_msg.from = "server";
                            send_message(client,JsonSerializer.Serialize<Message>(new_msg));
                        }
                    }
                }else{
                        OnMessageCompleted?.Invoke(client, msg?.message);
                }
            }else{
                foreach(Serv_Client cli in clients_list)
                {
                    if(cli.name == msg?.from)
                    {
                        Message new_msg = new Message();
                        new_msg.from = client.name;
                        new_msg.message = msg?.message;
                        new_msg.key = msg?.key;
                        new_msg.name = msg?.name;
                        send_message(cli,JsonSerializer.Serialize<Message>(new_msg));
                    }
                }
            }
        }
        //// End Events

        TcpListener server;
        ArrayList clients_list = new ArrayList();
        IPAddress Ip;
        int Port;
        public Server(string ip, int port)
        {
            this.Port = port;
            this.Ip = IPAddress.Parse(ip);
            server = new TcpListener(this.Ip, this.Port);
        }
        public void Start()
        {
            try
            {
                server.Start();
            }
            catch (Exception e)
            {
                OnError(null,e.Message.ToString());
            }
        }
        public void Stop()
        {
            if (server.Server.Connected){
                server.Stop();
                clients_list = new ArrayList();
                }
        }

        public void Brodcast(string message)
        {
            if (server.Server.Connected){
                foreach(Serv_Client client in clients_list)
                {
                    send_message(client,message);
                }
            }
        }

        private void send_message(Serv_Client client,string message)
        {
            NetworkStream stream = client.Tcpclient.GetStream();
            byte[] list = Encoding.UTF8.GetBytes(message);
            stream.Write(list, 0, list.Length);
        }

        public void send(Serv_Client client,string message)
        {
            Message new_msg = new Message();
            new_msg.from = "server";
            new_msg.message = message;
            NetworkStream stream = client.Tcpclient.GetStream();
            byte[] list = Encoding.UTF8.GetBytes(JsonSerializer.Serialize<Message>(new_msg));
            stream.Write(list, 0, list.Length);
        }

        public void Listener()
        {
                while (true)
                {
                try
                {
                    TcpClient client = server.AcceptTcpClient();
                    client.ReceiveBufferSize = 8192;
                    Serv_Client cl = new Serv_Client(client);
                    clients_list.Add(cl);
                    _ = Listener_connect(cl);
                    _ = Listener_message(cl);
                    OnConnected(cl);
                }
                catch { break; }
                }
        }

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
