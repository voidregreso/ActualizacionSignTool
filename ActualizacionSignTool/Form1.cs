using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace ActualizacionSignTool;

public class Form1 : Form
{
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
		openFileDialog.Filter = "Executables|*.exe;*.dll;*.sys";
		openFileDialog.Multiselect = true;
		if (openFileDialog.ShowDialog() == DialogResult.OK)
		{
			string[] fileNames = openFileDialog.FileNames;
            processAtBackground(fileNames);
		}
	}

    private void button2_Click(object sender, EventArgs e)
    {
        OpenFileDialog folderBrowser = new OpenFileDialog
        {
            ValidateNames = false,
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "Folder Selection"
        };
        if (folderBrowser.ShowDialog() == DialogResult.OK)
        {
            string directory = Path.GetDirectoryName(folderBrowser.FileName);
            string[] files = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories)).ToArray();

            processAtBackground(files);
        }
    }

    private void MyDragDrop(object sender, DragEventArgs e)
    {
        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        processAtBackground(files);
    }

    private void MyDragEnter(object sender, DragEventArgs e)
    {
        // Check if the drag-and-drop data is a file
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // Indicates allowing
            e.Effect = DragDropEffects.Copy;
        }
        else
        {
            // Indicates refusing
            e.Effect = DragDropEffects.None;
        }
    }

    private void processAtBackground(string[] files)
    {
        int totalFiles = files.Length;
        procList.Items.Clear();
        progresoBarra.Value = 0;
        Thread backgroundThread = new Thread(() =>
        {
            int processed = 0;
            foreach (string file in files)
            {
                bool result = SignTool.SignWithCert(file, "http://timestamp.digicert.com/?alg=sha1");
                Invoke((MethodInvoker)delegate
                {
                    procList.Items.Add(file + "... " + (result ? "OK" : "Failed") + Environment.NewLine);
                    processed++;
                    progresoBarra.Value = (int)(processed * 100.0 / totalFiles);
                });
            }
            Invoke((MethodInvoker)(() => MessageBox.Show("Done")));
        });
        backgroundThread.IsBackground = true;
        backgroundThread.Start();
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
            this.button1.Margin = new System.Windows.Forms.Padding(4);
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
            this.button2.Margin = new System.Windows.Forms.Padding(4);
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
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(825, 468);
            this.Controls.Add(this.progresoBarra);
            this.Controls.Add(this.procList);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Signtool Actualizado";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MyDragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MyDragEnter);
            this.ResumeLayout(false);

	}
}
