using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SMSClient
{
    public class ozThreadMessage
    {
        public Object sender = null;
        public int messageId = 0;
        public Object messageParam1 = null;
        public Object messageParam2 = null;
        public ManualResetEvent messageProcessed = null;//new ManualResetEvent(false);

        //constructor

        public ozThreadMessage(Object inSender, int inMessageId, Object inParam1, Object inParam2)
        {
            sender = inSender;
            messageId = inMessageId;
            messageParam1 = inParam1;
            messageParam2 = inParam2;
        }

    }
}
