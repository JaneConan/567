using System.Diagnostics;

namespace SameStatsCodeUI
{
    public partial class Form1 : Form
    {
        private Button runButton;
        private Label imageLabel;
        private Label statusLabel;
        private Label currentImageNameLabel;
        private bool monitoring = false;
        private string currentImage = "";
        private string pythonCodeFolderPath;

        public Form1()
        {
            InitializeComponent();
            // ��ȡ����ĵ�ǰ����Ŀ¼������PythonCode�ļ��е�·��
            string programPath = Application.StartupPath;
            pythonCodeFolderPath = Path.Combine(programPath, "PythonCode");

            // ���ô����С
            this.ClientSize = new Size(800, 600);
            // ������
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "SameStatsCodeUI";
            this.Text = "Python����������ͼƬ�鿴��";
            this.ResumeLayout(false);

            // ������а�ť
            runButton = new Button
            {
                Text = "����",
                Location = new Point(300, 10),
                Size = new Size(150, 30)
            };
            runButton.Click += RunButton_Click;
            this.Controls.Add(runButton);

            // �����ʾ����״̬��Label
            this.statusLabel = new Label
            {
                Location = new Point(10, 130),
                AutoSize = true
            };
            this.Controls.Add(this.statusLabel);

            // ���ͼƬ��ʾ��ǩ
            imageLabel = new Label
            {
                Location = new Point(10, 170),
                Size = new Size(760, 400)
            };
            imageLabel.AutoSize = false;
            imageLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(imageLabel);

            // �����ʾ��ǰͼƬ���ƺ���ŵ�Label
            this.currentImageNameLabel = new Label
            {
                Location = new Point(10, 570),
                AutoSize = true
            };
            this.Controls.Add(this.currentImageNameLabel);
        }


        private async void RunButton_Click(object sender, EventArgs e)
        {
            this.runButton.Enabled = false;
            this.monitoring = true;
            this.currentImage = "";

            // ����ִ��Python�ű�������
            var pythonTask = Task.Run(() => ExecutePythonScript());

            // ����������������������������ؽű�ִ�н����ص��ļ��ȣ�
            var monitorTask = Task.Run(() =>
            {
                Thread.Sleep(500);
                // ������������ӳ�500�����ʼ
                MonitorResults();
            });

            await Task.WhenAll(pythonTask, monitorTask);

            this.runButton.Enabled = true;
            this.monitoring = false;
        }

        private void ExecutePythonScript()
        {
            // ��ִ��Python�ű�֮ǰ���results�ļ���
            string resultsFolderPath = Path.Combine(pythonCodeFolderPath, "results");
            if (Directory.Exists(resultsFolderPath))
            {
                Directory.Delete(resultsFolderPath, true);
                Directory.CreateDirectory(resultsFolderPath);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "python";
            startInfo.Arguments = "same_stats.py run rando circle 200000 2 150";
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.WorkingDirectory = pythonCodeFolderPath;

            // ʹ��Invokeȷ����UI�߳��и���Label
            this.Invoke((MethodInvoker)(() => {
                statusLabel.Text = "��ʼִ��Python�ű�";
            }));

            Console.WriteLine("��ʼִ��Python�ű�");

            try
            {
                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine("���: " + e.Data);
                            this.Invoke((MethodInvoker)(() => {

                                statusLabel.Text = "���: " + e.Data;
                            }));
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine("����: " + e.Data);
                            this.Invoke((MethodInvoker)(() => {
                                statusLabel.Text = "����: " + e.Data;
                            }));
                        }
                    };

                    process.Start();

                    // ��ʼ�첽��ȡ���
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    Console.WriteLine("Python�ű�ִ�н���");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"��������ʧ��: {ex.Message}");
            }
        }

        private void MonitorResults()
        {
            string resultsFolderPath = Path.Combine(pythonCodeFolderPath, "results");
            FileSystemWatcher watcher = new FileSystemWatcher
            {
                Path = resultsFolderPath,
                Filter = "circle-image-*.png",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            watcher.Created += (s, e) =>
            {
                // �ȴ��ļ���ȫд�루��ֹ�ļ������⣩
                Thread.Sleep(500);
                if (e.FullPath != this.currentImage)
                {
                    this.currentImage = e.FullPath;
                    this.Invoke((MethodInvoker)(() =>
                    {
                        ShowImage(e.FullPath);
                    }));
                }
            };

            while (this.monitoring)
            {
                Thread.Sleep(100); // �����߳�����
            }

            watcher.EnableRaisingEvents = false;
        }


        private void ShowImage(string imagePath)
        {
            using (Bitmap bitmap = new Bitmap(imagePath))
            {
                // ��ȡͼƬԭʼ�ߴ�
                int imgWidth = bitmap.Width;
                int imgHeight = bitmap.Height;

                // �������ű�������Label�Ŀ��Ϊ�������ֵȱ���
                double scale = (double)this.imageLabel.Width / imgWidth;
                int newWidth = this.imageLabel.Width;
                int newHeight = (int)(imgHeight * scale);

                // �����������¸߶ȳ���Label�ĸ߶ȣ������¼�����Label�߶�Ϊ����
                if (newHeight > this.imageLabel.Height)
                {
                    scale = (double)this.imageLabel.Height / imgHeight;
                    newWidth = (int)(imgWidth * scale);
                    newHeight = this.imageLabel.Height;
                }

                Image scaledImage = new Bitmap(bitmap, new Size(newWidth, newHeight));
                this.imageLabel.Image = scaledImage;

                // ��ȡͼƬ���ƺ����
                string fileName = Path.GetFileName(imagePath);
                int index = int.Parse(Path.GetFileNameWithoutExtension(fileName).Split('-')[2]);
                this.currentImageNameLabel.Text = $"��ǰͼƬ: {fileName} (���: {index})";
            }
        }
    }
}