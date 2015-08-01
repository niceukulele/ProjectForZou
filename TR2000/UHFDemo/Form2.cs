using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace UHFDemo
{
    public partial class Form2 : Form
    {
        private Reader.ReaderMethod reader;
        private ReaderSetting m_curSetting = new ReaderSetting();
        private InventoryBuffer m_curInventoryBuffer = new InventoryBuffer();
        private DistinctInvBuffer m_invBuffer = new DistinctInvBuffer();
        private bool m_bDisplayLog = false;
        //记录快速轮询天线参数
        private byte[] m_btAryData = new byte[10];
        //盘存操作前，需要先设置工作天线，用于标识当前是否在执行盘存操作
        private bool m_bInventory = false;
        //Inventory time per 100ms and progress value
        private int m_count = 4000;
        //Inventory output power (dbm)
        private byte m_power = 30;
        //Reader is Normal
        private bool m_readerIsNormal = false;
        //play alarm
        private UHFSoundPlayer sp = new UHFSoundPlayer();
        //server communication module
        private ServerComm sc;

        private ReaderConfig conf = new ReaderConfig();

        public log4net.ILog logFile;
#if false
        private delegate void writeLogUnSafe(string log);   
        private void writeLog(string log)
        {
            if (this.InvokeRequired)
            {
                writeLogUnSafe InvokeWriteLog = new writeLogUnSafe(writeLog);
                this.Invoke(InvokeWriteLog, new object[] { log });
            }
            else
            //if (conf.LogOn)
            {
                //writeLog(log);
                logFile.Debug(log);
            }
        }
#endif
        private void writeLog(string log)
        {
             {
                //writeLog(log);
                logFile.Debug(log);
            }
        }
        //在Form1中改写鼠标消息
        /*
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_NCHITTEST:
                    base.WndProc(ref m);
                    if ((int)m.Result == HTCLIENT)
                        m.Result = (IntPtr)HTCAPTION;
                    return;
                    break;
            }
            base.WndProc(ref m);
        }
        */
        public Form2()
        {
            InitializeComponent();
            //this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.MaximumSize = Screen.PrimaryScreen.WorkingArea.Size;
            //logRichText.BackColor = System.Drawing.Color.Transparent;
        }
        private void initConfigInfo()
        {
            //string logname = DateTime.Now.ToFileTimeUtc().ToString() + ".log";
            logFile = log4net.LogManager.GetLogger("logdebug");
            HelperConfigSerialize helper = new HelperConfigSerialize(conf);

            if (File.Exists(helper.fileName))
            {
                if (helper.LoadConfig())
                {
                    writeLog("Load config file ok");
                }
                else
                {
                    helper.SaveConfig();
                    writeLog("Load config file fail, use default config");
                }   
                conf = helper.oData;
            }
            else
            {
                helper.SaveConfig();
            }

            //test
            //CYMessageBox.Show("Hello,world");
            //startRealInv();
            

        }
        private void Form2_Load(object sender, EventArgs e)
        {
            initConfigInfo();
            initWidget();

            if (initReader())
            {
                getFirmwareVersion();
                Thread.Sleep(300);
                setOutputPower((byte)conf.ReaderPower);
                Thread.Sleep(300);
                getOutputPower();
                Thread.Sleep(300);
                if (readerIsLive)
                {
                    writeLog("init reader ok and reader is live");
                }
                else
                {
                    writeLog("reader did not resp");
                }
            }
            else
            {
                writeLog("open reader serial port fail");
            }
            initServerComm();
            if (conf.operaMode == (byte)ReaderConfig.OPERA_MODE.STORAGE)
            {
                if (initPr9000())
                {
                    enablePr9000CtsInv();
                    writeLog("init pr9000 ok");
                }
                else
                {
                    writeLog("init pr9000 fail");
                }
            }
            else if (conf.operaMode == (byte)ReaderConfig.OPERA_MODE.SECURITY)
            {
                if (initInfrared())
                {
                    writeLog("init infrared ok and start to polling infrared");
                    infraredTimerInit();
                }
                else
                {
                    writeLog("init infrared fail");
                }
                if (initLed())
                {
                    //ledTimerInit();
                    writeLog("init led control ok");
                    
                }
                else
                {
                    writeLog("init led fail");
                }
                //startLedAlarm();
            }
           
            //just for debug
            //startCollectDocEpc();
        }
        private void initWidget()
        {
            
            MyHelper hlp = new MyHelper();
            hlp.controllInitializeSize(this);
            //hlp.controllInitializeSize(this.panel3);
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            hlp.controlAutoSize(this);
            int x = (textBoxStuffNum.Right + textBoxStuffNum.Left)/2 - label2.Width / 2;
            int y = this.label2.Location.Y;
            this.label2.Location = new System.Drawing.Point(x, y);//(int)(hlp.getWscale()*this.label1.Location.X);
            //x = (int)(hlp.getWscale() * this.label3.Location.X);
            y = this.label3.Location.Y;
            this.label3.Location = new System.Drawing.Point(x, y);
            //labelTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //labelTitle.Text = conf.FormTitle;
            //labelTitle.Location 
            //    = new Point((panel1.Location.X + panel1.Width - labelTitle.Width)/2, 
            //                (panel1.Location.Y + panel1.Height - labelTitle.Height)/ 2);
            label_date.Text = DateTime.Now.ToLongDateString().ToString();
            label_weeknum.Text = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(DateTime.Now.DayOfWeek);
            this.MaximizeBox = false;
            pictureBoxBackG.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBoxExit.SizeMode = PictureBoxSizeMode.StretchImage;
            //labelTitle.Location.X = panel1.Location.X / 2;
            //labelTitle.Location.Y = panel1.Location.Y / 2;
            Image img = pictureBoxBackG.Image;
            Bitmap bmp = new Bitmap(img); 
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    Color bmpcolor = bmp.GetPixel(i, j);
                    byte A = bmpcolor.A;
                    byte R = bmpcolor.R;
                    byte G = bmpcolor.G;
                    byte B = bmpcolor.B;
                    bmpcolor = Color.FromArgb(50, R, G, B);
                    bmp.SetPixel(i, j, bmpcolor);
                }
            }
            pictureBoxBackG.Image = bmp;
        }
        //init inventory params
        private bool initReader()
        {
            //初始化访问读写器实例
            reader = new Reader.ReaderMethod();

            //回调函数
            reader.AnalyCallback = AnalyData;
            reader.ReceiveCallback = ReceiveData;
            reader.SendCallback = SendData;

            //connect to reader
            string strException = string.Empty;
            string strComPort = conf.ReaderPort;
            //int nBaudrate = Convert.ToInt32(cmbBaudrate.Text);
            int nBaudrate = 115200;

            int nRet = reader.OpenCom(strComPort, nBaudrate, out strException);
            if (nRet != 0)
            {
                string strLog = "连接读写器失败，失败原因： " + strException;
                WriteLog(logRichText, strLog, 1);
                writeLog(strLog);
                m_readerIsNormal = true;
                //buttonInv.Enabled = false;
                return false;
            }
            else
            {
                string strLog = "连接读写器设备 " + strComPort + "@" + nBaudrate.ToString();
                WriteLog(logRichText, strLog, 0);
                writeLog(strLog);
            }

            //output power init
            //Thread.Sleep(2000);
            
            return true;
        }

        //copy from demo
        //ReceiveData() just for debug
        private void ReceiveData(byte[] btAryReceiveData)
        {
            if (m_bDisplayLog)
            {
                string strLog = CCommondMethod.ByteArrayToString(btAryReceiveData, 0, btAryReceiveData.Length);

                //WriteLog(lrtxtDataTran, strLog, 1);
            }
        }
        //SendData() just for debug
        private void SendData(byte[] btArySendData)
        {
            if (m_bDisplayLog)
            {
                string strLog = CCommondMethod.ByteArrayToString(btArySendData, 0, btArySendData.Length);

                //WriteLog(lrtxtDataTran, strLog, 0);
            }
        }
        private bool readerIsLive = false;
        private void AnalyData(Reader.MessageTran msgTran)
        {
            readerIsLive = true;
            if (msgTran.PacketType != 0xA0)
            {
                return;
               
            }
            string log = "recv cmd resp cmd = " + msgTran.Cmd;
            //writeLog(log);
            switch (msgTran.Cmd)
            {
                case 0x72:
                    ProcessGetFirmwareVersion(msgTran);
                    break;
                case 0x74:
                    ProcessSetWorkAntenna(msgTran);
                    break;
                case 0x76:
                    ProcessSetOutputPower(msgTran);
                    break;
                case 0x77:
                    ProcessGetOutputPower(msgTran);
                    break;
                case 0x80:
                    ProcessInventory(msgTran);
                    break;
                case 0x89:
                case 0x8B:
                    ProcessInventoryReal(msgTran);
                    break;
                case 0x91:
                    ProcessGetAndResetInventoryBuffer(msgTran);
                    break;
                case 0x92:
                    ProcessGetInventoryBufferTagCount(msgTran);
                    break;
                case 0x93:
                    ProcessResetInventoryBuffer(msgTran);
                    break;
                default:
                    break;
                    
            }
  
        }
        private void ProcessGetInventoryBufferTagCount(Reader.MessageTran msgTran)
        {
            string strCmd = "缓存标签数量";
            string strErrorCode = string.Empty;
            gGetEpcTag = true;
            if (msgTran.AryData.Length == 2)
            {
                m_curInventoryBuffer.nTagCount = Convert.ToInt32(msgTran.AryData[0]) * 256 + Convert.ToInt32(msgTran.AryData[1]);

                //RefreshInventory(0x92);
                string strLog1 = strCmd + " " + m_curInventoryBuffer.nTagCount.ToString();
                writeLog(strLog1);
                return;
            }
            else if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;

            writeLog(strLog);
        }
        private void ProcessInventory(Reader.MessageTran msgTran)
        {
            string strCmd = "盘存标签";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 9)
            {
                m_curInventoryBuffer.nCurrentAnt = msgTran.AryData[0];
                m_curInventoryBuffer.nTagCount = Convert.ToInt32(msgTran.AryData[1]) * 256 + Convert.ToInt32(msgTran.AryData[2]);
                m_curInventoryBuffer.nReadRate = Convert.ToInt32(msgTran.AryData[3]) * 256 + Convert.ToInt32(msgTran.AryData[4]);
                int nTotalRead = Convert.ToInt32(msgTran.AryData[5]) * 256 * 256 * 256
                    + Convert.ToInt32(msgTran.AryData[6]) * 256 * 256
                    + Convert.ToInt32(msgTran.AryData[7]) * 256
                    + Convert.ToInt32(msgTran.AryData[8]);
                m_curInventoryBuffer.nDataCount = nTotalRead;
                m_curInventoryBuffer.lTotalRead.Add(nTotalRead);
                m_curInventoryBuffer.dtEndInventory = DateTime.Now;

               // RefreshInventory(0x80);
                //WriteLog(lrtxtLog, strCmd, 0);

                RunLoopInventroy();

                return;
            }
            else if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;

            //WriteLog(lrtxtLog, strLog, 1);
            writeLog(strLog);

            RunLoopInventroy();
        }
        private void ProcessResetInventoryBuffer(Reader.MessageTran msgTran)
        {
            string strCmd = "清空缓存";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    //RefreshInventory(0x93);
                    writeLog("clear inventory buffer");
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;

            writeLog(strLog);
        }
        private void ProcessSetWorkAntenna(Reader.MessageTran msgTran)
        {
            int intCurrentAnt = 0;
            intCurrentAnt = m_curSetting.btWorkAntenna + 1;
            string strCmd = "设置工作天线成功,当前工作天线: 天线" + intCurrentAnt.ToString();
            string log = "ProcessSetWorkAntenna:: current ant" + intCurrentAnt;
            //writeLog(log);
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    //WriteLog(logRichText, strCmd, 0);

                    //校验是否盘存操作
                    if (m_bInventory)
                    {
                        log = "ProcessSetWorkAntenna:: continue to inventory";
                        //writeLog(log);
                        RunLoopInventroy();
                    }
                    else
                    {
                        string mylog = "ProcessSetWorkAntenna::Now inventory finish";
                        writeLog(mylog);
                        waitInvFinish.Release();
                        /*
                        //wake up process thread
                        if (waitInvFinish != null)
                        {
                            try
                            {
                                waitInvFinish.Release();
                            }
                            catch (SystemException ex)
                            {
                                writeLog("ProcessSetWorkAntenna::waitInvFinish.Release()" + ex.Message);
                            }
                        }
                         * */
                    }
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            writeLog(strLog);

            if (m_bInventory)
            {
                m_curInventoryBuffer.nCommond = 1;
                m_curInventoryBuffer.dtEndInventory = DateTime.Now;
                RunLoopInventroy();
            }
        }
        private void getFirmwareVersion()
        {
            reader.GetFirmwareVersion(m_curSetting.btReadId);
        }
        private void ProcessGetFirmwareVersion(Reader.MessageTran msgTran)
        {
            string strCmd = "取得读写器版本号";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 2)
            {
                m_curSetting.btMajor = msgTran.AryData[0];
                m_curSetting.btMinor = msgTran.AryData[1];
                m_curSetting.btReadId = msgTran.ReadId;

                //RefreshReadSetting(msgTran.Cmd);
                //WriteLog(logRichText, strCmd, 0);
                return;
            }
            else if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(logRichText, strLog, 1);
            writeLog(strLog);
        }
        private bool isDocumentEpc(string epc)
        {
            if (epc.Substring(4, 6) == "232352")
            {
                return true;
            }
            else
            {
                writeLog("check if doc: epc is " + epc);
                return false;
            }
        }
        private int epcIndex = 0;
        private string[] EpcCollections = new string[400];
        private void ProcessGetAndResetInventoryBuffer(Reader.MessageTran msgTran)
        {
            string strCmd = "读取清空缓存";
            string strErrorCode = string.Empty;
            gGetEpc = true;
            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                string strLog = strCmd + "失败，失败原因： " + strErrorCode;
                writeLog(strLog);
                //WriteLog(logRichText, strLog, 1);
            }
            else
            {
                
                string tempEpc = CCommondMethod.ByteArrayToStringNoBlank(msgTran.AryData, 3, msgTran.AryData.Length - 8);
                //writeLog(tempEpc);
                //if (tempEpc != stuffEpc)
                if (isDocumentEpc(tempEpc))
                {
                    EpcCollections[epcIndex] = tempEpc;
                    epcIndex++;
                }
                /*
                int nDataLen = msgTran.AryData.Length;
                int nEpcLen = Convert.ToInt32(msgTran.AryData[2]) - 4;

                string strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, 2);
                string strEpc = CCommondMethod.ByteArrayToString(msgTran.AryData, 5, nEpcLen);
                string strCRC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5 + nEpcLen, 2);
                string strRSSI = msgTran.AryData[nDataLen - 3].ToString();
                //SetMaxMinRSSI(Convert.ToInt32(msgTran.AryData[nDataLen - 3]));
                byte btTemp = msgTran.AryData[nDataLen - 2];
                byte btAntId = (byte)((btTemp & 0x03) + 1);
                string strAntId = btAntId.ToString();
                string strReadCnr = msgTran.AryData[nDataLen - 1].ToString();
                */
            }
        }
        private void ProcessInventoryReal(Reader.MessageTran msgTran)
        {
            string strCmd = "";
            if (msgTran.Cmd == 0x89)
            {
                strCmd = "实时盘存";
            }
            if (msgTran.Cmd == 0x8B)
            {
                strCmd = "自定义Session和Inventoried Flag盘存";
            }
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                string strLog = strCmd + "失败，失败原因： " + strErrorCode;

                //WriteLog(logRichText, strLog, 1);
                //RefreshInventoryReal(0x00);
                writeLog(strLog);
                RunLoopInventroy();
            }
                //inventory finish
            else if (msgTran.AryData.Length == 7)
            {
                m_curInventoryBuffer.nReadRate = Convert.ToInt32(msgTran.AryData[1]) * 256 + Convert.ToInt32(msgTran.AryData[2]);
                m_curInventoryBuffer.nDataCount = Convert.ToInt32(msgTran.AryData[3]) * 256 * 256 * 256 + Convert.ToInt32(msgTran.AryData[4]) * 256 * 256 + Convert.ToInt32(msgTran.AryData[5]) * 256 + Convert.ToInt32(msgTran.AryData[6]);

                //WriteLog(logRichText, strCmd, 0);
                //RefreshInventoryReal(0x01);
                RunLoopInventroy();
            }
            else
            {
                //save tag epc and update UI
                saveEpcAndUpdate(msgTran);
            }
        }
        private enum TagType { Unknow = 0, Person = 1, Record = 2};
        private TagType checkTagType(byte[] epc)
        {
            if (epc.Length > 3)
            {
                if (epc[7] == '#' && epc[8] == '#')
                {
                    if (epc[9] == 'U')
                        return TagType.Person;
                    else if (epc[9] == 'R')
                        return TagType.Record;
                    else
                        return TagType.Unknow;
                }
            }
            return TagType.Unknow;
        }
        private delegate void saveEpcAndUpdateUnsafe(Reader.MessageTran msgTran);
        private void saveEpcAndUpdate(Reader.MessageTran msgTran)
        {
            if (this.InvokeRequired)
            {
                saveEpcAndUpdateUnsafe InvokeRefresh = new saveEpcAndUpdateUnsafe(saveEpcAndUpdate);
                this.Invoke(InvokeRefresh, new object[] { msgTran });
            }
            else
            {
                int nLength = msgTran.AryData.Length;
                int nEpcLength = nLength - 4;

                string strEPC = CCommondMethod.MyByteArrayToString(msgTran.AryData, 1, msgTran.AryData.Length - 2);
                //string strEPC = CCommondMethod.ByteArrayToStringNoBlank(msgTran.AryTranData, 5, msgTran.AryTranData.Length - 5);
                //WriteLog(logRichText, strEPC, 0);
                
                //if (tt != TagType.Unknow)
                {
                    //if (!m_invBuffer.contains(strEPC))
                    {
                        TagType tt = checkTagType(msgTran.AryTranData);
                        if (tt == TagType.Record)
                        {
                            sp.playAlarmTime(conf.AlarmDuration);
                            startLedAlarm();
                            //ledCtrl.openLed();
                            //startSwitchLed();
                        }
                        m_invBuffer.collectEpc(strEPC);
                        //update view
                    }
                }
            }
        }
        string epcFileName = "";
        private bool saveEpcToFile()
        {
            bool ret = true;
            try
            {
                //create file and name is current time tick
                
                //sw.WriteLine(stuffEpc);
                //save epc to file
                /*
                foreach (KeyValuePair<string, string> kv in m_invBuffer.EpcCollections)
                {
                    string epc = kv.Value;
                    sw.WriteLine(epc);
                }
                 * */
                string strLog = "inventory result: epcIndex=" + epcIndex;
                writeLog(strLog);
                int count = epcIndex - 1; //1st is stuff EPC
                if (count > 0)
                {
                    strLog = "盘存到" + count + "份档案";
                    epcFileName = "EPC"+DateTime.Now.ToFileTimeUtc().ToString();
                    epcFileName += ".dat";
                    FileStream fs = new FileStream(epcFileName, FileMode.Create);
                    StreamWriter sw = new StreamWriter(fs);
                    for (int i = 0; i < epcIndex; i++)
                    {

                        string briefEpc = EpcCollections[i].Substring(4, EpcCollections[i].Length-4);
                        //writeLog("Epc:" + EpcCollections[i]);
                        //writeLog("Brief Epc:" + briefEpc);
                        sw.WriteLine(briefEpc);
                    }
                    
                    sw.Flush();
                    sw.Close();
                    fs.Close();
                }
                else
                {
                    strLog = "未盘存到档案";
                    ret = false;
                }
                WriteLog(logRichText, strLog, 0);
                writeLog(strLog);
                epcIndex = 0;
                return ret;
                    //m_invBuffer.clearBuffer();
                
            }
            catch (SystemException e)
            {
                writeLog("save epc to file error:" + e.Message);
                return false;
            }
        }
        private void saveWarnLogToFile()
        {
            try
            {
                //create file and name is current time tick
                if (m_invBuffer.size() > 0)
                {
                    epcFileName = "WarnLog" + DateTime.Now.ToFileTimeUtc().ToString();
                    epcFileName += ".dat";
                    FileStream fs = new FileStream(epcFileName, FileMode.Create);
                    StreamWriter sw = new StreamWriter(fs);
                    //save epc to file
                    foreach (string kv in m_invBuffer.EpcCollections)
                    {
                        string briefEpc = kv.Substring(4, kv.Length - 4);
                        writeLog("Warn Epc:" + kv);
                        //writeLog("Warn log Brief Epc:" + briefEpc);
                        sw.WriteLine(briefEpc);
                    }
                    string strLog = "盘存到" + m_invBuffer.size() + "张标签";
                    WriteLog(logRichText, strLog, 0);
                    writeLog(strLog);
                    m_invBuffer.clearBuffer();
                    sw.Flush();
                    sw.Close();
                    fs.Close();
                }
            }
            catch (SystemException e)
            {
                writeLog("save epc to file error:" + e.Message);
            }
        }
        private delegate void RunLoopInventoryUnsafe();
        private void RunLoopInventroy()
        {
            if (this.InvokeRequired)
            {
                RunLoopInventoryUnsafe InvokeRunLoopInventory = new RunLoopInventoryUnsafe(RunLoopInventroy);
                this.Invoke(InvokeRunLoopInventory, new object[] { });
            }
            else
            {
                //校验盘存是否所有天线均完成
                //string mylog = "using ant " + m_curSetting.btWorkAntenna + "inventory";
                //writeLog(mylog);
                //mylog = "RunLoopInventroy:: nIndexAntenna=" + m_curInventoryBuffer.nIndexAntenna + "  count=" + m_curInventoryBuffer.lAntenna.Count;
                //writeLog(mylog);
                //mylog = "nCommond=" + m_curInventoryBuffer.nCommond;
                if (m_curInventoryBuffer.nIndexAntenna < m_curInventoryBuffer.lAntenna.Count - 1 || m_curInventoryBuffer.nCommond == 0)
                {
                    if (m_curInventoryBuffer.nCommond == 0)
                    {
                        m_curInventoryBuffer.nCommond = 1;

                        if (m_curInventoryBuffer.bLoopInventoryReal)
                        {
                            //m_bLockTab = true;
                            //btnInventory.Enabled = false;
                            if (m_curInventoryBuffer.bLoopCustomizedSession)//自定义Session和Inventoried Flag 
                            {
                                reader.CustomizedInventory(m_curSetting.btReadId, m_curInventoryBuffer.btSession, m_curInventoryBuffer.btTarget, m_curInventoryBuffer.btRepeat);
                            }
                            else //实时盘存
                            {
                                reader.InventoryReal(m_curSetting.btReadId, m_curInventoryBuffer.btRepeat);

                            }
                        }
                        else
                        {
                            if (m_curInventoryBuffer.bLoopInventory)
                            {
                                string mylog = "RunLoopInventroy:: using ant " + m_curSetting.btWorkAntenna + "inventory";
                                //writeLog(mylog);
                                reader.Inventory(m_curSetting.btReadId, m_curInventoryBuffer.btRepeat);
                            }
                        }
                    }
                    else
                    {
                        m_curInventoryBuffer.nCommond = 0;
                        m_curInventoryBuffer.nIndexAntenna++;

                        byte btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                        //writeLog("nIndexAntenna %d ant %d inventory ", m_curInventoryBuffer.nIndexAntenna, btWorkAntenna);
                        reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                        m_curSetting.btWorkAntenna = btWorkAntenna;
                    }
                }
                //校验是否循环盘存
                else if (m_curInventoryBuffer.bLoopInventory)
                {
                    m_curInventoryBuffer.nIndexAntenna = 0;
                    m_curInventoryBuffer.nCommond = 0;

                    byte btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                    reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                    m_curSetting.btWorkAntenna = btWorkAntenna;
                }
                else
                {
                    string mylog = "RunLoopInventroy::Now inventory finish";
                    writeLog(mylog);
                    waitInvFinish.Release();
                    /*
                    //wake up process thread
                    if (waitInvFinish != null)
                    {
                        try
                        {
                            waitInvFinish.Release();
                        }
                        catch (SystemException ex)
                        {
                            writeLog("RunLoopInventroy::waitInvFinish.Release()" + ex.Message);
                        }
                    }
                     * */
                }
            
            }
        }

        private delegate void RunLoopFastSwitchUnsafe();
        private void RunLoopFastSwitch()
        {
            if (this.InvokeRequired)
            {
                RunLoopFastSwitchUnsafe InvokeRunLoopFastSwitch = new RunLoopFastSwitchUnsafe(RunLoopFastSwitch);
                this.Invoke(InvokeRunLoopFastSwitch, new object[] { });
            }
            else
            {
                if (m_curInventoryBuffer.bLoopInventory)
                {
                    string mylog = "RunLoopFastSwitch";
                    writeLog(mylog);
                    reader.FastSwitchInventory(m_curSetting.btReadId, m_btAryData);
                }
            }
        }

        private delegate void WriteLogUnSafe(CustomControl.LogRichTextBox logRichTxt, string strLog, int nType);
        private void WriteLog(CustomControl.LogRichTextBox logRichTxt, string strLog, int nType)
        {
            if (this.InvokeRequired)
            {
                WriteLogUnSafe InvokeWriteLog = new WriteLogUnSafe(WriteLog);
                this.Invoke(InvokeWriteLog, new object[] { logRichTxt, strLog, nType });
            }
            else
            {
                if (nType == 0)
                {
                    logRichTxt.AppendTextEx(strLog, Color.Indigo);
                }
                else
                {
                    logRichTxt.AppendTextEx(strLog, Color.Red);
                }
                logRichTxt.Select(logRichTxt.TextLength, 0);
                logRichTxt.ScrollToCaret();
            }
        }
        private delegate void ClearLogUnSafe(CustomControl.LogRichTextBox logRichTxt);
        private void ClearLog(CustomControl.LogRichTextBox logRichTxt)
        {
            if (this.InvokeRequired)
            {
                ClearLogUnSafe InvokeWriteLog = new ClearLogUnSafe(ClearLog);
                this.Invoke(InvokeWriteLog, new object[] { logRichTxt});
            }
            else
            {
                logRichTxt.Clear();
            }
        }
        private void logRichText_TextChanged(object sender, EventArgs e)
        {

        }
        private void startRealInv()
        {
            //sp.playAlarm();
            m_curInventoryBuffer.ClearInventoryPar();
            m_curInventoryBuffer.bLoopCustomizedSession = false;
            m_curInventoryBuffer.btRepeat = 1;
            //set ant params
            for (int i = 0; i < 4; i++)
            {
                if ((conf.AntPort & (1 << i)) != 0)
                {
                    m_curInventoryBuffer.lAntenna.Add((byte)i);
                    writeLog("real inv :open ant " + i);
                }
            }
                //m_curInventoryBuffer.lAntenna.Add(0x00);
            //m_curInventoryBuffer.lAntenna.Add(0x01);
            //m_curInventoryBuffer.lAntenna.Add(0x02);
            //m_curInventoryBuffer.lAntenna.Add(0x03);

            m_curInventoryBuffer.bLoopInventoryReal = true;
            m_curInventoryBuffer.ClearInventoryRealResult();
            m_bInventory = true;
            m_curInventoryBuffer.bLoopInventory = true;
            //wangbo add
            m_invBuffer.clearBuffer();
            //InvlistView.Clear();
            //InvlistView.Items.Clear();
            byte btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
            reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
            m_curSetting.btWorkAntenna = btWorkAntenna;

            invSem.WaitOne();
            processInvEpcData();
        }
       
        private void stopRealInv()
        {
            m_curInventoryBuffer.bLoopInventoryReal = false;
            m_bInventory = false;
            m_curInventoryBuffer.bLoopInventory = false;
        }
        static Form_messagebox myInfoMessageBox;
        private void showMyMesgBox()
        {
            if (myInfoMessageBox == null)
            {
                myInfoMessageBox = new Form_messagebox();
            }
            myInfoMessageBox.showInfoWithBar("正在扫描...", conf.InvDuration, conf.InvDuration*100/(4+3));
            //Form_messagebox.dispose();
        }
        Thread tInvDiag;
        private void startInvDiag()
        {
            if (tInvDiag != null)
            {
                try
                {
                    tInvDiag.Abort();
                }
                catch (Exception)
                {
                }
            }
#if true
            tInvDiag = new Thread(new ThreadStart(showMyMesgBox));
            tInvDiag.Start();
#endif
        }

        private delegate void stopInvDiagUnsafe();
        private void stopInvDiag()
        {
            if (this.InvokeRequired)
            {
                stopInvDiagUnsafe InvokestopInvDiag = new stopInvDiagUnsafe(stopInvDiag);
                this.Invoke(InvokestopInvDiag, new object[] { });
            }
            else
            {
                if (tInvDiag != null)
                {
                    try
                    {
                        
                        myInfoMessageBox.diposeInfo();
                    }
                    catch (SystemException e)
                    {
                        writeLog(e.Message);
                    }
                }
            }
        }
        private Semaphore invSem = new Semaphore(0, 1);
        private void startBufferInv()
        {
            //sp.playAlarm();
            //first clear reader Inventory buffer
            //reader.ResetInventoryBuffer(m_curSetting.btReadId);
            //Thread.Sleep(300);
            m_curInventoryBuffer.ClearInventoryPar();
            m_curInventoryBuffer.bLoopCustomizedSession = false;
            m_curInventoryBuffer.btRepeat = 1;
            //set ant params
            for (int i = 0; i < 4; i++)
            {
                if ((conf.AntPort & (1 << i)) != 0)
                {
                    m_curInventoryBuffer.lAntenna.Add((byte)i);
                    writeLog("buffer inv : open ant " + i);
                }
            }
            //m_curInventoryBuffer.lAntenna.Add(0x02);
            //m_curInventoryBuffer.lAntenna.Add(0x03);

            m_curInventoryBuffer.bLoopInventoryReal = false;
            m_curInventoryBuffer.ClearInventoryRealResult();
            m_bInventory = true;
            m_curInventoryBuffer.bLoopInventory = true;
            
            byte btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
            reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
            m_curSetting.btWorkAntenna = btWorkAntenna;
            
            startInvDiag();
            waitInvFinish = new Semaphore(0, 1);
            invSem.WaitOne();
            processInvEpcData();
        }
        private void stopBufferInv()
        {
            m_curInventoryBuffer.bLoopInventoryReal = false;
            m_bInventory = false;
            m_curInventoryBuffer.bLoopInventory = false;
            
        }
       // private int m_timer_count = 0;
