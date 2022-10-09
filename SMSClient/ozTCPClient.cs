using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace SMSClient
{
    public delegate void OnDataAvailableType(string data);
    public delegate void OnDataAvailableSocketType(string data, Socket source);
    public delegate void OnErrorType(int errorCode, string errorMessage);
    public delegate void OnConnectedType();
    public delegate void OnConnectedTypeEx(ozTCPClient source);
    public delegate void OnDisconnectedType();

    public class ozTCPClient
    {

        private const int BufferSize = 1024;
        private byte[] buffer = new byte[BufferSize];
        private StringBuilder sb = new StringBuilder();

        public OnDataAvailableType OnDataAvailable;
        public OnDataAvailableSocketType OnDataAvailableSocket;
        public OnErrorType OnError;
        public OnConnectedType OnConnected;
        public OnConnectedTypeEx OnConnectedEx;
        public OnDisconnectedType OnDisconnected;

        private Socket socketForServer;
        private IPEndPoint iep;

        private string host;
        private int port;
        private bool connected = false;
        private bool moreDataAvail = false;
        private EndPoint connectedLocalEndPoint;
        private EndPoint desiredLocalEndPoint = null;

        public EndPoint LocalEndPoint
        {
            get { return connectedLocalEndPoint; }
            set { desiredLocalEndPoint = value; }
        }

        public static IPAddress GetIPAddressFromString(string host)
        {
            IPAddress ipaddress;
            if (IPAddress.TryParse(host, out ipaddress))
            {
                return ipaddress;
            }
            else
            {
                return Dns.GetHostEntry(host).AddressList[0];
            }
        }

        public void BindToAddress(string host, int port)
        {
            try
            {
                desiredLocalEndPoint = new IPEndPoint(GetIPAddressFromString(host), port);
            }
            catch (Exception e)
            {
                if (e is System.Net.Sockets.SocketException)
                {
                    SocketException socex = (e as SocketException);
                    if (socex.ErrorCode == 11001)
                    {
                        if (OnError != null)
                        {
                            OnError(ozTCPError.ERROR_HOSTRESOLVE, ozTCPError.ERROR_STR_HOSTRESOLVE + host + ":" + port.ToString() + " (" + e.Message + ")");
                        }
                    }
                }
                desiredLocalEndPoint = null;
            }
        }

        public bool MoreDataAvail
        {
            get { return moreDataAvail; }
        }

        public bool Connected
        {
            get { return connected; }
        }

        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        public string Host
        {
            get { return host; }
            set { host = value; }
        }


        public bool connect()
        {
            if (host == "")
            {
                if (OnError != null)
                {
                    OnError(ozTCPError.ERROR_NOHOSTNAME, ozTCPError.ERROR_STR_NOHOSTNAME);
                }
                connected = false;
                return false;
            }

            if ((socketForServer != null) && (socketForServer.Connected == true))
            {
                if (OnError != null)
                {
                    OnError(ozTCPError.WARN_ALREADYCONNECTED, ozTCPError.WARN_STR_ALREADYCONNECTED);
                }
                connected = true;
                return false;
            }

            try
            {
                socketForServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if (desiredLocalEndPoint != null) socketForServer.Bind(desiredLocalEndPoint);
                iep = new IPEndPoint(GetIPAddressFromString(host), port);
                socketForServer.BeginConnect(iep, new AsyncCallback(callbackConnected), socketForServer);
            }
            catch (Exception e)
            {
                if (e is System.Net.Sockets.SocketException)
                {
                    SocketException socex = (e as SocketException);
                    if (socex.ErrorCode == 11001)
                    {
                        if (OnError != null)
                        {
                            OnError(ozTCPError.ERROR_HOSTRESOLVE, ozTCPError.ERROR_STR_HOSTRESOLVE + host + ":" + port.ToString() + " (" + e.Message + ")");
                        }
                    }
                    if (socex.ErrorCode == 10048)
                    {
                        if (OnError != null)
                        {
                            string desLEP = "";
                            if (desiredLocalEndPoint != null)
                            {
                                desLEP = desiredLocalEndPoint.ToString();
                            }
                            OnError(ozTCPError.ERROR_SOCKETINUSE, ozTCPError.ERROR_STR_SOCKETINUSE + " " + desLEP + " (" + e.Message + ")");
                        }
                    }

                }
                else
                {
                    if (OnError != null)
                    {
                        OnError(ozTCPError.ERROR_CONNECTFAIL, ozTCPError.ERROR_STR_CONNECTFAIL + host + ":" + port.ToString() + " (" + e.ToString() + ")");
                    }
                }
                CloseSocket();
                return false;
            }

            return true;
        }

        void callbackConnected(IAsyncResult ar)
        {
            try
            {
                Socket clientSoc = (Socket)ar.AsyncState;
                if (socketForServer == null) return;
                clientSoc.EndConnect(ar);
                if (clientSoc.Connected)
                {
                    connected = true;
                    connectedLocalEndPoint = clientSoc.LocalEndPoint;
                    if (OnConnected != null)
                    {
                        OnConnected();
                    }
                    if (OnConnectedEx != null)
                    {
                        OnConnectedEx(this);
                    }
                    ReceiveData();
                }
                else
                {
                    if (OnError != null)
                    {
                        OnError(ozTCPError.ERROR_CONNECTFAIL, ozTCPError.ERROR_STR_CONNECTFAIL + host + ":" + port.ToString());
                    }
                }
            }
            catch (ObjectDisposedException ode)
            {
                if (socketForServer == null) return;
                else throw (ode);
            }
            catch (Exception e)
            {
                if (OnError != null)
                {
                    OnError(ozTCPError.ERROR_CONNECTFAIL, ozTCPError.ERROR_STR_CONNECTFAIL + host + ":" + port.ToString() + " (" + e.Message + ")");
                }
                CloseSocket();
            }
        }

        private void CloseSocket()
        {
            try
            {
                if (socketForServer != null)
                {
                    if (socketForServer.Connected)
                    {
                        socketForServer.Shutdown(SocketShutdown.Receive);
                    }
                    socketForServer.Close();
                    socketForServer = null;
                }
            }
            catch (Exception e)
            {
                if (OnError != null)
                {
                    OnError(ozTCPError.ERROR_DISCONNECTFAIL, ozTCPError.ERROR_STR_DISCONNECTFAIL + host + ":" + port.ToString() + " (" + e.Message + ")");
                }
            }
            finally
            {
                connected = false;
                connectedLocalEndPoint = null;
                if (OnDisconnected != null)
                {
                    OnDisconnected();
                }
            }

        }

        public void sendString(string data)
        {
            SendData(data);
        }

        public static void stringToByteArray(string theString, ref byte[] data, int offset)
        {
            for (int i = 0; i < theString.Length; ++i)
            {
                data[i] = (byte)(theString[i] & 0xFF);
            }
        }

        public static string byteArrayToString(byte[] data, int count)
        {
            StringBuilder ret = new StringBuilder("");
            for (int x = 0; x < count; x++)
            {
                ret.Append((char)data[x]);
            }
            return ret.ToString();
        }

        public void SendData(string data)
        {
            try
            {
                if ((socketForServer != null) && (socketForServer.Connected))
                {
                    byte[] byteData = new byte[data.Length];
                    stringToByteArray(data, ref byteData, 0);

                    socketForServer.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(callbackSend), socketForServer);
                }
                else
                {
                    if (OnError != null)
                    {
                        OnError(ozTCPError.ERROR_SEND_NOTCONNECTED, ozTCPError.ERROR_STR_SEND_NOTCONNECTED);
                    }
                    CloseSocket();
                }
            }
            catch (Exception e)
            {
                if (OnError != null)
                {
                    OnError(ozTCPError.ERROR_SEND_START, ozTCPError.ERROR_STR_SEND_START + " (" + e.Message + ")");
                }
                CloseSocket();
            }

        }

        private void callbackSend(IAsyncResult ar)
        {
            try
            {
                Socket clientSoc = (Socket)ar.AsyncState;
                if (clientSoc.Connected)
                {
                    int byteSend = clientSoc.EndSend(ar);
                }
                else
                {
                    if (OnError != null)
                    {
                        OnError(ozTCPError.ERROR_SEND, ozTCPError.ERROR_STR_SEND + " (Disconnected during sending.)");
                    }
                }
            }
            catch (Exception e)
            {
                if (OnError != null)
                {
                    OnError(ozTCPError.ERROR_SEND, ozTCPError.ERROR_STR_SEND + " (" + e.Message + ")");
                }
                CloseSocket();
            }
        }

        public void ReceiveData()
        {
            if ((socketForServer != null) && (socketForServer.Connected))
            {
                try
                {
                    socketForServer.BeginReceive(buffer, 0, BufferSize, SocketFlags.None, new AsyncCallback(callbackReceive), socketForServer);
                }
                catch (Exception e)
                {
                    if (OnError != null)
                    {
                        OnError(ozTCPError.ERROR_RECEIVE_START, ozTCPError.ERROR_STR_RECEIVE_START + " (" + e.Message + ")");
                    }
                    CloseSocket();
                }
            }
            else
            {
                if (OnError != null)
                {
                    OnError(ozTCPError.ERROR_RECEIVE_NOTCONNECTED, ozTCPError.ERROR_STR_RECEIVE_NOTCONNECTED);
                }
                CloseSocket();
            }
        }

        private void callbackReceive(IAsyncResult ar)
        {
            try
            {
                Socket clientSoc = (Socket)ar.AsyncState;
                if ((clientSoc != null) && (clientSoc.Connected))
                {
                    int byteRead = clientSoc.EndReceive(ar);
                    if (byteRead > 0)
                    {
                        string input = byteArrayToString(buffer, byteRead);
                        sb.Append(input);
                        moreDataAvail = true;
                        if (OnDataAvailable != null)
                        {
                            OnDataAvailable(sb.ToString());
                            sb.Remove(0, sb.Length);
                        }
                        if (OnDataAvailableSocket != null)
                        {
                            OnDataAvailableSocket(sb.ToString(), clientSoc);
                            sb.Remove(0, sb.Length);
                        }
                        ReceiveData();
                    }
                    else CloseSocket();
                }
            }
            catch (Exception e)
            {
                if (OnError != null)
                {
                    OnError(ozTCPError.ERROR_RECEIVE, ozTCPError.ERROR_STR_RECEIVE + " (" + e.Message + ")");
                }
                CloseSocket();
            }
        }

        public void done()
        {
            CloseSocket();
        }

        public void disconnect()
        {
            CloseSocket();
        }
    }
}
