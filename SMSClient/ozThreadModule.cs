using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;

namespace SMSClient
{
    public delegate void onThreadModuleException(string errorMessage);
    
    public class ozThreadModule
    {
        protected onThreadModuleException localExceptionHandler;
        public static onThreadModuleException globalExceptionHandler;
        private bool started = false;
        private object startedLock = new object();
        protected object threadIDLock = new object();
        protected int firstThreadCallingGetMessage = -1;
        public int lastMessage = 0;

        private ArrayList messageQueue = new ArrayList();
        public ManualResetEvent messageAvailable = new ManualResetEvent(false);
        protected bool stopped = true;
        public ManualResetEvent moduleStarted = new ManualResetEvent(false);
        public ManualResetEvent moduleStopped = new ManualResetEvent(false);

        public Thread thread;
        
        public const int WM_OZ_STOP = 1000;

        public virtual void onMessage(ozThreadMessage threadMessage)
        {
             switch (threadMessage.messageId)
             {
                 case WM_OZ_STOP :
                     stopped = true;
                     break;
                 default:
                     //Eat message
                     break;
             }
        }

        public bool peekMessages()
        {
            int cn = 0;
            lock (messageQueue)
            {
                cn = messageQueue.Count;
            }

            if (cn > 0)
            {
                return getMessages();
            } else {
                return false;
            }
        }

        public bool getMessages()
        {
            //Read and process messages until stopped. The main idea here is
            //to isolate message queue locking from message processing. Message
            //processing can be lengthy and while the thread processes a message
            //other threads can post new messages.
            
            int myThreadID = Thread.CurrentThread.GetHashCode();
            if (firstThreadCallingGetMessage!= myThreadID)
            {
                lock (threadIDLock)
                {
                    if (firstThreadCallingGetMessage == -1)
                    {
                        firstThreadCallingGetMessage = myThreadID;
                    }
                    else
                    {
                        throw (new Exception("CRITICAL ERROR: multiple threads are using the getMessages() function!"));
                    }
                }
            }

            ozThreadMessage nextMessage = null;
            bool moreMessages = false;
            bool processMessage = false;

            if (WaitHandle.WaitAll(new ManualResetEvent[] { messageAvailable }))
            {
                moreMessages = true;
                while (moreMessages && !stopped)
                {
                    //We read the next message from the message queue
                    moreMessages = false;
                    processMessage = false;
                    lock (messageQueue)
                    {
                        if (messageQueue.Count > 0)
                        {
                            processMessage = true;
                            nextMessage = (ozThreadMessage)messageQueue[0];
                            messageQueue.RemoveAt(0);

                            if (messageQueue.Count > 0)
                            {
                                moreMessages = true;
                            }
                            else
                            {
                                moreMessages = false;
                                messageAvailable.Reset();
                            }
                        }
                        else
                        {
                            nextMessage = null;
                            moreMessages = false;
                            processMessage = false;
                        }
                    }
                    //We process the message
                    if (processMessage)
                    {
                        try
                        {
                            //  if (nextMessage != null)
                            //  {
                            lastMessage = nextMessage.messageId;
                            onMessage(nextMessage);
                            if (nextMessage.messageProcessed!=null) nextMessage.messageProcessed.Set();
                            //  }
                        }
                        catch (Exception e)
                        {
                            if (nextMessage.messageProcessed != null) nextMessage.messageProcessed.Set();
                            handleException(e, "(getMsg:" + lastMessage.ToString() + ")");
                            #if (DEBUG)
                            throw (e);
                            #endif
                        }
                    }
                    
                }
            }
            return true;
        }

        private void handleException(Exception ex, string info)
        {
            string expMsg;
            try
            {
                expMsg = "[" + ex.GetType().FullName + "] " + ex.Message;
            }
            catch
            {//ez talan nem kell...
                expMsg = "[.] " + ex.Message;
            }
            expMsg = expMsg + " " + info;
            if (localExceptionHandler != null)
            {
                try
                {
                    localExceptionHandler(expMsg);
                }
                catch { }
            }
            else if (globalExceptionHandler != null)
            {
                try
                {
                    globalExceptionHandler(expMsg);
                }
                catch { }
            }
        }

        public virtual void execute()
        {
            lock (threadIDLock)
            {
                firstThreadCallingGetMessage = Thread.CurrentThread.GetHashCode();
            }
            moduleStarted.Set();
            while (!stopped)
            {
                getMessages();
            }
            moduleStopped.Set();
        }

