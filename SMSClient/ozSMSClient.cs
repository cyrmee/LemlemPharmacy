using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Threading;

namespace SMSClient
{
    #region Event argument classes

    public class ErrorEventArgs : EventArgs
    {
        public int ErrorCode;
        public string ErrorMessage;
    }

    public class DeliveryEventArgs : EventArgs
    {
        public string Messageid;
        public string Senderaddress;
        public string Receiver;
        public string Messagedata;
        public string Messagetype;
        public string Serviceprovider;
        public DateTime Sentdate;
        public DateTime Receiveddate;
    }

    public class DeliveryErrorEventArgs : DeliveryEventArgs
    {
        public int ErrorCode;
        public string ErrorMessage;
    }

    #endregion

    #region Delegates

    public delegate void SimpleEventHandler(object sender, EventArgs e);
    public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);
    public delegate void DeliveryEventHandler(object sender, DeliveryEventArgs e);
    public delegate void DeliveryErrorEventHandler(object sender, DeliveryErrorEventArgs e);

    #endregion

    public class ozSMSClient
    {
        private const char etx = (char)3;
        private const char stx = (char)2;

        //****************************************
        // Messages used for synchonization
        //****************************************
        public const int WM_USER = 1000;
        public const int WM_OZ_STOP = WM_USER + 200;
        public const int WM_TCP_SOCKET_CONNECTED = WM_USER + 201;
        public const int WM_TCP_SOCKET_DISCONNECTED = WM_USER + 202;
        public const int WM_TCP_DATAAVAILABLE = WM_USER + 203;
        public const int WM_CLIENT_CONNECTED = WM_USER + 210;
        public const int WM_CLIENT_MESSAGEDELIVERY_ERROR = WM_USER + 211;
        public const int WM_CLIENT_KEEPALIVE = WM_USER + 212;

        private int transactionID;
        private string fHost;
        private int fPort;
        private string fUsername;
        private string fPassword;
        private int fLastErrorCode;
        private string fLastErrorMessage;
        private bool fConnected;
        private int fSocketTimeout; //in seconds
        private int fKeepalivePeriod; //in seconds
        private bool waitingForPDU = false;

        private string tcpInputBuffer;
        //  socketThread : TSocketReaderThread;

        System.Timers.Timer timerKeepalive;

        //Published events
        /// <summary>
        /// Occurs when unread mail is read or mail is moved by the Owner.
        /// </summary>
        public event SimpleEventHandler OnClientConnected;
        public event SimpleEventHandler OnClientDisconnected;
        public event ErrorEventHandler OnClientConnectionError;
        public event DeliveryEventHandler OnMessageAcceptedForDelivery;
        public event DeliveryEventHandler OnMessageDeliveredToNetwork;
        public event DeliveryEventHandler OnMessageDeliveredToHandset;
        public event DeliveryErrorEventHandler OnMessageDeliveryError;
        public event DeliveryEventHandler OnMessageReceived;

        //Client events
        //  procedure onIncomingPDU(inputPDU : string);
        private ozProcessingThread processingThread;

        //Socket 
        private ozTCPClient tcpClient;

        //Published properties
        public bool Connected
        {
            get { return fConnected; }
            set { connect(value); }
        }

        public string Host
        {
            get { return fHost; }
            set { fHost = value; }
        }

        public int Port
        {
            get { return fPort; }
            set { fPort = value; }
        }

        public string Username
        {
            get { return fUsername; }
            set { fUsername = value; }
        }

        public string Password
        {
            get { return fPassword; }
            set { fPassword = value; }
        }

        public int KeepalivePeriod
        {
            get { return fKeepalivePeriod; }
            set { fKeepalivePeriod = value; }
        }

        public int SocketTimeout
        {
            get { return fSocketTimeout; }
            set { fSocketTimeout = value; }
        }

        public ozSMSClient()
        {
            fConnected = false;
            fPort = 9500;
            fHost = "127.0.0.1";
            fUsername = "admin";
            fPassword = "abc123";
            fSocketTimeout = 10;
            transactionID = -1;

            fKeepalivePeriod = 60;
         
        }

        ~ozSMSClient()
        {
           
        }

        void timerKeepalive_Elapsed(object sender, ElapsedEventArgs e)
        {
            processingThread.postMessage(this, WM_CLIENT_KEEPALIVE);
        }

        private ManualResetEvent connectComplete = new ManualResetEvent(false);

        private void connect(bool v)
        {
            if (v)
            {
                doConnect();
            }
            else
            {
                disconnect();
            }
        }

        private void doConnect()
        {
            processingThread = new ozProcessingThread();
            processingThread.owner = this;
            processingThread.startAndWait();

            timerKeepalive = new System.Timers.Timer();
            timerKeepalive.Interval = fKeepalivePeriod * 1000;
            timerKeepalive.Elapsed += new ElapsedEventHandler(timerKeepalive_Elapsed);
            timerKeepalive.Stop();

            transactionID = -1;

            //Create socket
            tcpClient = new ozTCPClient();
            tcpClient.OnDataAvailable = onTCPDataAvailable;
            tcpClient.OnDisconnected = onTCPClientDisConnected;
            tcpClient.OnError = onTCPClientError;
            tcpClient.OnConnected = onTCPClientConnected;
            tcpClient.Host = fHost;
            tcpClient.Port = fPort;
            tcpInputBuffer = "";

            try
            {
                connectComplete.Reset();
                fConnected = tcpClient.connect();
                if (fConnected)
                {
                    if (!connectComplete.WaitOne(8000, false))
                    {
                        fLastErrorCode = ozTCPError.ERROR_SOCKET_CONNECT;
                        fLastErrorMessage = ozTCPError.ERROR_STR_CONNECTFAIL;
                        doOnClientConnectionError(fLastErrorCode, fLastErrorMessage);
                    }
                }
            }
            catch (Exception e)
            {
                fLastErrorCode = ozTCPError.ERROR_SOCKET_CONNECT;
                fLastErrorMessage = e.Message;
                doOnClientConnectionError(fLastErrorCode, fLastErrorMessage);
            }

            if (fConnected) { 
                versioncheck(); 
            }
            if (fConnected) { 
                login(); 
            }
            if (fConnected)
            {
                processingThread.postMessage(this, WM_CLIENT_CONNECTED);
            }
            if (fConnected)
            {
                processingThread.postMessage(this, WM_TCP_DATAAVAILABLE);
            }

            if (fConnected)
            {
                //Create keepalive timer
                timerKeepalive.Interval = fKeepalivePeriod * 1000;
                timerKeepalive.Start();
            }

           // connectComplete.Set();
            //return fConnected;
        }

        private void versioncheck()
        {
            string versioncheckpdu;
            string versioncheckresppdu;
            ozHashtable pduhash = new ozHashtable();
            string action;
            string errorcodestr;

            //Version check PDU
            versioncheckpdu = stx +
            "transactionid=" + getNextTransID() + "&" +
            "action=getversion" +
            "&responseformat=urlencoded" +
            etx;

            //Send version check PDU and process response PDU
            waitingForPDU = true;
            pduwaitreceived.Reset();
            if (!sendPDU(versioncheckpdu))
            {
                doOnClientConnectionError(fLastErrorCode, fLastErrorMessage);
            }
            else if (!waitforPDU(out versioncheckresppdu, fSocketTimeout))
            {
                fLastErrorCode = ozTCPError.ERROR_SOCKET_RECEIVE_TIMEOUT;
                fLastErrorMessage = ozTCPError.ERROR_STR_NO_RESPONSE_RECEIVED;
                doOnClientConnectionError(fLastErrorCode, fLastErrorMessage);
            }
            else
            {
                pduhash.fromUnparsedParams(versioncheckresppdu);
                action = (string)pduhash["action"];
                if (action == "error")
                {
                    errorcodestr = (string)pduhash["errorcode"];
                    fLastErrorCode = Int32.Parse(errorcodestr);
                    fLastErrorMessage = (string)pduhash["errormessage"];
                    doOnClientConnectionError(fLastErrorCode, fLastErrorMessage);
                }
                else if (action == "getversion")
                {
                    //all protocol versions are supported
                }
                else
                {
                    fLastErrorCode = ozTCPError.ERROR_PDU_UNEXPECTED;
                    fLastErrorMessage = "Unexpected response received from server. " + visualize(versioncheckresppdu);
                    doOnClientConnectionError(fLastErrorCode, fLastErrorMessage);
                }
            }
        }

        private void login()
        {
            string loginresppdu;
            ozHashtable pduhash = new ozHashtable();
            string action;
            string errorcodestr;

            //Build login PDU
            string loginpdu = stx +
               "transactionid=" + getNextTransID() + "&" +
               "action=login" +
               "&username=" + ozHTTPUtility.UrlEncode(fUsername) +
               "&password=" + ozHTTPUtility.UrlEncode(fPassword) +
               "&responseformat=urlencoded" + etx;

            //Send login PDU and process response PDU
            waitingForPDU = true;
            pduwaitreceived.Reset();
            if (!sendPDU(loginpdu))
            {
                doOnClientConnectionError(fLastErrorCode, fLastErrorMessage);
            }
            else if (!waitforPDU(out loginresppdu, fSocketTimeout))
            {
                fLastErrorCode = ozTCPError.ERROR_SOCKET_RECEIVE_TIMEOUT;
                fLastErrorMessage = ozTCPError.ERROR_STR_NO_RESPONSE_RECEIVED;
                doOnClientConnectionError(fLastErrorCode, fLastErrorMessage);
            }
            else
            {
                pduhash.fromUnparsedParams(loginresppdu);
                action = (string)pduhash["action"];
                if (action == "error")
                {
                    errorcodestr = (string)pduhash["errorcode"];
                    fLastErrorCode = Int32.Parse(errorcodestr);
                    fLastErrorMessage = (string)pduhash["errormessage"];
                    doOnClientConnectionError(fLastErrorCode, fLastErrorMessage);
                }
            }
        }

        private void disconnect()
        {
            if ((timerKeepalive != null) && (timerKeepalive.Enabled)) {
                timerKeepalive.Stop();
            }
            try
            {
                if (tcpClient.Connected) {
                    tcpClient.disconnect(); 
                }
            }
            catch { }
            if ((processingThread!=null) && (!processingThread.isStopped())) {
                processingThread.stop();
            }
            fConnected = false;
        }



        private string visualize(string s)
        {
            return s.Replace(stx.ToString(), "<stx>").Replace(etx.ToString(), "<etx>");
        }

        private string getNextTransID()
        {
            transactionID += 2;
            if (transactionID > 32768) { transactionID = 1; }
            return (transactionID.ToString());
        }

        private void onIncomingPDU(string inputPDU)
        {
            ozHashtable pduhash = new ozHashtable();
            string action = "";
            string errorcodestr = "";
            ozSMSMessage sms;
            string transaction;
            string deliverystatus;
            string ackPDU;

            try
            {
                pduhash.fromUnparsedParams(inputPDU);
                action = (string)pduhash["action"];
                if (action == "error")
                {
                    errorcodestr = (string)pduhash["errorcode"];
                    fLastErrorCode = Int32.Parse(errorcodestr);
                    fLastErrorMessage = (string)pduhash["errormessage"];
                    doOnClientConnectionError(fLastErrorCode, fLastErrorMessage);
                }
                else if (action == "sendmessage")
                {
                    //Sendmessage response
                    sms = smsFromPdu(pduhash, "acceptreport");
                    if (sms.errorcode > 0)
                    {
                        doOnMessageDeliveryError(sms);
                    }
                    else
                    {
                        doOnMessageAcceptedForDelivery(sms);
                    }
                }
                else if (action == "deliveryreport")
                {
                    //Acknowledge incoming delivery report
                    transaction = (string)pduhash["transactionid"];
                    ackPDU = stx +
                    "transactionid=" + transaction + "&" +
                    "action=deliveryreport" +
                    "&acceptreport.statuscode=0" +
                    "&acceptreport.statusmessage=acknowledged" +
                    etx;

                    if (!sendPDU(ackPDU))
                    {
                        doOnClientConnectionError(fLastErrorCode, fLastErrorMessage);
                    }
                    else
                    {
                        //Incoming deliver report
                        sms = smsFromPdu(pduhash, "deliveryreport");
                        sms.sentdate = DateTime.Parse((string)pduhash["deliveryreport.deliveredtonetworkdate"]);
                        sms.receiveddate = DateTime.Parse((string)pduhash["deliveryreport.deliveredtohandsetdate"]);
                        deliverystatus = (string)pduhash["deliveryreport.statuscode"];
                        if (deliverystatus == "0")
                        {
                            doOnMessageDeliveredToNetwork(sms);
                        }
                        else if (deliverystatus == "1")
                        {
                            doOnMessageDeliveredToHandset(sms);
                        }
                        else
                        {
                            doOnMessageDeliveryError(sms);
                        }

                    };
                }
                else if (action == "messagereceived")
                {
                    //Acknowledge incoming delivery report
                    transaction = (string)pduhash["transactionid"];
                    ackPDU = stx +
                    "transactionid=" + transaction + "&" +
                    "action=messagereceived" +
                    "&acceptreport.statuscode=0" +
                    "&acceptreport.statusmessage=acknowledged" +
                    etx;

                    if (!sendPDU(ackPDU))
                    {
                        doOnClientConnectionError(fLastErrorCode, fLastErrorMessage);
                    }
                    else
                    {
                        //Incoming deliver report
                        sms = smsFromPdu(pduhash, "message");
                        sms.sentdate = DateTime.Parse((string)pduhash["message.sentdate"]);
                        sms.receiveddate = DateTime.Parse((string)pduhash["message.receiveddate"]);
                        doOnMessageReceived(sms);
                    };
                }
                else if (action == "keepalive")
                {
                    //do nothing
                }
                else
                {
                    fLastErrorCode = ozTCPError.ERROR_PDU_UNEXPECTED;
                    fLastErrorMessage = "Unexpected data received from server. " + visualize(inputPDU);
                    doOnClientConnectionError(fLastErrorCode, fLastErrorMessage);
                };
            }
            catch
            {

            };
        }

        //***********************************************************************
        //Socket control methods
        //***********************************************************************

        private bool sendPDU(string pdu)
        {
            bool ret;
            try
            {
                tcpClient.sendString(pdu);
                ret = true;
            }
            catch (Exception e)
            {
                fLastErrorCode = ozTCPError.ERROR_SOCKET_SEND;
                fLastErrorMessage = e.Message;
                disconnect();
                ret = false;
            }
            return ret;
        }


        ManualResetEvent pduwaitreceived = new ManualResetEvent(false);
        string pduwaitincoming = "";
        private bool waitforPDU(out string pduReceived, int timeout)
        {
            pduReceived = "";

            WaitHandle.WaitAll(new WaitHandle[] {pduwaitreceived},timeout*1000,false);

            waitingForPDU = false;

            if (pduwaitincoming.Length>0)
            {
                pduReceived = pduwaitincoming;
                pduwaitincoming = "";
                return true;
            } else {
                return false;
            }
        }

        private bool readPDU(string inputData, out string pduReceived)
        {

            bool moremessages = false;
            pduReceived = "";
            tcpInputBuffer += inputData;

            int p1 = tcpInputBuffer.IndexOf(etx);
            if (p1 > 1)
            {
                pduReceived = tcpInputBuffer.Substring(1, p1 - 2);
                tcpInputBuffer = tcpInputBuffer.Substring(p1 + 1);

                if (tcpInputBuffer.IndexOf(etx) > 0) { moremessages = true; }
            }

            return moremessages;
        }

        private void onTCPClientConnected()
        {
            connectComplete.Set();
            processingThread.postMessage(this, WM_TCP_SOCKET_CONNECTED);
        }

        private void onTCPClientDisConnected()
        {
            connectComplete.Set();
            processingThread.postMessage(this, WM_TCP_SOCKET_DISCONNECTED);
        }

        private void onTCPClientError(int errorCode, string errorMessage)
        {
            //Handle socket errors
        }

        private void onTCPDataAvailable(string data)
        {
           processingThread.postMessage(this, WM_TCP_DATAAVAILABLE, data);
        }

        public bool onMessage(ozThreadMessage threadMessage)
        {
            bool ret = true;
            switch (threadMessage.messageId)
            {
                case WM_OZ_STOP: break;
                case WM_TCP_SOCKET_CONNECTED: break;
                case WM_CLIENT_CONNECTED:
                    {
                        doOnClientConnected();
                        break;
                    }

                case WM_TCP_SOCKET_DISCONNECTED:
                    {
                        if (fConnected) { doOnClientDisconnected(); };
                        break;
                    }
                case WM_TCP_DATAAVAILABLE:
                    {
                        bool moredata;
                        string inputPDU;
                        string inputData = (string)threadMessage.messageParam1;
                        do
                        {
                            moredata = readPDU(inputData, out inputPDU);
                            inputData = String.Empty;
                            if (inputPDU.Length > 0)
                            {
                                if (waitingForPDU)
                                {
                                    pduwaitincoming = inputPDU;
                                    pduwaitreceived.Set();
                                }
                                else
                                {
                                    onIncomingPDU(inputPDU);
                                }
                            }
                        } while (moredata);
                        break;
                    }
                case WM_CLIENT_MESSAGEDELIVERY_ERROR:
                    {
                        doOnMessageDeliveryError((ozSMSMessage)threadMessage.messageParam1);
                        break;
                    }
                case WM_CLIENT_KEEPALIVE:
                    {
                        sendKeepalive();
                        break;
                    }

                default:
                    ret = false;
                    break;
            }
            return ret;
        }


        //***********************************************************************
        //Event handler wrappers
        //***********************************************************************

        private void doOnClientConnected()
        {
            if (OnClientConnected != null)
            {
                try
                {
                    ThreadStart starter = delegate { OnClientConnected(this, new EventArgs()); };
                    new Thread(starter).Start();
                    
                    //OnClientConnected(this, new EventArgs());
                }
                catch { }
            }
        }

        private void doOnClientDisconnected()
        {
            fConnected = false;
            if (OnClientDisconnected != null)
            {
                try
                {
                    //ThreadStart starter = delegate { OnClientDisconnected(this, new EventArgs()); };
                    //new Thread(starter).Start();

                    OnClientDisconnected(this, new EventArgs());
                }
                catch { }
            }
        }

        private void doOnMessageAcceptedForDelivery(ozSMSMessage sms)
        {
            if (OnMessageAcceptedForDelivery != null)
            {
                try
                {
                    DeliveryEventArgs ea = new DeliveryEventArgs();
                    ea.Messageid = sms.messageID;
                    ea.Receiver = sms.receiver;
                    ea.Senderaddress = sms.sender;
                    ea.Sentdate = sms.sentdate;
                    ea.Receiveddate = sms.receiveddate;
                    ea.Messagedata = sms.messagedata;
                    ea.Messagetype = sms.messagetype;
                    ea.Serviceprovider = sms.serviceprovider;

                    ThreadStart starter = delegate { OnMessageAcceptedForDelivery(this, ea); };
                    new Thread(starter).Start();

                    //OnMessageAcceptedForDelivery(this, ea);
                }
                catch { }
            }
        }

        private void doOnMessageDeliveredToNetwork(ozSMSMessage sms)
        {
            if (OnMessageDeliveredToNetwork != null)
            {
                try
                {
                    DeliveryEventArgs ea = new DeliveryEventArgs();
                    ea.Messageid = sms.messageID;
                    ea.Receiver = sms.receiver;
                    ea.Senderaddress = sms.sender;
                    ea.Sentdate = sms.sentdate;
                    ea.Receiveddate = sms.receiveddate;
                    ea.Messagedata = sms.messagedata;
                    ea.Messagetype = sms.messagetype;
                    ea.Serviceprovider = sms.serviceprovider;

                    ThreadStart starter = delegate { OnMessageDeliveredToNetwork(this, ea); };
                    new Thread(starter).Start();

                    //OnMessageDeliveredToNetwork(this, ea);
                }
                catch { }
            }
        }

        private void doOnMessageDeliveredToHandset(ozSMSMessage sms)
        {
            if (OnMessageDeliveredToHandset != null)
            {
                try
                {
                    DeliveryEventArgs ea = new DeliveryEventArgs();
                    ea.Messageid = sms.messageID;
                    ea.Receiver = sms.receiver;
                    ea.Senderaddress = sms.sender;
                    ea.Sentdate = sms.sentdate;
                    ea.Receiveddate = sms.receiveddate;
                    ea.Messagedata = sms.messagedata;
                    ea.Messagetype = sms.messagetype;
                    ea.Serviceprovider = sms.serviceprovider;

                    ThreadStart starter = delegate { OnMessageDeliveredToHandset(this, ea); };
                    new Thread(starter).Start();

                    //OnMessageDeliveredToHandset(this, ea);
                }
                catch { }
            }
        }

        private void doOnMessageDeliveryError(ozSMSMessage sms)
        {
            if (OnMessageDeliveryError != null)
            {
                try
                {
                    DeliveryErrorEventArgs ea = new DeliveryErrorEventArgs();
                    ea.Messageid = sms.messageID;
                    ea.Receiver = sms.receiver;
                    ea.Senderaddress = sms.sender;
                    ea.Sentdate = sms.sentdate;
                    ea.Receiveddate = sms.receiveddate;
                    ea.Messagedata = sms.messagedata;
                    ea.Messagetype = sms.messagetype;
                    ea.Serviceprovider = sms.serviceprovider;
                    ea.ErrorCode = sms.errorcode;
                    ea.ErrorMessage = sms.errormessage;

                    ThreadStart starter = delegate { OnMessageDeliveryError(this, ea); };
                    new Thread(starter).Start();

                    //OnMessageDeliveryError(this, ea);
                }
                catch { }
            }
        }

        private void doOnMessageReceived(ozSMSMessage sms)
        {
            if (OnMessageReceived != null)
            {
                try
                {
                    DeliveryEventArgs ea = new DeliveryEventArgs();
                    ea.Messageid = sms.messageID;
                    ea.Receiver = sms.receiver;
                    ea.Senderaddress = sms.sender;
                    ea.Sentdate = sms.sentdate;
                    ea.Receiveddate = sms.receiveddate;
                    ea.Messagedata = sms.messagedata;
                    ea.Messagetype = sms.messagetype;
                    ea.Serviceprovider = sms.serviceprovider;

                    ThreadStart starter = delegate { OnMessageReceived(this, ea); };
                    new Thread(starter).Start();

                    //OnMessageReceived(this, ea);
                }
                catch { }
            }
        }

        private void doOnClientConnectionError(int lastErrorCode, string lastErrorMessage)
        {
            if (OnClientConnectionError != null)
            {
                try
                {
                    ErrorEventArgs ea = new ErrorEventArgs();
                    ea.ErrorCode = lastErrorCode;
                    ea.ErrorMessage = lastErrorMessage;

                    ThreadStart starter = delegate { OnClientConnectionError(this, ea); };
                    new Thread(starter).Start();

                    //OnClientConnectionError(this, ea);
                    disconnect();
                }
                catch { }
            }
        }


        private ozSMSMessage smsFromPdu(ozHashtable pduhash, string fieldprefix)
        {
            ozSMSMessage sms = new ozSMSMessage();
            sms.messageID = (string)pduhash[fieldprefix + ".messageid"];
            sms.sender = (string)pduhash[fieldprefix + ".originator"];
            sms.receiver = (string)pduhash[fieldprefix + ".recipient"];
            sms.messagedata = (string)pduhash[fieldprefix + ".messagedata"];
            sms.messagetype = (string)pduhash[fieldprefix + ".messagetype"];
            sms.serviceprovider = (string)pduhash[fieldprefix + ".serviceprovider"];
            sms.errorcode = Int32.Parse((string)pduhash[fieldprefix + ".statuscode"]);
            sms.errormessage = (string)pduhash[fieldprefix + ".statusmessage"];
            return sms;
        }

        private void sendKeepalive()
        {
            string keepalivePDU = stx +
            "transactionid=" + getNextTransID() + "&" +
            "action=keepalive" +
            "&responseformat=urlencoded" +
            etx;

            if (!sendPDU(keepalivePDU))
            {
                fLastErrorCode = ozTCPError.ERROR_KEEPALIVE;
                fLastErrorMessage = "Keepalive failed.";
                doOnClientConnectionError(fLastErrorCode, fLastErrorMessage);
            }
        }

        public string getLastErrorMessage() { return fLastErrorMessage; }

        public int getLastErrorCode() { return fLastErrorCode; }


        public string sendMessage(string receiver, string messagedata, string optionalParameters)
        {
            var optionalParameterList = new Dictionary<string, string>();

            foreach (var parameter in optionalParameters.Split('&'))
            {
                var parts = parameter.Split('=');
                if(parts.Length == 2 && !optionalParameterList.ContainsKey(parts[0]))
                {
                    optionalParameterList.Add(parts[0], parts[1]);
                }
            }


            return sendMessageEx("", receiver, messagedata, "SMS:TEXT", "", optionalParameterList);
        }

        public string sendMessage(string receiver, string messagedata) 
        {
            return sendMessageEx("",receiver,messagedata,"SMS:TEXT","", null);
        }

        public string sendMessageEx(
            string sender,
            string receiver,
            string messagedata,
            string messagetype,
            string serviceprovider,
            Dictionary<string,string> optionalParameters
            )
        {
            string messageID = ozSMSMessage.makeMessageID();

            var sb = new StringBuilder();

            if (optionalParameters != null)
            {
                foreach (var parameter in optionalParameters)
                {
                    sb.Append("&");
                    sb.Append(ozHTTPUtility.UrlEncode(parameter.Key));
                    sb.Append("=");
                    sb.Append(ozHTTPUtility.UrlEncode(parameter.Value));
                }
            }

            string submitPDU = stx +
                               "transactionid=" + getNextTransID() + "&" +
                               "action=sendmessage" +
                               "&responseformat=urlencoded" +
                               "&originator=" + ozHTTPUtility.UrlEncode(sender) +
                               "&recipient=" + ozHTTPUtility.UrlEncode(receiver) +
                               "&messagetype=" + ozHTTPUtility.UrlEncode(messagetype) +
                               "&messagedata=" + ozHTTPUtility.UrlEncode(messagedata) +
                               "&messageid=" + ozHTTPUtility.UrlEncode(messageID) +
                               "&serviceprovider=" + ozHTTPUtility.UrlEncode(serviceprovider) +
                               sb + etx;

            if (!sendPDU(submitPDU))
            {
                ozSMSMessage sms = new ozSMSMessage();
                sms.messageID = messageID;
                sms.sender = sender;
                sms.receiver = receiver;
                sms.messagedata = messagedata;
                sms.messagetype = messagetype;
                sms.serviceprovider = serviceprovider;
                sms.errorcode = ozTCPError.ERROR_SOCKET_SEND;
                sms.errormessage = "Client disconnected from SMS gateway during message submission.";
                processingThread.postMessage(this,WM_CLIENT_MESSAGEDELIVERY_ERROR, sms);
            }

            return (messageID);
        }


    }
}
