using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UHFDemo
{
    public partial class Form_messagebox : Form
    {
       static private int ret = 0;
       static private Form_messagebox newMsgBox;
        public Form_messagebox()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
        }

        private void Form3_Load(object sender, EventArgs e)
        {

        }
        public void showInfo(string str)
        {
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.label1.Text = str;
            setLabel1Pos();
            this.label2.Visible = false;
            this.button1.Visible = false;
            this.button2.Visible = false;
            this.button3.Visible = false;
            //SetCursor(LoadCursor(null, IDC_WAIT));
            try
            {
                this.ShowDialog();
            }
            catch (SystemException ex)
            {
                string err = ex.Message;
            }
        }
        private int barDuration = 0;
        private int remain = 0;
        private int count = 0;
        System.Timers.Timer barTimer;
        private void updateBar(Object source, System.Timers.ElapsedEventArgs e)
        {
            this.progressBar1.Value = count * (100 - remain) / barDuration + 8;
            if (count < barDuration)
            {
                count++;
            }
            else
            {
                barTimer.AutoReset = false;
                barTimer.Enabled = false;
                //count = 0;
            }
        }
        private void setLabel1Pos()
        {
            int x = (panel3.Right + panel3.Left) / 2 - label1.Width / 2;
                //(.Right + textBoxStuffNum.Left) / 2 - label2.Width / 2;
            int y = this.label1.Location.Y;
            this.label1.Location = new System.Drawing.Point(x, y);
        }
        static private void setLabel1PosStatic()
        {
            int x = (newMsgBox.panel3.Right + newMsgBox.panel3.Left) / 2 - newMsgBox.label1.Width / 2;
            //(.Right + textBoxStuffNum.Left) / 2 - label2.Width / 2;
            int y = newMsgBox.label1.Location.Y;
            newMsgBox.label1.Location = new System.Drawing.Point(x, y);
        }
        private void barTimerInit()
        {
            if (barTimer == null)
            {
                barTimer = new System.Timers.Timer(1000);
                barTimer.Elapsed += new System.Timers.ElapsedEventHandler(updateBar);
            }
            barTimer.AutoReset = true;
            barTimer.Enabled = true; 
        }
        public void showInfo(string str, int duration)
        {
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.label1.Text = str;
            setLabel1Pos();
            this.label2.Visible = false;
            this.button1.Visible = false;
            this.button2.Visible = false;
            this.button3.Visible = false;
            this.barDuration = duration;
            this.progressBar1.Maximum = 100;
            this.progressBar1.Value = 0;
            count = 0;
            barTimerInit();
            //SetCursor(LoadCursor(null, IDC_WAIT));
            try
            {
                this.ShowDialog();
            }
            catch (SystemException ex)
            {
                string err = ex.Message;
            }
        }
        public void setBarValue(int val)
        {
            if (this.progressBar1.Value < 100)
                this.progressBar1.Value += val;
        }
        public int getBarRemain()
        {
            return (this.progressBar1.Maximum - this.progressBar1.Value);
        }
        public void showInfoWithBar(string str, int duration, int remain)
        {
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.label1.Text = str;
            setLabel1Pos();
            this.label2.Visible = false;
            this.button1.Visible = false;
            this.button2.Visible = false;
            this.button3.Visible = false;
            this.barDuration = duration;
            this.remain = remain;
            this.progressBar1.Maximum = 100;
            this.progressBar1.Value = 8;
            count = 0;
            barTimerInit();
            //SetCursor(LoadCursor(null, IDC_WAIT));
            try
            {
                this.ShowDialog();
            }
            catch (SystemException ex)
            {
                string err = ex.Message;
            }
        }
        public void diposeInfo()
        {
            
            this.Close();
        }
        static public int show()
        {
            newMsgBox = new Form_messagebox();
            newMsgBox.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            newMsgBox.button3.Visible = false;
            newMsgBox.progressBar1.Visible = false;
            newMsgBox.ShowDialog();
            return ret;
        }
        static public int show(string title)
        {
            //if (isInfo)
            {
                newMsgBox = new Form_messagebox();
                newMsgBox.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                newMsgBox.label2.Visible = false;
                newMsgBox.button1.Visible = false;
                newMsgBox.button2.Visible = false;
                newMsgBox.button3.Visible = true;
                //newMsgBox.button2.Text = "确认";
                newMsgBox.progressBar1.Visible = false;
                newMsgBox.label1.Text = title;
                setLabel1PosStatic();
                newMsgBox.ShowDialog();
                //Thread.Sleep(1000);
            }
            return ret;
        }
       
        static public int show(string title, bool needButton)
        {
            if (needButton)
            {
                show(title);
            }
            else
            {
                newMsgBox = new Form_messagebox();
                newMsgBox.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                newMsgBox.label2.Visible = false;
                newMsgBox.button1.Visible = false;
                newMsgBox.button2.Visible = false;
                newMsgBox.button3.Visible = false;
                //newMsgBox.button2.Text = "确认";
                newMsgBox.TopMost = true;
                newMsgBox.label1.Text = title;
                setLabel1PosStatic();
                newMsgBox.ShowDialog();
            }
            return ret;
        }

        static public void dispose()
        {
            newMsgBox.Dispose();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            ret = -1;
            newMsgBox.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ret = 0;
            newMsgBox.Dispose();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ret = 0;
            newMsgBox.Dispose();
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.TopMost = false;
            this.BringToFront();
            this.TopMost = true;
        }
    }
}
