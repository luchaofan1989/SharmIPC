﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO.MemoryMappedFiles;

namespace tiesky.com.SharmIpc
{
    internal enum eInstanceType
    {
        Undefined,
        Master,
        Slave
    }

    internal enum eMsgType:byte
    {
        RpcRequest=1,        
        RpcResponse=2,
        ErrorInRpcAnswer=3,     
        Request = 4
    }

    internal class SharedMemory:IDisposable
    {
       

        //System.IO.MemoryMappedFiles.MemoryMappedViewAccessor accessor = null;
        //System.IO.MemoryMappedFiles.MemoryMappedFile mmf = null;

        Mutex mt = null;

        //EventWaitHandle ewh_ReadyToRead = null;
        //EventWaitHandle ewh_ReadyToWrite = null;

        internal string uniqueHandlerName = "";
        internal long bufferCapacity = 50000;
        internal int maxQueueSizeInBytes = 20000000;
        internal eInstanceType instanceType = eInstanceType.Undefined;

        ReaderWriterHandler rwh = null;
       

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uniqueHandlerName">Can be name of APP, both syncronized processes must use the same name and it must be unique among the OS</param>
        /// <param name="bufferCapacity"></param>
        /// <param name="dataArrived">MsgId of the sender, payload</param>
        public SharedMemory(string uniqueHandlerName, Action<eMsgType, ulong, byte[]> dataArrived, long bufferCapacity = 50000, int maxQueueSizeInBytes = 20000000)
        {
            if (dataArrived == null)
                throw new Exception("tiesky.com.SharmIpc: dataArrived callback can't be empty");

            if (String.IsNullOrEmpty(uniqueHandlerName) || uniqueHandlerName.Length > 200)
                throw new Exception("tiesky.com.SharmIpc: uniqueHandlerName can't be empty or more then 200 symbols");

            if (bufferCapacity < 100)
                bufferCapacity = 100;

            if (bufferCapacity > 1000000)    //max 1MB
                bufferCapacity = 1000000;

            this.uniqueHandlerName = uniqueHandlerName;
            this.bufferCapacity = bufferCapacity;

            try
            {
                mt = new Mutex(true, uniqueHandlerName + "SharmNet_MasterMutex");

                if (mt.WaitOne(500))
                {
                    instanceType = eInstanceType.Master;
                }
                else
                {
                    instanceType = eInstanceType.Slave;
                    if (mt != null)
                    {
                        //mt.ReleaseMutex();
                        mt.Close();
                        mt.Dispose();
                        mt = null;
                    }
                }              
            }
            catch (System.Threading.AbandonedMutexException)
            {
                instanceType = eInstanceType.Master;
            }

            Console.WriteLine(instanceType);

            rwh = new ReaderWriterHandler(this, dataArrived);          
        }

        /// <summary>
        /// Disposing
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (mt != null)
                {
                    mt.ReleaseMutex();
                    mt.Close();
                    mt.Dispose();
                    mt = null;
                }
            }
            catch{
            }

            if (rwh != null)
            {
                rwh.Dispose();
                rwh = null;
            }

        }


        public ulong GetMessageId()
        {
            return this.rwh.GetMessageId();
        }

        public bool SendMessage(eMsgType msgType, ulong msgId, byte[] msg)     
        {
            return this.rwh.SendMessage(msgType, msgId, msg);
        }


        //public void TestSendMessage()
        //{
        //    this.rwh.TestSendMessage();
        //}

     

       
    }//eoc
}