#if false
        private void timerInv_Tick(object sender, EventArgs e)
        {
            if (m_timer_count == 0)
            {
                //timerInv.Enabled = false;
                timerInventory.Stop();
                stopRealInv();
                //buttonInv.Enabled = true;
                saveEpcToFile();
                if (sc.sendEpc(epcFileName) != 0)
                {
                    MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
                    MessageBoxIcon messIcon = MessageBoxIcon.Warning;
                    DialogResult dr = MessageBox.Show("确定继续提交数据么？","取消操作",messButton,messIcon);
                    if (dr == DialogResult.OK)
                    {
                        if (sc.forceSendEpc(epcFileName) != 0)
                        {
                            //TODO
                        }
                    }
                    else
                    {
                        sc.deleteEpcFile(epcFileName);
                    }
                }
                //start new session
                inUploadSession = false;
                enablePr9000Inv();
                return;
            }
            //progressBarInv.Value += progressBarInv.Maximum / m_count;
            m_timer_count--;
        }
#endif
        
        //private int bufferTagCount = 0;
        private void timeout(object source, System.Timers.ElapsedEventArgs e)
        {
            stopBufferInv();
            //stopInvDiag();
            invSem.Release();
            writeLog("inventory timeout finish");
        }
        private bool gGetEpc = false;
        private bool gGetEpcTag = false;
        private Semaphore waitInvFinish = new Semaphore(0, 1);
        private void processInvEpcData()
        {
            
            if (conf.operaMode == (byte)ReaderConfig.OPERA_MODE.STORAGE)
            {
                string mylog = "";
                waitInvFinishtimerInit(8);
                
                waitInvFinish.WaitOne();
                /*
                if (waitInvFinish != null)
                {
                    waitInvFinish.Close();
                    waitInvFinish = null;
                }
                if (waitInvFinish == null)
                {
                    waitInvFinish = new Semaphore(0, 1);
                }
                try
                {
                    waitInvFinish.WaitOne();
                }
                catch (SystemException ex)
                {
                    writeLog("waitInvFinish.WaitOne()"+ex.Message);
                }
                 * */
                clearInvFinishTimer();
                
                myInfoMessageBox.setBarValue(myInfoMessageBox.getBarRemain()/4);
                //invSem.Release();
                //Thread.Sleep(3000);
                for (int j = 0; j < 3; j++)
                {
                    mylog = "send GetInventoryBufferTagCount cmd " + j;
                    writeLog(mylog);
                    reader.GetInventoryBufferTagCount(m_curSetting.btReadId);
                    Thread.Sleep(1500);
                    if (gGetEpcTag)
                    {
                        gGetEpcTag = false;
                        writeLog("Already get Tag count");
                        break;
                    }
                }
                myInfoMessageBox.setBarValue(myInfoMessageBox.getBarRemain() / 3);
                //Thread.Sleep(1000);
                for (int j = 0; j < 5; j++)
                {
                    mylog = "send GetAndResetInventoryBuffer cmd " + j;
                    writeLog(mylog);
                    reader.GetAndResetInventoryBuffer(m_curSetting.btReadId);
                    Thread.Sleep(1000);
                    if (gGetEpc)
                    {
                        gGetEpc = false;
                        writeLog("Already get Epc");
                        break;
                    }
                }
                myInfoMessageBox.setBarValue(myInfoMessageBox.getBarRemain()/2);
                    
                //wait for get Epc
                Thread.Sleep(500);
                myInfoMessageBox.setBarValue(myInfoMessageBox.getBarRemain());
                Thread.Sleep(500);
                stopInvDiag();
                //buttonInv.Enabled = true;  
                if (saveEpcToFile())
                {
                    int sendRet = sc.sendEpc(epcFileName);
                    if (sendRet != 0)
                    {
                        //MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
                        //MessageBoxIcon messIcon = MessageBoxIcon.Warning;
                        
                        string strLog = "发送失败 error=" + sendRet;
                        writeLog(strLog);
                        string showLog = "发送数据至服务器异常";
                        WriteLog(logRichText, showLog, 1);
                        int ret = Form_messagebox.show();
                        if (ret == 0)
                        {
                            if (sc.forceSendEpc(epcFileName) != 0)
                            {
                                //TODO
                                strLog = "强制发送失败";
                                writeLog(strLog);
                                WriteLog(logRichText, strLog, 1);
                                Form_messagebox.show("强制发送失败");
                            }
                            else
                            {
                                strLog = "强制发送数据至服务器成功";
                                writeLog(strLog);
                                WriteLog(logRichText, strLog, 0);
                                Form_messagebox.show("√ 操作成功");
                            }
                        }
                        else
                        {
                            //do not delete file
                            //sc.deleteEpcFile(epcFileName);
                        }
                    }
                    else
                    {
                        string strLog = "已完成档案标签信息上传";
                        writeLog(strLog);
                        WriteLog(logRichText, strLog, 0);
                        Form_messagebox.show("√ 操作成功");
                    }
                }
                //start new session
                
                
                inUploadSession = false;
                enablePr9000CtsInv();
            }
            else if (conf.operaMode == (byte)ReaderConfig.OPERA_MODE.SECURITY)
            {
                stopRealInv();
                //reader.GetAndResetInventoryBuffer(m_curSetting.btReadId);
                waitInvFinishtimerInit(2);
                if (waitInvFinish != null)
                {
                    waitInvFinish.Close();
                    waitInvFinish = null;
                }
                if (waitInvFinish == null)
                {
                    waitInvFinish = new Semaphore(0, 1);
                }
                try
                {
                    waitInvFinish.WaitOne();
                }
                catch (SystemException ex)
                {
                    writeLog("waitInvFinish.WaitOne()" + ex.Message);
                }
                clearInvFinishTimer();
                //Thread.Sleep(500);
                saveWarnLogToFile();
                ledCtrl.closeLed();
                if (sc.sendWarnLog(epcFileName) != 0)
                {
                    string strLog = "发送LOG失败";
                    writeLog(strLog);
                    WriteLog(logRichText, strLog, 1);
                }
                else
                {
                    string strLog = "已完成WARN LOG信息上传";
                    writeLog(strLog);
                    WriteLog(logRichText, strLog, 0);
                }
                //start to run infrared checking routine
                if (waitInvFinish != null)
                {
                    waitInvFinish.Close();
                    waitInvFinish = null;
                }
                infraredTimer.Enabled = true;
                //stopSwitchLed();
            }
            else
            {
                //do nothing
            }

            reader.ClearCom();
        }
        private void waitInvFinishtimeout(object source, System.Timers.ElapsedEventArgs e)
        {
            string log = "wait Inv Finish already timeout";
            writeLog(log);
            waitInvFinish.Release();
            /*
            if (waitInvFinish != null)
            {
                try
                {
                    waitInvFinish.Release();
                }
                catch (SystemException ex)
                {
                    writeLog("waitInvFinishtimeout::waitInvFinish.Release()" + ex.Message);
                }
            }
             * */
        }
        System.Timers.Timer invWaitTimer;// = new System.Timers.Timer(10 * 1000);
        private void waitInvFinishtimerInit(int duration)
        {
            //m_timer_count = m_count;
            //timerInventory.Interval = 1000;
            //timerInventory.Start();
            //timerInv.Enabled = true;
            if (invWaitTimer != null)
            {
                invWaitTimer.Close();
                invWaitTimer = null;
            }
            if (invWaitTimer == null)
            {
                invWaitTimer = new System.Timers.Timer(duration * 1000);
                
                invWaitTimer.Elapsed += new System.Timers.ElapsedEventHandler(waitInvFinishtimeout);
                writeLog("new wait inv finish duration is " + duration * 1000 + "ms");
            }
            writeLog("wait inv finish duration is " + duration * 1000 + "ms");
            invWaitTimer.AutoReset = false;
            invWaitTimer.Enabled = true;
        }
        private void clearInvFinishTimer()
        {
            if (invWaitTimer != null)
            {
                invWaitTimer.Enabled = false;
                invWaitTimer.Close();
                invWaitTimer = null;
                writeLog("clear wait inv timer");
            }
        }
        System.Timers.Timer invTimer;
        private void timerInit(Int32 duration)
        {
            //m_timer_count = m_count;
            //timerInventory.Interval = 1000;
            //timerInventory.Start();
            //timerInv.Enabled = true;
            if (invTimer != null)
            {
                invTimer.Close();
                invTimer = null;
            }
            if (invTimer == null)
            {
                invTimer = new System.Timers.Timer(duration * 1000);

                invTimer.Elapsed += new System.Timers.ElapsedEventHandler(timeout);
                writeLog("new timer inv duration is " + duration * 1000 + "ms");
            }
            writeLog("inv duration is " + duration * 1000 + "ms");
            invTimer.AutoReset = false;
            invTimer.Enabled = true;
        }
        private void startCollectDocEpc()
        {
            writeLog("start to collect epc");
            //reader.ClearCom();
            if (conf.operaMode == (byte)ReaderConfig.OPERA_MODE.SECURITY)
            {
                writeLog("security start to collect epc");
                ClearLog(logRichText);
                timerInit(conf.SecureInvDuration);

                startRealInv();
            }
            else if (conf.operaMode == (byte)ReaderConfig.OPERA_MODE.STORAGE)
            {
                writeLog("storage start to collect epc");
                ClearLog(logRichText);
                timerInit(conf.InvDuration);
                startBufferInv();
            }
            else
            {
                //do nothing
            }
            writeLog("Inventory session finish");
        }
    
        private void CloseReader()
        {
            //处理串口断开连接读写器
            //reader.CloseCom();
        }

        //set and get output power
        private void setOutputPower(byte power)
        {
            reader.SetOutputPower(m_curSetting.btReadId, power);
            m_curSetting.btOutputPower = power;
        }
        private void ProcessSetOutputPower(Reader.MessageTran msgTran)
        {
            string strCmd = "设置输出功率";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    //WriteLog(logRichText, strCmd, 0);

                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            writeLog(strLog);
        }
        private void getOutputPower()
        {
            reader.GetOutputPower(m_curSetting.btReadId);
        }
        private delegate void showShellUnsafe(byte cmd);
        private void showShell(byte cmd)
        {
            if (this.InvokeRequired)
            {
                showShellUnsafe InvokeRefresh = new showShellUnsafe(showShell);
                this.Invoke(InvokeRefresh, new object[] { cmd });
            }
            else
            {
                switch (cmd)
                {
                case 0x77:
                    string str = "输出功率";
                    str += m_curSetting.btOutputPower;
                    str += "dbm";
                    WriteLog(logRichText, str, 0);
                    break;

                }
            }
        }
        private void ProcessGetOutputPower(Reader.MessageTran msgTran)
        {
            string strCmd = "取得输出功率";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btOutputPower = msgTran.AryData[0];
                strCmd += m_curSetting.btOutputPower;
                //showShell(0x77);
                //RefreshReadSetting(0x77);
                //WriteLog(logRichText, strCmd, 0);
                return;
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            writeLog(strLog);
        }

        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            //CloseReader();
        }

