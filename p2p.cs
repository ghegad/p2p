using System;
using System.Linq;

namespace p2p
{
    class p2p_server
    {
        static Server server;
        public p2p_server()
        {
            Console.WriteLine("Server");
            server = new Server("127.0.0.1",1548);
            server.OnConnectedCompleted += Client_OnConnectedCompleted;
            server.OnDisconnectedCompleted += Client_OnDisconnectedCompleted;
            server.OnMessageCompleted += Client_OnMessageCompleted;
            server.OnErrorCompleted += Client_OnErrorCompleted; 
            server.Start();
            server.Listener();
        }
        private static void Client_OnConnectedCompleted(Serv_Client client)
        {
            Console.WriteLine("\nconnected");
        }
        private static void Client_OnDisconnectedCompleted(Serv_Client client)
        {
            Console.WriteLine("\ndisconnect");
        }
        private static void Client_OnMessageCompleted(Serv_Client client,string message)
        {
            Console.WriteLine("\nMessage from " + client._id + " = " + message);
        }
        private static void Client_OnErrorCompleted(Serv_Client client,string error)
        {
            Console.WriteLine("\nError Client " + client._id + " = " +error);
        }
    }

        class p2p_client
    {
        static Client client;
        public p2p_client(string name)
        {
            Console.WriteLine("Client");
            client = new Client("127.0.0.1",1548,name);
            client.OnConnectedCompleted += Client_OnConnectedCompleted;
            client.OnDisconnectedCompleted += Client_OnDisconnectedCompleted;
            client.OnMessageCompleted += Client_OnMessageCompleted;
            client.OnErrorCompleted += Client_OnErrorCompleted;
            client.Start();
            string lastmsg = "";
            do{
                lastmsg = "";
                Console.Write("\nfor messsage M, show all client C, for exit E");
                lastmsg = Console.ReadLine();
                if(lastmsg.ToUpper() == "C")
                {
                    client.send("server","allusers");
                    foreach(Client_Info Cl in client.clients_list)
                    {
                        Console.WriteLine("\nname of client = "+Cl.Name);
                    }
                }else if(lastmsg.ToUpper() == "M")
                {
                    string selectedclient = "";
                    do{
                        Console.WriteLine("\nyou gonna see client one by one for select a client press T, R for return, or any word for next.");
                        foreach(Client_Info Cl in client.clients_list)
                        {
                            Console.WriteLine("\nname of client = "+Cl.Name);
                            string select = Console.ReadLine();
                            if(select.ToUpper() == "T")
                            {
                                selectedclient = Cl.Name;
                                break;
                            }else if(select.ToUpper() == "R")
                            {
                                break;
                            }
                        }
                    }while(selectedclient == "");
                    if(selectedclient.ToUpper() != "R")
                    {
                        Console.WriteLine("\nyou are with the user "+selectedclient);
                        Console.WriteLine("\nfor exit with user entre exit()");
                        string message = "";
                        while(message.ToUpper() != "EXIT()")
                        {
                            Console.Write("\nentre a message to send : ");
                            message = Console.ReadLine();
                            if(message.ToUpper() != "EXIT()")
                            {
                                client.send(selectedclient,message);
                            }
                        }
                    }
                }
            }while(lastmsg.ToUpper() != "E");
        }
        private static void Client_OnConnectedCompleted()
        {
            Console.WriteLine("\nconnected");
        }
        private static void Client_OnDisconnectedCompleted()
        {
            Console.WriteLine("\ndisconnect");
        }
        private static void Client_OnMessageCompleted(string client,string message)
        {
            Console.WriteLine("\nMessage from " + client + " = " + message);
        }
        private static void Client_OnErrorCompleted(string error)
        {
            Console.WriteLine("\nError "+error);
        }
    }
    class p2p{
        
        static void Main()
        {
            Console.WriteLine("start");
            Console.Write("for Server S, for Client C");
            string selected = Console.ReadLine();
            if(selected.ToUpper() == "S")
            {
                p2p_server Ser = new p2p_server();
            }
            else if(selected.ToUpper() == "C")
            {
                p2p_client Cli = new p2p_client(Console.ReadLine());
            }else{
                Console.WriteLine("prese key to exit");
                Console.ReadKey();
            }
            
        }

    }
}