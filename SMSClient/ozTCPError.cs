using System;
using System.Collections.Generic;
using System.Text;

namespace SMSClient
{
    class ozTCPError
    {
        private const int startErrorCode = 1800;
        private const int startWarningCode = startErrorCode + 100;

        public const int ERROR_HOSTRESOLVE = startErrorCode + 0;
        public const int ERROR_NOHOSTNAME = startErrorCode + 1;
        public const int ERROR_CONNECTFAIL = startErrorCode + 2;
        public const int ERROR_DISCONNECTFAIL = startErrorCode + 3;
        public const int ERROR_SEND_NOTCONNECTED = startErrorCode + 4;
        public const int ERROR_SEND_START = startErrorCode + 5;
        public const int ERROR_SEND = startErrorCode + 6;
        public const int ERROR_RECEIVE_NOTCONNECTED = startErrorCode + 7;
        public const int ERROR_RECEIVE_START = startErrorCode + 8;
        public const int ERROR_RECEIVE = startErrorCode + 9;
        public const int ERROR_SOCKETINUSE = startErrorCode + 10;

        //Socket level
        public const int ERROR_SOCKET_CONNECT = 8000;
        public const int ERROR_SOCKET_SEND = 8001;
        public const int ERROR_SOCKET_RECEIVE = 8002;
        public const int ERROR_SOCKET_RECEIVE_TIMEOUT = 8003;

        //PDU encoding/decoding level
        public const int ERROR_PDU_UNEXPECTED = 8010;

        //Protocol level
        public const int ERROR_CONNECT_INVALIDUSERORPASS = 8021;
        public const int ERROR_KEEPALIVE = 8022;

        public const int WARN_ALREADYCONNECTED = startWarningCode + 0;
        public const int WARN_READ_NODATA = startWarningCode + 1;
        public const int WARN_READ_NOTENOUGHDATA = startWarningCode + 2;

        public const string ERROR_STR_HOSTRESOLVE = "Cannot resolve hostname: ";
        public const string ERROR_STR_NOHOSTNAME = "No hostname.";
        public const string ERROR_STR_CONNECTFAIL = "Failed to connect to server: ";
        public const string ERROR_STR_DISCONNECTFAIL = "Failed to disconnect from server: ";
        public const string ERROR_STR_SEND_NOTCONNECTED = "Sending data : Not Connected to the server.";
        public const string ERROR_STR_SEND_START = "Cannot start sending data.";
        public const string ERROR_STR_SEND = "There was a problem while sending the data.";
        public const string ERROR_STR_RECEIVE_NOTCONNECTED = "Receiving data : Not Connected to the server.";
        public const string ERROR_STR_RECEIVE_START = "Cannot start receiving data.";
        public const string ERROR_STR_RECEIVE = "There was a problem while receiving data.";
        public const string ERROR_STR_SOCKETINUSE = "Socket is already in use by an other client.";
        public const string ERROR_STR_NO_RESPONSE_RECEIVED = "No response received from server.";

        public const string WARN_STR_ALREADYCONNECTED = "Already connected to the server.";
        public const string WARN_STR_READ_NODATA = "No data to read.";
        public const string WARN_STR_READ_NOTENOUGHDATA = "Not enough data to read.";




    }
}
