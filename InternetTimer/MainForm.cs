using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using InternetTimer.Properties;

namespace InternetTimer {

    public partial class MainForm : Form {
        /// <summary>
        /// Timer Update (every 1 sec)
        /// </summary>
        private readonly int timerUpdate = 1000;

        /// <summary>
        /// Interface Storage
        /// </summary>
        private NetworkInterface[] nicArr;

        /// <summary>
        /// Main Timer Object 
        /// </summary>
        private Timer timer;
        private long bytesSent;
        private long bytesReceived;
        private int bytesSentSpeed;
        private int bytesReceivedSpeed;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainForm() {
            InitializeComponent();
            InitializeSettings();
            InitializeNetworkInterface();
            InitializeTimer();
        }

        void InitializeSettings() {
            ShutdownPC.Checked = Settings.Default.Выключить;
            linkCheck.Checked = Settings.Default.Выполнить;
            link.Text = Settings.Default.Ссылка;
            limitKbSec.Value = Settings.Default.Лимит;
            intervalMin.Value = Settings.Default.Минуты;

            Application.ApplicationExit += new EventHandler(SaveSettings);
        }


        void SaveSettings(object sender = null, EventArgs e = null) {

            Settings.Default.Выключить = ShutdownPC.Checked;
            Settings.Default.Выполнить = linkCheck.Checked;
            Settings.Default.Ссылка = link.Text;
            Settings.Default.Лимит = limitKbSec.Value;
            Settings.Default.Минуты = intervalMin.Value;
            Settings.Default.Индекс = cmbInterface.SelectedIndex;

            Settings.Default.Save();
        }



        /// <summary>
        /// Initialize all network interfaces on this computer
        /// </summary>
        private void InitializeNetworkInterface() {

            nicArr = NetworkInterface.GetAllNetworkInterfaces();


            for (int i = 0; i < nicArr.Length; i++)
                cmbInterface.Items.Add(nicArr[i].Name);

            cmbInterface.SelectedIndex = Settings.Default.Индекс < nicArr.Length ? Settings.Default.Индекс : 0;
        }

        private void InitializeTimer() {
            timer = new Timer {
                Interval = (int)timerUpdate
            };
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Start();
        }

        private void UpdateNetworkInterface() {
            IPv4InterfaceStatistics interfaceStats = nicArr[cmbInterface.SelectedIndex].GetIPv4Statistics();

            bytesSentSpeed = (int)(interfaceStats.BytesSent - bytesSent) / 1024;
            bytesReceivedSpeed = (int)(interfaceStats.BytesReceived - bytesReceived) / 1024;

            bytesSent = interfaceStats.BytesSent;
            bytesReceived = interfaceStats.BytesReceived;

            lblUpload.Text = bytesSentSpeed.ToString() + " KB/s";
            lblDownload.Text = bytesReceivedSpeed.ToString() + " KB/s";

        }

        /// <summary>
        /// The Timer event for each Tick (second) to update the UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Timer_Tick(object sender, EventArgs e) {
            UpdateNetworkInterface();
            UpdateLimit();
        }





        bool timerStatus = false;
        private Stopwatch stopwatch;

        void UpdateLimit() {

            if (bytesReceivedSpeed > limitKbSec.Value && timerStatus == true) {
                TimerStop();
            }
            else if (bytesReceivedSpeed <= limitKbSec.Value && timerStatus == false) {
                TimerStart();
            }


            if (timerStatus) {
                timerValue.Text = ((float)intervalMin.Value - stopwatch.ElapsedMilliseconds / 60000f).ToString();

                var valBar = (int)(stopwatch.ElapsedMilliseconds / (intervalMin.Value * 60000) * progressBar.Maximum);
                valBar = Math.Min(progressBar.Maximum, valBar);
                progressBar.Value = valBar;

                if (stopwatch.ElapsedMilliseconds >= intervalMin.Value * 60000) {
                    ApplyAction();
                }
            }
        }


        void TimerStart() {
            timerStatus = true;

            stopwatch = Stopwatch.StartNew();

        }

        void TimerStop() {
            timerStatus = false;

            stopwatch.Reset();

            timerValue.Text = "∞";
            progressBar.Value = 0;
        }


        void ApplyAction() {
            TimerStop();
            if (linkCheck.Checked) {
                Process.Start(link.Text);

            }
            if (ShutdownPC.Checked) {
                //Process.Start("shutdown", "/s /t /f 00");
                //SaveSettings();
                Process.Start("cmd", "/c shutdown -s -f -t 00");

                Application.Exit();
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            Settings.Default.Reset();
            InitializeSettings();
        }
    }
}
