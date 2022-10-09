using System;
using System.Collections.Generic;
using System.Text;

namespace SMSClient
{
    public class ozSMSMessage
    {
        public string messageID;
        public string sender;
        public string receiver;
        public string messagedata;
        public string messagetype;
        public DateTime sentdate;
        public DateTime receiveddate;
        public string serviceprovider;
        public int errorcode;
        public string errormessage;
        private static Random random = new Random();

        private static string randomString(int size, bool lowerCase)
        {
            //Random random = new Random();
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            if (lowerCase)
                return builder.ToString().ToLower();
            return builder.ToString();
        }

        public static string makeMessageID()
        {
            return randomString(8, false);
        }

        public ozSMSMessage()
        {
            messageID = makeMessageID();
            messagetype = "SMS:TEXT";
            sentdate = DateTime.Now;
            receiveddate = DateTime.Now;
        }

        public override string ToString()
        {
            return
            "ID: " + messageID +
            " From: " + sender +
            " To: " + receiver +
            " Sent time: " + sentdate.ToString("yyyy-MM-ddTHH:mm:ss") +
            " Received time: " + receiveddate.ToString("yyyy-MM-ddTHH:mm:ss") +
            " Message type: " + messagetype +
            " Message data: " + messagedata;
        }

    }
}
