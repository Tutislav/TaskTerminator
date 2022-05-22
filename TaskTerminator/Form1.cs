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
        Thread update;

        private void process_Add(Process process)
        {
            String processName = process.ProcessName;
            if (process.MainWindowTitle != "") processName = process.MainWindowTitle;
            String processStatus = "running";
            if (!process.Responding) processStatus = "not responding";
            ListViewItem processInfo = new ListViewItem(processName);
            processInfo.SubItems.Add(processStatus);
            processInfo.SubItems.Add("");
            processInfo.SubItems.Add("");
            processInfo.SubItems.Add(process.Id.ToString());
            if (process.MainWindowHandle != IntPtr.Zero) listView.Items.Insert(0, processInfo);
            else listView.Items.Add(processInfo);
        }

        private void process_Update(Process process)
        {
            String processName = process.ProcessName;
            if (process.MainWindowTitle != "") processName = process.MainWindowTitle;
            String processStatus = "running";
            if (!process.Responding) processStatus = "not responding";
            ListViewItem processInfo = listView.Items.Cast<ListViewItem>().Where(l => l.SubItems[l.SubItems.Count - 1].Text == process.Id.ToString()).First();
            if (processInfo.Text != processName) processInfo.Text = processName;
            if (processInfo.SubItems[1].Text != processStatus) processInfo.SubItems[1].Text = processStatus;
        }

        private void process_Remove(Process process)
        {
            listView.Items.Remove(listView.Items.Cast<ListViewItem>().Where(l => l.SubItems[l.SubItems.Count - 1].Text == process.Id.ToString()).First());
        }

        private void updateListView()
        {
            currentProcessesList = Process.GetProcesses().ToList();
            foreach (Process process in currentProcessesList)
            {
                if (!processesList.Exists(p => p.Id == process.Id))
                {
                    listView.Invoke(new Action(() => process_Add(process)));
                }
                else
                {
                    listView.Invoke(new Action(() => process_Update(process)));
                }
            }
            foreach (Process process in processesList)
            {
                if (!currentProcessesList.Exists(p => p.Id == process.Id))
                {
                    listView.Invoke(new Action(() => process_Remove(process)));
                }
            }
            processesList = new List<Process>(currentProcessesList);
        }

        private void cmd(String command, bool asAdmin)
        {
            Process process = new Process();
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "cmd.exe";
            processStartInfo.Arguments = "/C " + command;
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            if (asAdmin) processStartInfo.Verb = "runas";
            process.StartInfo = processStartInfo;
            process.Start();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            processesList = Process.GetProcesses().ToList();
            update = new Thread(updateListView);
            foreach (Process process in processesList)
            {
                listView.Invoke(new Action(() => process_Add(process)));
            }
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
                cmd("start " + run.command, false);
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
    }
}
