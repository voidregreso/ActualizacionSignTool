using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ActualizacionSignTool;

public class Form1 : Form
{
	private IContainer components;

	private Button button1;
    private ListBox procList;
    private ProgressBar progresoBarra;
    private Button button2;

	public Form1()
	{
		InitializeComponent();
	}

	private void button1_Click(object sender, EventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "Executables|*.exe;*.dll";
		openFileDialog.Multiselect = true;
		if (openFileDialog.ShowDialog() == DialogResult.OK)
		{
			string[] fileNames = openFileDialog.FileNames;
            procList.Items.Clear();
            foreach (string text in fileNames)
            {
                System.Windows.Forms.Application.DoEvents();
                bool res = SignTool.SignWithCert(text, "http://timestamp.digicert.com/?alg=sha1");
                procList.Items.Add(text + "... " + (res ? "OK" : "Failed") + Environment.NewLine);
            }
			MessageBox.Show("Done");
		}
	}

	private void button2_Click(object sender, EventArgs e)
	{
        OpenFileDialog folderBrowser = new OpenFileDialog();
        folderBrowser.ValidateNames = false;
        folderBrowser.CheckFileExists = false;
        folderBrowser.CheckPathExists = true;
        folderBrowser.FileName = "Folder Selection";
        if (folderBrowser.ShowDialog() == DialogResult.OK)
        {
            procList.Items.Clear();
            progresoBarra.Value = 0;
            string[] files = Directory.GetFiles(Path.GetDirectoryName(folderBrowser.FileName), "*.exe", SearchOption.AllDirectories);
            string[] files2 = Directory.GetFiles(Path.GetDirectoryName(folderBrowser.FileName), "*.dll", SearchOption.AllDirectories);
            int cnt = files.Length + files2.Length, idx = 0;
            Thread backgroundThread = new Thread(() =>
            {
                foreach (string text in files)
                {
                    bool res = SignTool.SignWithCert(text, "http://timestamp.digicert.com/?alg=sha1");
                    // Update UI
                    this.Invoke((MethodInvoker)delegate
                    {
                        procList.Items.Add(text + "... " + (res ? "OK" : "Failed") + Environment.NewLine);
                        idx++;
                        if (idx < cnt) progresoBarra.Value = (int)(idx * 100.0 / cnt);
                        else if (idx == cnt) progresoBarra.Value = 100;
                    });
                }
                foreach (string text2 in files2)
                {
                    bool res = SignTool.SignWithCert(text2, "http://timestamp.digicert.com/?alg=sha1");
                    // Update UI
                    this.Invoke((MethodInvoker)delegate
                    {
                        procList.Items.Add(text2 + "... " + (res ? "OK" : "Failed") + Environment.NewLine);
                        idx++;
                        if (idx < cnt) progresoBarra.Value = (int)(idx * 100.0 / cnt);
                        else if (idx == cnt) progresoBarra.Value = 100;
                    });
                }
                this.Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show("Done");
                });
            });
            backgroundThread.IsBackground = true;
            backgroundThread.Start();
        }
	}

	private void InitializeComponent()
	{
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.procList = new System.Windows.Forms.ListBox();
            this.progresoBarra = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(37, 18);
            this.button1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(100, 28);
            this.button1.TabIndex = 0;
            this.button1.Text = "Sign file";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(145, 18);
            this.button2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(100, 28);
            this.button2.TabIndex = 1;
            this.button2.Text = "Sign folder";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // procList
            // 
            this.procList.FormattingEnabled = true;
            this.procList.ItemHeight = 16;
            this.procList.Location = new System.Drawing.Point(16, 74);
            this.procList.Name = "procList";
            this.procList.Size = new System.Drawing.Size(797, 372);
            this.procList.TabIndex = 2;
            // 
            // progresoBarra
            // 
            this.progresoBarra.Location = new System.Drawing.Point(288, 18);
            this.progresoBarra.MarqueeAnimationSpeed = 5;
            this.progresoBarra.Name = "progresoBarra";
            this.progresoBarra.Size = new System.Drawing.Size(485, 31);
            this.progresoBarra.Step = 1;
            this.progresoBarra.TabIndex = 3;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(825, 468);
            this.Controls.Add(this.progresoBarra);
            this.Controls.Add(this.procList);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Signtool Actualizado";
            this.ResumeLayout(false);

	}
}
