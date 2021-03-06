﻿using System.Net.Sockets;
using System;
using System.Threading;
using simple_tcp_server.Data;
using simple_tcp_server.Hosting;
using System.Threading.Tasks;

namespace simple_tcp_server.Connecting
{
    /// <summary> A tcp client to connect to the tcp server.</summary>
    class Client
    {
        private static Socket socket;
        private static int id = -1;
		private static Thread receivingThread;

        /// <summary> Connect to server.</summary>
        public static void ConnectToServer(string ip = "127.0.0.1", int port = 26950)
        {
            if (IsRunning())
                return;

            Logger.Log("[Client] Initializing socket");
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Logger.Log("[Client] Starting connection loop...");
            int attempts = 0;
            while (!socket.Connected)
            {
                try
                {
                    attempts++;
                    Logger.Log($"[Client] Trying to connect to server {ip}:{port}");
                    socket.Connect(ip, port);
                    Logger.Log($"[Client] Successfully found connection to server!");
                    Logger.Log($"[Client] Started listening to the server!");
                    receivingThread = new Thread(ReceivingThread);
                    receivingThread.Start(socket);
                    Logger.Log($"[Client] Waiting for registration...");
                    return;
                }
                catch (SocketException)
                {
                    if (attempts >= 5)
                    {
                        Logger.Log($"[Client] Failed to connect to server after {attempts} attempts, shutting down...");
                        OnClientAction.UnableToConnectToServer();
                        return;
                    }
                    Logger.Log($"[Client] Unable to connect to server, trying agien... [attempt: {attempts}]");
                }
				catch (Exception)
				{
                    OnClientAction.OnClientException();
				}
            }
        }

        /// <summary> Disconnect from current connection.</summary>
        public static void Disconnect(bool terminateServerIfHost = true)
        {
            if (!IsRunning())
                return;

            Logger.Log("[Client] Socked closed, You are now disconnected!");
            try
            {
                socket.Close();
            }
            catch (Exception) { }
            socket = null;
            id = -1;
            if (Server.IsRunning() && terminateServerIfHost)
				Server.CloseServer();
        }

        /// <summary> Check if client is running/connected.</summary>
        public static bool IsRunning()
        {
            return socket != null;
        }
        /// <summary> Check if client is registred.</summary>
        public static bool IsRegistred()
        {
            return id != -1;
        }

        /// <summary> Get client id, the id that the server gave it.</summary>
        public static int GetId() { return id; }

        /// <summary> Sets client id, the client id shoulde be given by the server!</summary>
        public static void ReciveRegistration(int newId)
        {
            Logger.Log($"[Client] Registration recived, you are now client[{newId}]");
            id = newId;
            OnClientAction.OnClientSuccessfullyConnected();
        }

        /// <summary> Get client socket.</summary>
        public static Socket GetSocket() { return socket; }

        /// <summary> Listening for packets from the server.</summary>
        private static void ReceivingThread(object socket)
        {

            Socket soc = (Socket)socket;
            byte[] buffer;
            int readBytes;

            // Listen for server
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (!Server.SocketConnected(soc))
                        OnClientAction.LostConnectionToServer();
                    Thread.Sleep(1000);
                }
            });

            // Listen for server packet
            while (IsRunning())
            {
                try
                {
                    buffer = new byte[soc.SendBufferSize];
                    readBytes = soc.Receive(buffer);
                    if (readBytes > 0)
                    {
                        PacketHandler.ClientReadData(buffer);
                    }

                }
                catch (Exception)
                {
                    OnClientAction.OnClientException();
                }
            }
        }
    }
}
