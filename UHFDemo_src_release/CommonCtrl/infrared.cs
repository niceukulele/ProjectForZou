using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace CommonCtrl
{
    public class infrared
    {
        
        private SerialPort iSerialPort;
        public infrared()
        {
            iSerialPort = new SerialPort();

            iSerialPort.DataReceived+=new SerialDataReceivedEventHandler(ReceivedComData);
        }
        public delegate void InfraredEventCallback(byte[] payload);
        public InfraredEventCallback callback;
        private void ReceivedComData(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                while (true)
                {
                    int nCount = 1;
                    byte[] preamble = new byte[nCount];
                    iSerialPort.Read(preamble, 0, nCount);
                    if (preamble[0] == '!')
                    {
                        nCount = 6;
                        byte[] header = new byte[nCount];
                        int nBytes = iSerialPort.Read(header, 0, nCount);
                        if (nBytes != nCount)
                        {
                            iSerialPort.Read(header, nBytes, nCount-nBytes);
                        }
                        if (callback != null)
                        {
                            callback(header);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                string msg = ex.Message;
            }
        }
        public int SendPollingCmd()
        {
            byte[] cmd = { 0x24, 0x30, 0x31, 0x36, 0x0D};
            return SendMessage(cmd);
            //return 0;
        }
        private int SendMessage(byte[] btArySenderData)
        {
            //串口连接方式
            //if (m_nType == 0)
            {
                if (!iSerialPort.IsOpen)
                {
                    return -1;
                }

                iSerialPort.Write(btArySenderData, 0, btArySenderData.Length);

                return 0;
            }
        }
        public int OpenCom(string strPort, out string strException)
        {
            strException = string.Empty;

            if (iSerialPort.IsOpen)
            {
                iSerialPort.Close();
            }

            try
            {
                iSerialPort.PortName = strPort;
                iSerialPort.BaudRate = 9600;
                iSerialPort.ReadTimeout = 200;
                iSerialPort.Open();
            }
            catch (System.Exception ex)
            {
                strException = ex.Message;
                return -1;
            }

            //m_nType = 0;
            return 0;
        }
        public void CloseCom()
        {
            if (iSerialPort.IsOpen)
            {
                iSerialPort.Close();
            }

            //m_nType = -1;
        }
    }
}
