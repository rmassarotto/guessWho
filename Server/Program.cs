using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Chat {
    class Servidor {
        private static TcpListener serverSocket = default(TcpListener);
        private static Socket clientSocket = default(Socket);
        private static readonly int maxClientsCount = 2;
        private static readonly handleClient[] clients = new handleClient[maxClientsCount];

        static void Main(string[] args) {

            Console.Title = "Servidor";
            
            serverSocket = new TcpListener(IPAddress.Any, 7777);
            clientSocket = default(Socket);
            serverSocket.Start();
                        
            while (true) {
                Console.WriteLine("Esperando conexões...");                
                clientSocket = serverSocket.AcceptSocket();
                Console.WriteLine("Conectado!");
                int i = 0;
                for (i = 0; i < maxClientsCount; i++) {
                    if (clients[i] == null) {
                        (clients[i] = new handleClient()).startClient(clientSocket, clients);
                        break;
                    }
                }

                if (i == maxClientsCount) {
                    StreamWriter ots = new StreamWriter(new NetworkStream(clientSocket));
                    ots.AutoFlush = true;
                    ots.WriteLine("*** Servidor Cheio ***");
                    ots.Close();
                    clientSocket.Close();
                }
            }
        }
    }

    public class handleClient {
        private Socket clientSocket;
        private handleClient[] clients;
        private int maxClientsCount;
        private String clientName;
        private StreamReader ins;
        private StreamWriter ots;
        private String palavra;

        public void startClient(Socket inClientSocket, handleClient[] clients) {
            this.clientSocket = inClientSocket;
            this.clients = clients;
            this.maxClientsCount = clients.Length;
            
            ots = new StreamWriter(new NetworkStream(clientSocket));
            ots.AutoFlush = true;

            if (inClientSocket.Equals(clients[0].clientSocket))
            {
                ots.WriteLine("*** Voce e o mestre ***");
            }

            Thread ctThread = new Thread(doChat);
            ctThread.Start();
        }

        private Boolean checkCorrect(String s) {
            if (s.Equals("") || s.Equals("\n")) {
                return false;
            }

            for (int i = 0; i < s.Length; i++) {
                if (!char.IsLetterOrDigit(s.ElementAt(i))) {
                    return false;
                }
            }

            return true;
        }

        private Boolean checkCommand(String s) {
            if (s.Equals("/list") || s.Equals("/quit") || s.Equals("") || s.Equals("\n")) {
                return true;
            }

            return false;
        }

        private void doChat() {
            int maxClientsCount = this.maxClientsCount;
            handleClient[] clients = this.clients;

            try {
                ins = new StreamReader(new NetworkStream(clientSocket));
                ots = new StreamWriter(new NetworkStream(clientSocket));
                ots.AutoFlush = true;
                String name;

                ots.WriteLine("*** Informe seu nome ***");
                name = ins.ReadLine().Trim();

                Console.WriteLine("Novo usuario: " + name);
                ots.WriteLine("*** Ola " + name + " ***\n*** Para sair digite /quit ***");
                ots.WriteLine("*** Para ver os usuarios conectados digite /list ***");
                lock(this) {
                    for(int i=0; i<maxClientsCount; i++) {
                        if(clients[i] != null && clients[i] == this) {
                            clientName = "@" + name;
                            break;
                        }
                    }

                    for(int i=0; i<maxClientsCount; i++) {
                        if(clients[i] != null && clients[i] != this) {
                            clients[i].ots.WriteLine("*** Novo usuario entrou: " + name + " ***");
                        }
                    }
                }

                if (clientSocket.Equals(clients[0].clientSocket))
                {
                    ots.WriteLine("Informe a palavra do jogo:");       
                    palavra = ins.ReadLine();
                    Console.WriteLine("Palavra do jogo: " + clients[0].palavra);
                }

                while(true) {
                    String line = ins.ReadLine();
                    if (line.StartsWith("/quit")) {
                        break;
                    }

                    if(line.StartsWith("/list")) {
                        for(int i=0; i<maxClientsCount; i++) {
                            if(clients[i] != null && clients[i] != this) {
                                ots.WriteLine(clients[i].clientName);
                            }
                        }
                    }

                    if(line.Length < 2) {
                        ots.WriteLine("*** Mensagem muito curta***");
                    }

                    if(line.StartsWith("@")) {
                        String[] words = Regex.Split(line, "\\s");
                        if(words.Length > 1 && words[1] != null) {
                            words[1] = words[1].Trim();
                            if (words[1].Any()) {
                                lock(this) {
                                    for(int i=0; i<maxClientsCount; i++) {
                                        if(clients[i] != null && clients[i] != this
                                            && clients[i].clientName != null
                                            && clients[i].clientName.Equals(words[0])) {
                                            clients[i].ots.WriteLine("<" + name + "> " + words[1]);
                                            this.ots.WriteLine(">" + name + "> " + words[1]);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    } else {
                        lock(this) {
                            if(!checkCommand(line)) {
                                for(int i=0; i<maxClientsCount; i++) {
                                    if(clients[i] != null && clients[i].clientName != null) {
                                        clients[i].ots.WriteLine("<" + name + "> " + line);
                                    }
                                }
                            }
                        }
                    }
                    
                    if (line == clients[0].palavra)
                    {
                        Console.WriteLine("Palavra adivinhada");
                        lock(this) {
                            for(int i=0; i<maxClientsCount; i++) {
                                if(clients[i] != null && clients[i] != null) {
                                    clients[i].ots.WriteLine("*** O usuario " + name + " acertou a palavra " + clients[0].palavra + " e o VENCEDOR ***");
                                }
                            }
                        }
                    }
                }             

                Console.WriteLine("Usuario " + name + " se desconectou");
                lock(this) {
                    for(int i=0; i<maxClientsCount; i++) {
                        if(clients[i] != null && clients[i] != null) {
                            clients[i].ots.WriteLine("*** O usuario " + name + " saiu ***");
                        }
                    }
                }
                ots.WriteLine("*** Ate logo " + name + " ***");

                lock (this) {
                    for(int i=0; i<maxClientsCount; i++) {
                        if(clients[i] == this) {
                            clients[i] = null;
                        }
                    }
                }
                ins.Close();
                ots.Close();
                clientSocket.Close();

            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }

        }
    }
}
