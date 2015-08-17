using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace CommonCtrl
{
    public delegate void MsgHandler(byte msg);
    public class ServerComm
    {
        public event MsgHandler MessageReceived;

        private static string curDir = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            //System.Windows.Forms.Application.StartupPath;
        private static string baseDir = curDir;
        private static string userPath = curDir;
        private static string sendBat = baseDir + "\\dist\\bin\\uploadFile.bat";
        private static string forceSendBat = baseDir + "\\dist\\bin\\forceUploadFile.bat";
        private static string checkUsrBat = baseDir + "\\dist\\bin\\checkUser.bat";
        private static string usrResult = baseDir + "\\dist\\user\\user.result";
        private static string uploadWarnLog = baseDir + "\\dist\\bin\\uploadWarnLog.bat";
        public string usrName = "Unknown";
        public string usrId = "1";
        public ServerComm(string path, string userRetPath)
        {
            baseDir = path;
            userPath = userRetPath;
            sendBat = baseDir + "\\uploadFile.bat";
            forceSendBat = baseDir + "\\forceUploadFile.bat";
            checkUsrBat = baseDir + "\\checkUser.bat";
            usrResult = userPath + "\\user.result";
            uploadWarnLog = baseDir + "\\uploadWarnLog.bat";
        }
        public int sendEpc(string epcDatName)
        {
            string epcDatFullPath = curDir + "\\" + epcDatName;
            if (existDatFile(epcDatFullPath))
            {
                if (execBatFile(sendBat, epcDatFullPath) != 0)
                //if (execBatFileNoParams(sendBat) != 0)
                    return -1;
                Thread.Sleep(500);
                //if (startPollingDatFile(epcDatFullPath) != 0)
                if (existDatFile(epcDatFullPath))
                    return -2;
                return 0;
            }
            return -3;
        }
        public int sendWarnLog(string epcDatName)
        {
            string epcDatFullPath = curDir + "\\" + epcDatName;
            if (existDatFile(epcDatFullPath))
            {
                if (execBatFile(uploadWarnLog, epcDatFullPath) != 0)
                    return -1;
                //if (startPollingDatFile(epcDatFullPath) != 0)
                if (existDatFile(epcDatFullPath))
                    return -1;
                return 0;
            }
            return -1;
        }
        public int forceSendEpc(string epcDatName)
        {
            string epcDatFullPath = curDir + "\\" + epcDatName;
            if (existDatFile(epcDatFullPath))
            {
                if (execBatFile(forceSendBat, epcDatFullPath) != 0)
                    return -1;
                if (existDatFile(epcDatFullPath))
                    return -1;
                return 0;
            }
            return -1;
        }
        public int checkUsrInfo(string epc)
        {
            if (execBatFile(checkUsrBat, epc) == 0)
            {
                try
                {
                    FileStream aFile = new FileStream(usrResult, FileMode.Open);
                    StreamReader sr = new StreamReader(aFile);
                    usrId = sr.ReadLine();
                    usrName = sr.ReadLine();
                    sr.Close();
                    aFile.Close();
                    return 0;
                }
                catch (IOException ex)
                {
                    return -1;
                }
            }
            return -1;
        }
        public void deleteEpcFile(string epcDatName)
        {
            string epcDatFullPath = curDir + "\\" + epcDatName;
            if (File.Exists(epcDatFullPath))
            {
                try
                {
                    File.Delete(epcDatFullPath);
                }
                catch (SystemException ex)
                {
                }
            }
        }
        private bool existDatFile(string fileName)
        {
            return File.Exists(fileName);
        }
        private int execBatFileNoParams(string batName)
        {
            try
            {
                Process process = new Process();
                FileInfo bat = new FileInfo(batName);
                ProcessStartInfo pi = new ProcessStartInfo(batName);
                //pi.CreateNoWindow = true;
                process.StartInfo = pi;
                //process.StartInfo.CreateNoWindow = true;
                //process.StartInfo.WorkingDirectory = bat.Directory.FullName;
                //process.StartInfo.FileName = batName;
                process.Start();
                process.WaitForExit();
            }
            catch (SystemException e)
            {
                Console.WriteLine("exec bat error: " + e.Message);
                return -1;
            }
            return 0;
        }
        private int execBatFile(string batName, string datName)
        {
            try
            {
                Process process = new Process();
                FileInfo bat = new FileInfo(batName);
                ProcessStartInfo pi = new ProcessStartInfo(batName, datName);
                pi.CreateNoWindow = true;
                pi.WindowStyle = ProcessWindowStyle.Hidden;
                pi.ErrorDialog = false;
                process.StartInfo = pi;
                //process.StartInfo.CreateNoWindow = true;
                //process.StartInfo.WorkingDirectory = bat.Directory.FullName;
                //process.StartInfo.FileName = batName;
                process.Start();
                process.WaitForExit();
            }
            catch (SystemException e)
            {
                Console.WriteLine("exec bat error: " + e.Message);
                return -1;
            }
            return 0;
        }
        private Thread waitThread;
        private int startPollingDatFile(string datName)
        {
            try
            {
                ParameterizedThreadStart stThead = new ParameterizedThreadStart(polling);
                waitThread = new Thread(stThead);
                waitThread.IsBackground = true;
                waitThread.Start(datName);
            }
            catch (SystemException e)
            {
                Console.WriteLine("start polling thread error: " + e.Message);
                return -1;
            }
            return 0;
        }
        private void polling(object o)
        {
            int count = 10; //5s
            while (count > 0)
            {
                if (existDatFile(o.ToString()))
                    break;
                Thread.Sleep(500);
            }
            if (count > 0)
            {
                MessageReceived(1); //dat exists
            }
            else
            {
                MessageReceived(0); //dat is processed
            }
        }
    }
}
