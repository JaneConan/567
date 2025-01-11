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
            // 获取程序的当前工作目录并构建PythonCode文件夹的路径
            string programPath = Application.StartupPath;
            pythonCodeFolderPath = Path.Combine(programPath, "PythonCode");

            // 设置窗体大小
            this.ClientSize = new Size(800, 600);
            // 表单设置
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "SameStatsCodeUI";
            this.Text = "Python代码运行与图片查看器";
            this.ResumeLayout(false);

            // 添加运行按钮
            runButton = new Button
            {
                Text = "运行",
                Location = new Point(300, 10),
                Size = new Size(150, 30)
            };
            runButton.Click += RunButton_Click;
            this.Controls.Add(runButton);

            // 添加显示运行状态的Label
            this.statusLabel = new Label
            {
                Location = new Point(10, 130),
                AutoSize = true
            };
            this.Controls.Add(this.statusLabel);

            // 添加图片显示标签
            imageLabel = new Label
            {
                Location = new Point(10, 170),
                Size = new Size(760, 400)
            };
            imageLabel.AutoSize = false;
            imageLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(imageLabel);

            // 添加显示当前图片名称和序号的Label
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

            // 启动执行Python脚本的任务
            var pythonTask = Task.Run(() => ExecutePythonScript());

            // 启动监控任务（如果有相关需求，例如监控脚本执行结果相关的文件等）
            var monitorTask = Task.Run(() =>
            {
                Thread.Sleep(500);
                // 启动监控任务，延迟500毫秒后开始
                MonitorResults();
            });

            await Task.WhenAll(pythonTask, monitorTask);

            this.runButton.Enabled = true;
            this.monitoring = false;
        }

        private void ExecutePythonScript()
        {
            // 在执行Python脚本之前清空results文件夹
            string resultsFolderPath = Path.Combine(pythonCodeFolderPath, "results");
            if (Directory.Exists(resultsFolderPath))
            {
                Directory.Delete(resultsFolderPath, true);
                Directory.CreateDirectory(resultsFolderPath);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "python";
            startInfo.Arguments = "same_stats.py run dino circle 200000 2 150";
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.WorkingDirectory = pythonCodeFolderPath;

            // 使用Invoke确保在UI线程中更新Label
            this.Invoke((MethodInvoker)(() => {
                statusLabel.Text = "开始执行Python脚本";
            }));

            Console.WriteLine("开始执行Python脚本");

            try
            {
                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine("输出: " + e.Data);
                            this.Invoke((MethodInvoker)(() => {

                                statusLabel.Text = "输出: " + e.Data;
                            }));
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine("错误: " + e.Data);
                            this.Invoke((MethodInvoker)(() => {
                                statusLabel.Text = "错误: " + e.Data;
                            }));
                        }
                    };

                    process.Start();

                    // 开始异步读取输出
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    Console.WriteLine("Python脚本执行结束");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"进程启动失败: {ex.Message}");
            }
        }

        private void MonitorResults()
        {
            string resultsFolderPath = Path.Combine(pythonCodeFolderPath, "results");
            while (this.monitoring)
            {
                string[] files = Directory.GetFiles(resultsFolderPath, "circle-image-*.png").OrderBy(f => f).ToArray();
                if (files.Length > 0)
                {
                    string latestFile = files[files.Length - 1];
                    if (latestFile != this.currentImage)
                    {
                        this.currentImage = latestFile;
                        this.Invoke((MethodInvoker)(() =>
                        {
                            ShowImage(latestFile);
                        }));
                    }
                }
                Thread.Sleep(2000);
            }
        }

        private void ShowImage(string imagePath)
        {
            using (Bitmap bitmap = new Bitmap(imagePath))
            {
                // 获取图片原始尺寸
                int imgWidth = bitmap.Width;
                int imgHeight = bitmap.Height;

                // 计算缩放比例，以Label的宽度为基础保持等比例
                double scale = (double)this.imageLabel.Width / imgWidth;
                int newWidth = this.imageLabel.Width;
                int newHeight = (int)(imgHeight * scale);

                // 如果计算出的新高度超过Label的高度，则重新计算以Label高度为基础
                if (newHeight > this.imageLabel.Height)
                {
                    scale = (double)this.imageLabel.Height / imgHeight;
                    newWidth = (int)(imgWidth * scale);
                    newHeight = this.imageLabel.Height;
                }

                Image scaledImage = new Bitmap(bitmap, new Size(newWidth, newHeight));
                this.imageLabel.Image = scaledImage;

                // 获取图片名称和序号
                string fileName = Path.GetFileName(imagePath);
                int index = int.Parse(Path.GetFileNameWithoutExtension(fileName).Split('-')[2]);
                this.currentImageNameLabel.Text = $"当前图片: {fileName} (序号: {index})";
            }
        }
    }
}