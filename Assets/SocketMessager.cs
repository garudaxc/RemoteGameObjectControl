using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;


    //class Logger
    //{
    //    public static void Log(string fmt, params Object[] values)
    //    {
    //        //Console.WriteLine(fmt, values);
            
    //    }


    //    public static void LogError(string fmt, params Object[] values)
    //    {
    //        Console.WriteLine(fmt, values);
    //    }
    //}

    public class SocketMessager
    {
        int Port = 5000;

        Socket listenSocket_;
        Socket socket_;

        Thread receThread_;
        Thread sendThread_;
        ManualResetEvent sendEvent = new ManualResetEvent(false);
        
        static int token__;
        static int GenToken()
        {
            return token__++;
        }

        public bool IsConnected
        {
            get
            {
                return socket_ != null;
            }
        }

        public string ConnectionInfo
        {
            get
            {
                if(socket_ != null)
                {
                    return socket_.RemoteEndPoint.ToString();
                }

                return "";
            }
        }

        [Serializable]
        public class Msg
        {
            public int token;
            public string cmd;
            public byte[] data;
        }

        static BinaryFormatter formatter = new BinaryFormatter();

        List<Msg> sendQueue_ = new List<Msg>();
        List<Msg> receiveQueue_ = new List<Msg>();

        public SocketMessager()
        {
        }

        public void StartTransmitThread()
        {
            sendQueue_.Clear();
            receiveQueue_.Clear();

            receThread_ = new Thread(ReceivingWorker);
            receThread_.Start();

            sendEvent.Reset();
            sendThread_ = new Thread(SendingWorker);
            sendThread_.Start();

            Thread cleanup = new Thread(CleanupWorker);
            cleanup.Start();
        }

        public void CleanupWorker(Object obj)
        {
            receThread_.Join();
            sendThread_.Join();

            socket_.Close();
            socket_ = null;

            Logger.Log("connection closed");
        }

        void ListeningWorker(Object obj)
        {
            Logger.Log("listening thread running id {0}", Thread.CurrentThread.ManagedThreadId);
            while(true)
            {
                try
                {
                    Socket s = listenSocket_.Accept();
                    s.NoDelay = true;

                    Logger.Log("accept a socket form {0}", s.RemoteEndPoint.ToString());

                    if (socket_ != null)
                    {
                        Logger.Log("already have a open channel, closed it");
                        s.Close();
                    }
                    else
                    {
                        socket_ = s;
                        StartTransmitThread();
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e.ToString());
                    break;
                }
            }
            Logger.Log("listening thread stopped");
        }

        void SendingWorker(Object obj)
        {
            Logger.Log("sending worker thread id {0}", Thread.CurrentThread.ManagedThreadId);
            byte[] buffer = new byte[1024 * 1024];

            while (true)
            {
                if (!socket_.Connected)
                {
                    break;
                }

                sendEvent.WaitOne();

                while (sendQueue_.Count > 0)
                {
                    Msg msg;
                    lock (sendQueue_)
                    {
                        msg = sendQueue_[0];
                        sendQueue_.Remove(msg);
                    }

                    MemoryStream stream = new MemoryStream(buffer, 4, buffer.Length - 4);
                    formatter.Serialize(stream, msg);

                    byte[] header = BitConverter.GetBytes((int)stream.Position);
                    Array.Copy(header, 0, buffer, 0, header.Length);

                    int byteToSend = (int)stream.Position + header.Length;

                    try
                    {
                        int count = socket_.Send(buffer, 0, byteToSend, 0);
                        Logger.Log("send data count {0}", count);
                    }
                    catch (SocketException ex)
                    {
                        socket_.Close();
                        Logger.Log(ex.ToString());
                        return;
                    }
                }              

                sendEvent.Reset();
            }

            Logger.Log("sending thread stopped");
        }

        void ReceivingWorker(Object obj)
        {
            Logger.Log("receiving worker thread id {0}", Thread.CurrentThread.ManagedThreadId);
            byte[] buffer = new byte[1024 * 1024];
            int receOffset = 0;

            while (true)
            {
                try
                {
                    int count = socket_.Receive(buffer, receOffset, buffer.Length - receOffset, 0);
                    if (count == 0)
                    {
                        throw new SocketException();
                    }

                    int length = BitConverter.ToInt32(buffer, 0);

                    if ((length + 4) != (count + receOffset))
                    {
                        Logger.Log("pack split found! received size {0} not equal buffer size {1}", count, length + 4);
                        receOffset += count;
                        continue;
                    }
                    receOffset = 0;

                    MemoryStream stream = new MemoryStream(buffer, 4, length);
                    Msg msg = (Msg)formatter.Deserialize(stream);

                    lock (receiveQueue_)
                    {
                        receiveQueue_.Add(msg);
                    }

                    Logger.Log("received data count {0} cmd {1} token {2} data {3}", count, msg.cmd, msg.token, msg.data);
                }
                catch (SocketException ex)
                {
                    socket_.Close();
                    Logger.Log(ex.ToString());
                    sendEvent.Set();
                    break;
                }
            }
            Logger.Log("receive thread stopped");
        }

        public void StartListen(string host)
        {
            IPAddress ip = IPAddress.Parse(host);
            IPEndPoint ipe = new IPEndPoint(ip, Port);
            listenSocket_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            try
            {
                listenSocket_.Bind(ipe);
                listenSocket_.Listen(10);

                Thread listenThread = new Thread(ListeningWorker);
                listenThread.Start();
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            Logger.Log("start listening thread {0}", Thread.CurrentThread.ManagedThreadId);
        }

        public bool Connect(string host)
        {
            IPAddress ip = IPAddress.Parse(host);
            IPEndPoint ipe = new IPEndPoint(ip, Port);
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            s.BeginConnect(ipe, new System.AsyncCallback(ConnectCallback), s);
            return true;
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                socket_ = (Socket)ar.AsyncState;
                socket_.EndConnect(ar);
                socket_.NoDelay = true;

                Logger.Log("connected to server {0}", socket_.RemoteEndPoint.ToString());

                StartTransmitThread();
            }
            catch (SocketException ex)
            {
                Logger.LogError(ex.ToString());
            }
        }


        public void Send(string cmd, byte[] byteData = null)
        {
            if (socket_ == null)
            {
                Logger.LogError("can not send, channel not established!");
                return;
            }
            
            try
            {
                Msg msg = new Msg();
                msg.token = GenToken();
                msg.cmd = cmd;
                msg.data = byteData;
                lock (sendQueue_)
                {
                    sendQueue_.Add(msg);
                }
                sendEvent.Set();
            }
            catch (SocketException ex)
            {
                Logger.LogError(ex.ToString());
            }
        }

        public Msg[] PopMessage()
        {
            Msg[] msgs;
            lock(receiveQueue_)
            {
                msgs = receiveQueue_.ToArray();
                receiveQueue_.Clear();
            }

            return msgs;
        }

        public void Dispose()
        {
            if(socket_ != null)
            {
                socket_.Close();
            }

            if(listenSocket_ != null)
            {
                listenSocket_.Close();
                listenSocket_ = null;
            }
        }

    }