#if false
        private int m_timer_count = 0;
        private void progress_init()
        {
            m_timer_count = m_count;
            progressBarInv.Maximum = m_count * 10;
            progressBarInv.Value = 0;
            timerInv.Enabled = true;
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (m_timer_count == 0)
            {
                timerInv.Enabled = false;
                stopRealInv();
                buttonInv.Enabled = true;
                return;
            }
            progressBarInv.Value += progressBarInv.Maximum / m_count;
            m_timer_count--;
        }
#endif
        /************************************************************************/
        /* PR9000 */
        private PR9000 pr9000;
        private bool isContinuous = false;
        DistinctInvBuffer personEpc = new DistinctInvBuffer();
        private string stuffEpc = "";
        private bool initPr9000()
        {
            string strException = string.Empty;
            string strComPort = conf.PR9000Port;
            //int nBaudrate = Convert.ToInt32(cmbBaudrate.Text);
            int nBaudrate = 19200;
            pr9000 = new PR9000();
            pr9000.AnalyCallback = analyData;
            int nRet = pr9000.OpenCom(strComPort, out strException);
            if (nRet != 0)
            {
                string strLog = "连接读卡器失败，失败原因： " + strException;
                WriteLog(logRichText, strLog, 1);
                writeLog(strLog);
                m_readerIsNormal = true;
                //buttonInv.Enabled = false;
                return false;
            }
            else
            {
                string strLog = "连接读卡器 " + strComPort + "@" + nBaudrate.ToString();
                WriteLog(logRichText, strLog, 0);
                writeLog(strLog);
            }
            
            return true;
        }
        private void enablePr9000Inv()
        {
            pr9000.SendStartInvCmd();
        }
        private void disablePr9000Inv()
        {
            pr9000.SendStopInvCmd();
        }
        private void enablePr9000CtsInv()
        {
            isContinuous = true;
            enablePr9000Inv();
        }
        private void disablePr9000CtsInv()
        {
            isContinuous = false;
            disablePr9000Inv();
        }
        private bool inUploadSession = false;
        void analyData(PR9000.CALLBACK_MSG_TYPE msgType, byte arg, byte[] payload)
        {
            string strLog = "";
            switch (msgType)
            {
                case PR9000.CALLBACK_MSG_TYPE.POWER_ON:
                    if (arg == 0)
                    {
                        pr9000.setPowerOnState(true);
                        strLog = "PR9000 power on ok";
                    }
                    else
                    {
                        pr9000.setPowerOnState(false);
                        strLog = "PR9000 power on fail";
                    }
                    //WriteLog(logRichText, strLog, 0);
                    break;
                case PR9000.CALLBACK_MSG_TYPE.START_INV:
                    if (arg == 0)
                    {
                        pr9000.setInvState(true);
                        strLog = "PR9000 start INV";
                        //WriteLog(logRichText, strLog, 0);
                    }                    
                    
                    break;
                case PR9000.CALLBACK_MSG_TYPE.STOP_INV:
                    if (arg == 0)
                    {
                        pr9000.setInvState(false);
                        strLog = "PR9000 stop INV";
                        //WriteLog(logRichText, strLog, 0);
                    }
                    break;
                case PR9000.CALLBACK_MSG_TYPE.EPC:
                    //strLog = "EPC:" + CCommondMethod.ByteArrayToString(payload, 0, payload.Length);
                    if (!inUploadSession)
                    {
                        if (payload[2] == 0x23 && payload[3] == 0x23 && payload[4] == 0x55)
                        {
                            inUploadSession = true;
                            disablePr9000Inv();
                            pr9000.ClearBuffer();
                            //verify the epc is stuff 
                            strLog = "识别到人员标签";
                            WriteLog(logRichText, strLog, 0);
                            writeLog(strLog);
                            //remove CRC first
                            stuffEpc = CCommondMethod.ByteArrayToStringNoBlank(payload, 0, payload.Length-2);
                            writeLog("Stuff Epc:" + stuffEpc);
                            epcIndex = 0;
                            EpcCollections[epcIndex] = stuffEpc;
                            epcIndex++;
                            //m_invBuffer.collectEpc(stuffEpc);
                            //string tmpEpc = CCommondMethod.ByteArrayToStringNoBlank(payload, 2, payload.Length - 4);
                            //remove 0x3000 head
                            string tmpEpc = stuffEpc.Substring(4, stuffEpc.Length - 4);
                            //writeLog("temp Epc:" + stuffEpc);
                            if (sc.checkUsrInfo(tmpEpc) == 0)
                            {
                                updateUserInfo();
                            }
                            startCollectDocEpc();

                            pr9000.ClearBuffer();
                        }
                    }
                    //WriteLog(logRichText, strLog, 0);
                    break;
                case PR9000.CALLBACK_MSG_TYPE.INV_FINISH:
                    strLog = "Inventory finish";
                    //WriteLog(logRichText, strLog, 0);
                    if (isContinuous)
                    {
                        enablePr9000CtsInv();
                    }
                    else
                    {
                        disablePr9000CtsInv();
                    }
                    break;
                default:
                    break;
            }
        }
        private delegate void updateUserInfoUnSafe();
        private void updateUserInfo()
        {
            if (this.InvokeRequired)
            {
                updateUserInfoUnSafe InvokeWriteLog = new updateUserInfoUnSafe(updateUserInfo);
                this.Invoke(InvokeWriteLog);
            }
            else
            {
                try
                {
                    string userId = String.Format("{0:D6}", Convert.ToInt32(sc.usrId));
                    textBoxStuffNum.Text = userId;
                }
                catch (SystemException ex)
                {
                    writeLog("Convert User ID fail " + ex.Message);
                }
                textBoxStuffName.Text = sc.usrName;
            }
            writeLog("user name: " + sc.usrName);
        }
        /************************************************************************/
        /* server communication module */
        private bool initServerComm()
        {
            sc = new ServerComm(conf.BasePath, conf.BaseUserPath);
            sc.MessageReceived += new MsgHandler(processServerResp);
            return true;
        }
        private void processServerResp(byte msg)
        {
            string strLog = "";
            switch (msg)
            {
                case 0:
                    strLog += "发送成功";
                    WriteLog(logRichText, strLog, 0);
                    writeLog(strLog);
                    break;
                case 1:
                    strLog += "发送失败";
                    WriteLog(logRichText, strLog, 0);
                    writeLog(strLog);
                    break;
                default:
                    break;
            }
        }

        /************************************************************************/
        /* Infrared */
        private infrared inf;
        private bool infraredIsAlive = false;
        private bool initInfrared()
        {
            string strException = string.Empty;
            string strComPort = conf.InfraredPort;
            string nBaudrate = "9600";
            inf = new infrared();
            inf.callback = onInfraredEvent;
            int nRet = inf.OpenCom(strComPort, out strException);
            if (nRet != 0)
            {
                string strLog = "连接红外设备失败，失败原因： " + strException;
                WriteLog(logRichText, strLog, 1);
                writeLog("open infrared port fail");
                return false;
            }
            else
            {
                string strLog = "连接红外设备 " + strComPort + "@" + nBaudrate.ToString();
                WriteLog(logRichText, strLog, 0);
                writeLog("open infrared port ok");
            }
            
            return true;
        }
        private System.Timers.Timer infraredTimer;
        private void infraredTimerInit()
        {
            infraredTimer = new System.Timers.Timer(conf.InfraredFreq * 100);
            writeLog("Infrared T is " + conf.InfraredFreq * 100 + "ms");
            infraredTimer.Elapsed += new System.Timers.ElapsedEventHandler(infraredTimeout);
            infraredTimer.AutoReset = true;
            infraredTimer.Enabled = true;
        }
        private void infraredTimeout(object source, System.Timers.ElapsedEventArgs e)
        {
            inf.SendPollingCmd();
        }
        private void onInfraredEvent(byte[] payload)
        {
            infraredIsAlive = true;
            string str = System.Text.Encoding.ASCII.GetString(payload);
            string temp = str.Substring(2, 2);
            byte status = (byte)Convert.ToByte(temp, 16);
            status = (byte)~status;
            for (byte i = 0; i < 8; i++)
            {
                if ((status & (1 << i)) != 0)
                {
                    infraredTimer.Enabled = false;
                    string strLog = "检测到红外线端口" + i;
                    WriteLog(logRichText, strLog, 0);
                    startCollectDocEpc();
                    break;
                }
            }
            //byte[] status = new byte[2];
            //Array.Copy(payload, 2, status, 0, status.Length);
            //string str = System.Text.Encoding.Default.GetString(status);
            //byte 
        }

        /***********************************************************************
         * LED control module
         * ********************************************************************/
        private LedControl ledCtrl;
        private bool ledIsAlive = false;
        bool initLed()
        {
            string strException = string.Empty;
            string strComPort = conf.LedControlPort;
            string nBaudrate = "9600";
            ledCtrl = new LedControl();
            ledCtrl.callback = onLedControlResp;
            int nRet = ledCtrl.OpenCom(strComPort, out strException);
            if (nRet != 0)
            {
                string strLog = "连接LED设备失败，失败原因： " + strException;
                WriteLog(logRichText, strLog, 1);
                writeLog("open infrared port fail");
                return false;
            }
            else
            {
                string strLog = "连接LED设备 " + strComPort + "@" + nBaudrate.ToString();
                WriteLog(logRichText, strLog, 0);
                writeLog("open infrared port ok");
            }
            ledCtrl.closeLed();
            //ledTimerInit();
            return true;
        }
        private void onLedControlResp(byte[] payload)
        {
            ledIsAlive = true;
        }
        private bool ledIsOpen = false;
        private bool ledStartSwitch = false;
        private System.Timers.Timer ledSwitchTimer;

        private void startLedAlarm()
        {
            ledCtrl.openLed();
            ledTimerInit();
            
        }
        private void startSwitchLed()
        {
            ledSwitchTimer.Enabled = true;
            ledStartSwitch = true;
        }
        private void stopSwitchLed()
        {
            ledStartSwitch = false;
            ledSwitchTimer.Enabled = false;
            ledCtrl.closeLed();
        }
        private void ledTimerInit()
        {
            if (ledSwitchTimer == null)
            {
                ledSwitchTimer = new System.Timers.Timer(conf.LedSwitchFreq * 1000);

                ledSwitchTimer.Elapsed += new System.Timers.ElapsedEventHandler(ledSwitchTimeout);

                writeLog("led switch T is " + conf.LedSwitchFreq * 1000 + "ms");
            }
            ledSwitchTimer.AutoReset = false;
            ledSwitchTimer.Enabled = true;
        }
        private void ledSwitchTimeout(object source, System.Timers.ElapsedEventArgs e)
        {
#if false
            if (!ledIsOpen)
            {
                ledCtrl.openLed();
                ledIsOpen = true;
            }
            else
            {
                ledCtrl.closeLed();
                ledIsOpen = false;
            }
            if (!ledStartSwitch)
            {
                ledCtrl.closeLed();
            }
#endif
            ledCtrl.closeLed();
            ledCtrl.closeLed();
        }
        private bool open = false;
        private void button1_Click_1(object sender, EventArgs e)
        {
            if (!open)
            {
                startSwitchLed();
                open = true;
            }
            else
            {
                stopSwitchLed();
                open = false;
            }
        }

        private void button1_Click_2(object sender, EventArgs e)
        {
#if false
            string epcFileNameTest = "Epc12345.dat";
            if (sc.sendEpc(epcFileNameTest) == 0)
                writeLog("send epc " + epcFileNameTest + " ok");

            string userEpc = "232355000000000000000001";
            if (sc.checkUsrInfo(userEpc) == 0)
            {
                updateUserInfo();
            }
#endif
            updateUserInfo();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            button1_Click_2(sender, e);
        }
        
        [DllImport("user32.dll")]  //需添加using System.Runtime.InteropServices
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MOVE = 0xF010;
        public const int HTCAPTION = 0x0002;  
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            //ReleaseCapture();
            //SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
        }

        private void pictureBoxExit_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label_weeknum_Click(object sender, EventArgs e)
        {

        }

        private void label_date_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void labelTitle_Click(object sender, EventArgs e)
        {

        }

        private void timerInventory_Tick(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void pictureBoxBackG_Click(object sender, EventArgs e)
        {

        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void textBoxStuffName_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxStuffNum_TextChanged(object sender, EventArgs e)
        {

        }

        private void panel5_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void panel6_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void panel7_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            startCollectDocEpc();
        }

        private void button1_Click_3(object sender, EventArgs e)
        {
            startCollectDocEpc();
        }

        private void button1_Click_4(object sender, EventArgs e)
        {
            //startCollectDocEpc();
            Form_messagebox.show("是否继续");
        }

        private void button1_Click_5(object sender, EventArgs e)
        {
            Form_messagebox.show();
            //Form_messagebox.show("是否继续");
            //Form_messagebox.show("强制发送失败");
            Form_messagebox.show("√ 操作成功");
            startCollectDocEpc();
        }

        private void button1_Click_6(object sender, EventArgs e)
        {
            startCollectDocEpc();
        }
        
   }
}
