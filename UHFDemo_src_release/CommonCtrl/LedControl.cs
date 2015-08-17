using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace CommonCtrl
{
    public class LedControl
    {
        private SerialPort iSerialPort;
        public LedControl()
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
                    int nCount = 8;
                    byte[] preamble = new byte[nCount];
                    iSerialPort.Read(preamble, 0, nCount);
                    if (preamble[0] == 0x22)
                    {
                        if (callback != null)
                        {
                            callback(preamble);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                string msg = ex.Message;
            }
        }
        private bool ledIsOpen = false;
        public int openLed()
        {
            int ret = 0;
            if (!ledIsOpen)
            {
                ledIsOpen = true;
                byte[] cmd = { 0x55, 0x01, 0x13, 0x00, 0x00, 0x00, 0xff, 0x68 };
                ret = SendMessage(cmd);
            }
            return ret;
            //return 0;
        }
        public int closeLed()
        {
            ledIsOpen = false;
            byte[] cmd = {0x55, 0x01, 0x13, 0x00, 0x00, 0x00, 0x00, 0x69};
            return SendMessage(cmd);
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
