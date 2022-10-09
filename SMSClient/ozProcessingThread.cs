using System;
using System.Collections.Generic;
using System.Text;

namespace SMSClient
{
    public class ozProcessingThread : ozThreadModule 
    {
        public ozSMSClient owner;

        public override void onMessage(ozThreadMessage threadMessage)
        {
            if (!owner.onMessage(threadMessage))
            {
                base.onMessage(threadMessage);
            }
        }
    }
}
