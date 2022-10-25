using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Collections;

namespace p2p
{
    /// <summary>
    /// Class <c>Client_Info</c> models with the Client info.
    /// </summary>
    public class Client_Info
    {
        public string Name;
        public string Key;
        public Client_Info(string name, string key)
        {
            this.Name = name;
            this.Key = key;
        }
    }
    public class Client
    {
        // list of clients
        public ArrayList clients_list = new ArrayList();
        // the client object
        private TcpClient client;
        // the rsa object with rsa keys and functions
        private rsa RSA;
        // the ip address of server
        private string ip;
        // the port of server
        private int port;
        // the name of client
        private string name;

        //// Begin Event
        public delegate void CustomConnectedEventHandler();
        public delegate void CustomDisconnectedEventHandler();
        public delegate void CustomMessageEventHandler(string from, string message);
        public delegate void CustomErrorEventHandler(string error);
        public event CustomConnectedEventHandler OnConnectedCompleted;
        public event CustomDisconnectedEventHandler OnDisconnectedCompleted;
        public event CustomErrorEventHandler OnErrorCompleted;
        public event CustomMessageEventHandler OnMessageCompleted;

        protected virtual void OnConnected()
        {
            OnConnectedCompleted?.Invoke();
        }
        protected virtual void OnDisconnected()
        {
            OnDisconnectedCompleted?.Invoke();
        }
        protected virtual void OnError(string error)
        {
            OnErrorCompleted?.Invoke(error);
        }
        /// <summary>
        /// method <c>OnMessage</c> recive the message.
        /// </summary>
        protected virtual void OnMessage(string message)
        {
            try
            {
                // convert message to json Message format
                Message msg = JsonSerializer.Deserialize<Message>(message);
                // if message is from server
                if (msg.from == "server")
                {
                    // if a new client has connected to server
                    if (msg.message == "newclient")
                    {
                        // if the new client have infos
                        if (msg.name != this.name && msg.key != this.RSA.publickey)
                        {
                            Client_Info ci = new Client_Info(msg.name, msg.key);
                            // add client to clist of clients 
                            if (!clients_list.Contains(ci))
                                clients_list.Add(ci);
                        }
                    }// if a client is disconnected to server
                    else if (msg.message == "remouveclient")
                    {
                        Client_Info ci = new Client_Info(msg.name, msg.key);
                        // remouve client from clist of clients 
                        if (clients_list.Contains(ci))
                            clients_list.Remove(ci);
                    }
                    else
                    {
                        OnMessageCompleted?.Invoke(msg.from, msg.message);
                    }
                }// if message is from client
                else
                {
                    string text = "";
                    string key = "";
                    try
                    {
                        //decrypt the AES256 key
                        key = rsa.Decryption(msg.key, RSA.privatekey);
                        //decrypt the message with the AES 256 key
                        text = rsa.DecryptText(msg.message, key);
                    }
                    catch
                    {
                        Console.WriteLine("error rsa = " + key);
                        text = msg.message;
                    }
                    OnMessageCompleted?.Invoke(msg.from, text);
                }
            }
            catch { return; }
        }
        //// End Event
        /// <summary>
        /// constructor <c>Client</c> implement the Client.
        /// </summary>
        public Client(string server, int port, string name)
        {
            this.ip = server;
            this.port = port;
            if (name != "server")
                this.name = name;
            else
                this.name = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 5).Select(s => s[new Random().Next(s.Length)]).ToArray());
            RSA = new rsa();
        }
        /// <summary>
        /// method <c>Start</c> to start the Client.
        /// </summary>
        public void Start()
        {
            _ = Task.Run(() =>
            {
                try
                {
                    this.client = new TcpClient(this.ip, this.port);
                    this.client.ReceiveBufferSize = 8192;
                    OnConnected();
                    // send the client infos
                    Sendparams();
                    // Listener if client is connected
                    Listener();
                    // Listener of message
                    Listenermsg();
                }
                catch (Exception e)
                {
                    OnError(e.Message.ToString());
                }
            });
        }
        /// <summary>
        /// method <c>Sendparams</c> send the client info.
        /// </summary>
        protected void Sendparams()
        {
            Message new_msg = new Message();
            new_msg.name = name;
            new_msg.key = RSA.publickey;
            new_msg.from = "server";
            new_msg.message = "info";
            NetworkStream stream = client.GetStream();
            byte[] namebyte = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new_msg));
            stream.Write(namebyte, 0, namebyte.Length);
        }
        /// <summary>
        /// method <c>send</c> send a message format Message.
        /// </summary>
        public void send(string name_client, string message)
        {
            string key = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 8).Select(s => s[new Random().Next(s.Length)]).ToArray());
            string sender_key = "";
            string msg = message;
            foreach (Client_Info cl in clients_list)
            {
                if (cl.Name == name_client)
                {
                    msg = rsa.EncryptText(message, key);
                    sender_key = rsa.Encryption(key, cl.Key);
                }
            }
            Message new_msg = new Message();
            new_msg.from = name_client;
            new_msg.message = msg;
            new_msg.key = sender_key;
            NetworkStream stream = client.GetStream();
            byte[] list = Encoding.UTF8.GetBytes(JsonSerializer.Serialize<Message>(new_msg));
            stream.Write(list, 0, list.Length);
        }
        /// <summary>
        /// method <c>send_message</c> send a message format string.
        /// </summary>
        private void send_message(string message)
        {
            NetworkStream stream = client.GetStream();
            byte[] list = Encoding.UTF8.GetBytes(message);
            stream.Write(list, 0, list.Length);
        }
        /// <summary>
        /// method <c>Listener</c> Listener client status.
        /// </summary>
        protected void Listener()
        {
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(new byte[0], 0, 0);
                    }
                    catch
                    {
                        OnDisconnected();
                        break;
                    }
                    await Task.Delay(1000);
                }
            });
        }
        /// <summary>
        /// method <c>Listenermsg</c> Listener of message.
        /// </summary>
        public void Listenermsg()
        {
            NetworkStream stream = client.GetStream();
            while (true && client.Connected)
            {
                try
                {
                    byte[] data = new byte[client.Available];
                    string responseData = string.Empty;
                    int bytes = stream.Read(data, 0, client.Available);
                    responseData = Encoding.UTF8.GetString(data, 0, bytes);
                    if (responseData.Trim() != "")
                        OnMessage(responseData);
                }
                catch (Exception e)
                {
                    OnError(e.Message.ToString());
                }
            }
        }
    }
}
