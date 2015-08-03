using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace UHFDemo
{
    public class PR9000
    {
        enum MSG_TYPE
        {
            CMD = 0x00,
            RESP = 0x01,
            NOTI = 0x02
        };
        enum RESP_CODE
        {
            POWER_ON=0x01,
            EPC = 0x22,
            START_INV = 0x27,
            STOP_INV = 0x28
        };
        public enum CALLBACK_MSG_TYPE
        {
            POWER_ON = 0x01,
            START_INV = 0x02,
            STOP_INV = 0x03,
            EPC = 0x04,
            INV_FINISH = 0x05
        };
        private SerialPort iSerialPort;
        public PR9000()
        {
            iSerialPort = new SerialPort();

            iSerialPort.DataReceived+=new SerialDataReceivedEventHandler(ReceivedComData);
        }
        public delegate void AnalyDataCallback(PR9000.CALLBACK_MSG_TYPE msgType, byte arg, byte[] payload);
        public AnalyDataCallback AnalyCallback;
        private void ReceivedComData(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                while (true)
                {
                    int nCount = 1;
                    byte[] preamble = new byte[nCount];
                    iSerialPort.Read(preamble, 0, nCount);
                    if (preamble[0] == 0xBB)
                    {
                        nCount = 4;
                        byte[] header = new byte[nCount];
                        iSerialPort.Read(header, 0, nCount);
                        nCount = (header[2] << 8) | header[3];
                        if (nCount > 0)
                        {
                            byte[] payload = new byte[nCount];
                            if (iSerialPort.Read(payload, 0, nCount) > 0)
                            {
                                if (header[0] == (byte)MSG_TYPE.CMD)
                                {
                                    //can not be here
                                }
                                else if (header[0] == (byte)MSG_TYPE.RESP)
                                {
                                    if (header[1] == (byte)RESP_CODE.POWER_ON)
                                    {
                                        AnalyCallback(CALLBACK_MSG_TYPE.POWER_ON, payload[0],payload);
                                    }
                                    else if (header[1] == (byte)RESP_CODE.START_INV)
                                    {
                                        AnalyCallback(CALLBACK_MSG_TYPE.START_INV, payload[0], payload);
                                    }
                                    else if (header[1] == (byte)RESP_CODE.STOP_INV)
                                    {
                                        AnalyCallback(CALLBACK_MSG_TYPE.STOP_INV, payload[0], payload);
                                    }
                                    else
                                    {
                                        //do not process other msg
                                    }
                                }
                                else if (header[0] == (byte)MSG_TYPE.NOTI)
                                {
                                    if (header[1] == (byte)RESP_CODE.START_INV)
                                    {
                                        AnalyCallback(CALLBACK_MSG_TYPE.INV_FINISH, payload[0], payload);
                                    }
                                    else if (header[1] == (byte)RESP_CODE.EPC)
                                    {
                                        AnalyCallback(CALLBACK_MSG_TYPE.EPC, payload[0], payload);
                                    }
                                }
                                else
                                {
                                }
                            }
                                //AnalyCallback(header[0], header[1], payload);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                string msg = ex.Message;
            }
        }
        public void ClearCom()
        {
            try
            {
                iSerialPort.DiscardInBuffer();
                iSerialPort.DiscardOutBuffer();
            }
            catch (SystemException ex)
            {
            }
        }
        public int SendStartInvCmd()
        {
            byte[] cmd = {0xBB, 0x00, 0x27, 0x00, 0x03, 0x22, 0x00, 0x64, 0x7e};
            return SendMessage(cmd);
            //return 0;
        }
        public int SendStopInvCmd()
        {
            byte[] cmd = { 0xBB, 0x00, 0x28, 0x00, 0x00, 0x7E };
            return SendMessage(cmd);
        }
        public int SendMessage(byte[] btArySenderData)
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
        private void RunReceiveDataCallback(byte[] btAryReceiveData)
        {
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
                iSerialPort.BaudRate = 19200;
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
        private bool isPoweron = false;
        public void setPowerOnState(bool state)
        {
            isPoweron = state;
        }
        public bool getPowerOnState()
        {
            return isPoweron;
        }
        private bool isInvOngoing = false;
        public void setInvState(bool state)
        {
            isInvOngoing = state;
        }
        public bool getInvState()
        {
            return isInvOngoing;
        }
    }
}
