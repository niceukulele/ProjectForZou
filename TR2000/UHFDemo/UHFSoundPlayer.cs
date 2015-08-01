using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Media;
using System.IO;
namespace UHFDemo
{
    class UHFSoundPlayer
    {
        private System.Media.SoundPlayer player;
        //debug mode the path is incorrect
        private String alarmWavPath = Directory.GetCurrentDirectory() + @"\alarm.wav";
        public UHFSoundPlayer()
        {
            player = new System.Media.SoundPlayer();
        }
        public void playAlarm()
        {

            player.SoundLocation = alarmWavPath;
            _play();
        }
        public void playAlarmLoop()
        {
            player.SoundLocation = alarmWavPath;
            try
            {
                player.Load();
                player.PlayLooping();
            }
            catch (SystemException ex)
            {
                Console.WriteLine("Play alarm loop error " + ex.Message);
            }
        }
        private void _play()
        {
            try
            {
                //player.SoundLocation = alarmWavPath;
                player.Load();
                player.Play();
            }
            catch (SystemException ex)
            {
                Console.WriteLine("Play alarm error " + ex.Message);
            }
        }
        public void stopAlarmLoop()
        {
            player.Stop();
        }
        private bool onPlay = false;
        public void playAlarmTime(int time)
        {
            if (!onPlay)
            {
                onPlay = true;
                System.Timers.Timer t = new System.Timers.Timer(time * 1000);
                t.Elapsed += new System.Timers.ElapsedEventHandler(timeout);
                t.AutoReset = false;
                t.Enabled = true;
                playAlarmLoop();
            }
        }
        private void timeout(object source, System.Timers.ElapsedEventArgs e)
        {
            stopAlarmLoop();
            onPlay = false;
        }
    }
}