        private void doExecute()
        {
            while (!stopped)
            {
                try
                {
                    execute();
                }
                catch (Exception e)
                {
                    handleException(e,"(execute)");
                    //dont flood the system
                    Thread.Sleep(1000);
                }
            }
            moduleStopped.Set();
        }

        public void postMessage(Object sender, int messageId)
        {
            postMessage(sender ,messageId, null, null);
        }

        public void postMessage(Object sender, int messageId, Object messageParam)
        {
            postMessage(sender, messageId, messageParam, null);
        }

        public void postMessage(Object sender, int messageId, Object messageParam1, Object messageParam2) 
        {
            bool messageAvail = false;
            ozThreadMessage thrMsg = new ozThreadMessage(sender, messageId, messageParam1, messageParam2);
            lock(messageQueue) {
                messageQueue.Add(thrMsg);
                if (messageQueue.Count == 1) messageAvail = true;
            }
            if (messageAvail) messageAvailable.Set();
        }

        public void postMessageIfNotInQueue(Object sender, int messageId)
        {
            postMessageIfNotInQueue(sender, messageId, null, null);
        }

        public void postMessageIfNotInQueue(Object sender, int messageId, Object messageParam1)
        {
            postMessageIfNotInQueue(sender, messageId, messageParam1, null);
        }

        public void postMessageIfNotInQueue(Object sender, int messageId, Object messageParam1, Object messageParam2)
        {
            bool messageAvail = false;
            lock (messageQueue)
            {
                //ki kell deríteni, hogy ilyen ID-vel van-e üzenet a várakozó sorban.
                foreach (ozThreadMessage threadMessage in messageQueue)
                {
                    if ((threadMessage.messageId == messageId) && (threadMessage.messageParam1 == messageParam1) && (threadMessage.messageParam2 == messageParam2)) return;
                }
                //ha nincs akkor:
                messageQueue.Add(new ozThreadMessage(sender, messageId, messageParam1, messageParam2));
                //különben eldobjuk:
                if (messageQueue.Count == 1) messageAvail = true;
            }
            if (messageAvail) messageAvailable.Set();
        }


        public void sendMessage(Object sender, int messageId)
        {
            sendMessage(sender, messageId, null, null);
        }

        public void sendMessage(Object sender, int messageId, object messageParam)
        {
            sendMessage(sender, messageId, messageParam, null);
        }

        public void sendMessage(Object sender, int messageId, object messageParam1, object messageParam2)
        {
            ozThreadMessage myMessage = new ozThreadMessage(sender, messageId,messageParam1,messageParam2);
            myMessage.messageProcessed = new ManualResetEvent(false);
            bool messageAvail = false;
            lock (messageQueue)
            {
                messageQueue.Add(myMessage);
                if (messageQueue.Count == 1) messageAvail = true;
            }
            if (messageAvail) messageAvailable.Set();
            //The calling thread should wait for the processed result
            WaitHandle.WaitAll(new ManualResetEvent[] {myMessage.messageProcessed});
            myMessage = null;
        }

        public ozThreadModule()
        {
        }

        public void startFake()
        {
            lock (startedLock)
            {
                if (started)
                {
                    throw (new Exception("Module alread started"));
                }
                else
                {
                    started = true;
                    stopped = false;
                }
            }
        }

        public void start() {
            startFake();
            thread = new Thread(new ThreadStart(this.doExecute));
#if (DEBUG)
            thread.Name = this.GetType().FullName;
#endif
            thread.Start();
        }

        public void waitUntilStarted()
        {
            WaitHandle.WaitAll(new ManualResetEvent[] { moduleStarted });
        }

        public void startAndWait()
        {
            start();
            waitUntilStarted();
        }

        public void waitUntilStopped()
        {
            WaitHandle.WaitAll(new ManualResetEvent[] { moduleStopped });
        }

        public virtual void stop()
        {
            postMessage(this,WM_OZ_STOP);
        }

        public void stopAndWait()
        {
            if (!stopped)
            {
                stop();
                waitUntilStopped();
            }
        }

        public bool isStopped()
        {
            return stopped;
        }

        public bool busy()
        {
            if (messageQueue.Count > 0) { return true; } else { return false; }
        }
    }
}
