using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;

namespace UHFDemo
{
    public partial class MyOKMessageBox : Form
    {
        static private int ret = 0;
        static private MyOKMessageBox newMsgBox;
        public MyOKMessageBox()
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
            this.button1.Visible = false;
            this.button2.Visible = false;
            this.button3.Visible = false;
            //SetCursor(LoadCursor(null, IDC_WAIT));
            this.ShowDialog();
        }
        public void diposeInfo()
        {
            
            this.Close();
        }
        static public int show()
        {
            newMsgBox = new MyOKMessageBox();
            newMsgBox.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            newMsgBox.button3.Visible = false;
            newMsgBox.ShowDialog();
            return ret;
        }
        static public int show(string title)
        {
            //if (isInfo)
            {
                newMsgBox = new MyOKMessageBox();
                newMsgBox.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                newMsgBox.button1.Visible = false;
                newMsgBox.button2.Visible = false;
                newMsgBox.button3.Visible = true;
                //newMsgBox.button2.Text = "确认";
                
                newMsgBox.label1.Text = title;
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
                newMsgBox = new MyOKMessageBox();
                newMsgBox.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                newMsgBox.button1.Visible = false;
                newMsgBox.button2.Visible = false;
                newMsgBox.button3.Visible = false;
                //newMsgBox.button2.Text = "确认";
                newMsgBox.TopMost = true;
                newMsgBox.label1.Text = title;
                
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

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
