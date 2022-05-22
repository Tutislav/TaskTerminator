using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Specialized;

namespace TaskTerminator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        List<Process> processesList;
        List<Process> currentProcessesList;
        OrderedDictionary processesCpuCounters;
        Thread update;
        PerformanceCounter cpuCounter;
        PerformanceCounter ramCounter;
        double ramTotalMB;
        double ramAvailableMB;

        private void process_Add(Process process)
        {
            String processName = process.ProcessName;
            processesCpuCounters.Add(process.Id.ToString(), new PerformanceCounter("Process", "% Processor Time", processName));
            if (process.MainWindowTitle != "") processName = process.MainWindowTitle;
            String processStatus = "running";
            if (!process.Responding) processStatus = "not responding";
            ListViewItem processInfo = new ListViewItem(processName);
            processInfo.SubItems.Add(processStatus);
            processInfo.SubItems.Add("");
            processInfo.SubItems.Add("");
            processInfo.SubItems.Add(process.Id.ToString());
            listView.Invoke(new Action(() =>
            {
                if (process.MainWindowHandle != IntPtr.Zero) listView.Items.Insert(0, processInfo);
                else listView.Items.Add(processInfo);
            }));
        }

        private void process_Update(Process process)
        {
            String processName = process.ProcessName;
            PerformanceCounter processCpu = (PerformanceCounter)processesCpuCounters[process.Id.ToString()];
            String processCpuUsage = Math.Round(processCpu.NextValue() / Environment.ProcessorCount, 1) + "%";
            if (process.MainWindowTitle != "") processName = process.MainWindowTitle;
            String processStatus = "running";
            if (!process.Responding) processStatus = "not responding";
            listView.Invoke(new Action(() =>
            {
                ListViewItem processInfo = listView.Items.Cast<ListViewItem>().Where(l => l.SubItems[l.SubItems.Count - 1].Text == process.Id.ToString()).First();
                if (processInfo.Text != processName) processInfo.Text = processName;
                if (processInfo.SubItems[1].Text != processStatus) processInfo.SubItems[1].Text = processStatus;
                if (processInfo.SubItems[2].Text != processCpuUsage) processInfo.SubItems[2].Text = processCpuUsage;
            }));
        }

        private void process_Remove(Process process)
        {
            listView.Invoke(new Action(() =>
            {
                listView.Items.Remove(listView.Items.Cast<ListViewItem>().Where(l => l.SubItems[l.SubItems.Count - 1].Text == process.Id.ToString()).First());
            }));
            processesCpuCounters.Remove(process.Id.ToString());
        }

        private void updateListView()
        {
            ramAvailableMB = ramCounter.NextValue();
            statusStrip.Items[0].Text = "CPU: " + Math.Round(cpuCounter.NextValue(), 1) + "%";
            statusStrip.Items[1].Text = "RAM: " + Math.Round((ramTotalMB - ramAvailableMB) / ramTotalMB * 100, 1) + "%";
            currentProcessesList = Process.GetProcesses().ToList();
            foreach (Process process in currentProcessesList)
            {
                if (!processesList.Exists(p => p.Id == process.Id))
                {
                    process_Add(process);
                }
                else
                {
                    process_Update(process);
                }
            }
            foreach (Process process in processesList)
            {
                if (!currentProcessesList.Exists(p => p.Id == process.Id))
                {
                    process_Remove(process);
                }
            }
            processesList = new List<Process>(currentProcessesList);
        }

        private String cmd(String command, bool asAdmin=false, bool waitForExit=false)
        {
            Process process = new Process();
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "cmd.exe";
            processStartInfo.Arguments = "/C " + command;
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            if (asAdmin) processStartInfo.Verb = "runas";
            if (waitForExit)
            {
                processStartInfo.UseShellExecute = false;
                processStartInfo.RedirectStandardOutput = true;
            }
            process.StartInfo = processStartInfo;
            process.Start();
            String output = "";
            if (waitForExit)
            {
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            return output;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            processesList = Process.GetProcesses().ToList();
            processesCpuCounters = new OrderedDictionary();
            update = new Thread(updateListView);
            foreach (Process process in processesList)
            {
                listView.Invoke(new Action(() => process_Add(process)));
            }
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            ramTotalMB = double.Parse(new String(cmd("wmic computersystem get totalphysicalmemory", false, true).Where(Char.IsDigit).ToArray())) / 1024 / 1024;
            ramAvailableMB = ramCounter.NextValue();
            statusStrip.Items.Add("CPU: " + Math.Round(cpuCounter.NextValue(), 1) + "%");
            statusStrip.Items.Add("RAM: " + Math.Round((ramTotalMB - ramAvailableMB) / ramTotalMB * 100, 1) + "%");
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (!update.IsAlive)
            {
                update = new Thread(updateListView);
                update.Start();
            }
        }

        private void listView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (listView.FocusedItem.Bounds.Contains(e.Location))
                {
                    contextMenuStrip.Show(Cursor.Position);
                }
            }
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListViewItem processInfo = listView.FocusedItem;
            int processId = int.Parse(processInfo.SubItems[processInfo.SubItems.Count - 1].Text);
            Process process = processesList.Find(p => p.Id == processId);
            process.Kill();
        }

        private void terminateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListViewItem processInfo = listView.FocusedItem;
            int processId = int.Parse(processInfo.SubItems[processInfo.SubItems.Count - 1].Text);
            Process process = processesList.Find(p => p.Id == processId);
            cmd("taskkill /PID " + processId + " /T /F", true);
        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Run run = new Run();
            DialogResult result = run.ShowDialog();
            if (result == DialogResult.OK)
            {
                cmd("start " + run.command);
            }
            else if (result == DialogResult.Yes)
            {
                cmd("start " + run.command, true);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (update.IsAlive) update.Abort();
            Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (update.IsAlive) update.Abort();
            Application.Exit();
        }
    }
}
