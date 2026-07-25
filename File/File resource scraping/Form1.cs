using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace File_resource_scraping
{
    public partial class Form1 : Form
    {
        private static readonly Dictionary<string, string[]> FileTypeMappings =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "图片", new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tif", ".tiff", ".svg" } },
                { "图像", new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tif", ".tiff", ".svg" } },
                { "image", new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tif", ".tiff", ".svg" } },
                { "images", new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tif", ".tiff", ".svg" } },
                { "照片", new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tif", ".tiff", ".svg" } },
                { "视频", new[] { ".mp4", ".avi", ".mov", ".mkv", ".wmv", ".flv", ".webm", ".m4v", ".mpeg", ".mpg" } },
                { "video", new[] { ".mp4", ".avi", ".mov", ".mkv", ".wmv", ".flv", ".webm", ".m4v", ".mpeg", ".mpg" } },
                { "音频", new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a" } },
                { "音乐", new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a" } },
                { "audio", new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a" } },
                { "music", new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a" } },
                { "文档", new[] { ".doc", ".docx", ".pdf", ".txt", ".rtf", ".xls", ".xlsx", ".ppt", ".pptx", ".csv" } },
                { "document", new[] { ".doc", ".docx", ".pdf", ".txt", ".rtf", ".xls", ".xlsx", ".ppt", ".pptx", ".csv" } },
                { "documents", new[] { ".doc", ".docx", ".pdf", ".txt", ".rtf", ".xls", ".xlsx", ".ppt", ".pptx", ".csv" } },
                { "压缩包", new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2" } },
                { "压缩", new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2" } },
                { "archive", new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2" } },
                { "代码", new[] { ".cs", ".js", ".ts", ".html", ".css", ".json", ".xml", ".py", ".java", ".cpp", ".c", ".h" } },
                { "code", new[] { ".cs", ".js", ".ts", ".html", ".css", ".json", ".xml", ".py", ".java", ".cpp", ".c", ".h" } }
            };

        public Form1()
        {
            InitializeComponent();
            txtFileType.Text = ".jpg, .png";
            WriteLog("请选择源文件夹、文件类型或后缀、目标文件夹，然后点击“开始移动”。");
        }

        private void btnBrowseSource_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(txtSourceFolder.Text))
            {
                sourceFolderDialog.SelectedPath = txtSourceFolder.Text;
            }

            if (sourceFolderDialog.ShowDialog(this) == DialogResult.OK)
            {
                txtSourceFolder.Text = sourceFolderDialog.SelectedPath;
            }
        }

        private void btnBrowseDestination_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(txtDestinationFolder.Text))
            {
                destinationFolderDialog.SelectedPath = txtDestinationFolder.Text;
            }

            if (destinationFolderDialog.ShowDialog(this) == DialogResult.OK)
            {
                txtDestinationFolder.Text = destinationFolderDialog.SelectedPath;
            }
        }

        private async void btnMoveFiles_Click(object sender, EventArgs e)
        {
            string sourceFolder = txtSourceFolder.Text.Trim();
            string destinationFolder = txtDestinationFolder.Text.Trim();
            string fileTypeInput = txtFileType.Text.Trim();

            string validationMessage;
            if (!ValidateInput(sourceFolder, destinationFolder, fileTypeInput, out validationMessage))
            {
                MessageBox.Show(this, validationMessage, "输入有误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetBusy(true);
            WriteLog("正在扫描并移动文件，请稍候...");
            ResetProgress();

            try
            {
                IProgress<MoveProgress> progress = new Progress<MoveProgress>(UpdateMoveProgress);
                MoveResult result = await Task.Run(() => MoveMatchingFiles(sourceFolder, destinationFolder, fileTypeInput, progress));
                lblStatus.Text = string.Format("完成：匹配 {0} 个，成功移动 {1} 个，失败 {2} 个。", result.MatchCount, result.MovedCount, result.FailedCount);
                lblCurrentFile.Text = "当前文件：完成";
            }
            catch (Exception ex)
            {
                WriteLog("执行失败：" + ex.Message);
                lblStatus.Text = "执行失败。";
                MessageBox.Show(this, ex.Message, "执行失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private bool ValidateInput(string sourceFolder, string destinationFolder, string fileTypeInput, out string message)
        {
            if (string.IsNullOrWhiteSpace(sourceFolder))
            {
                message = "请先选择源文件夹。";
                return false;
            }

            if (!Directory.Exists(sourceFolder))
            {
                message = "源文件夹不存在。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(fileTypeInput))
            {
                message = "请输入文件类型或文件后缀，例如：图片、.pdf、jpg,png。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(destinationFolder))
            {
                message = "请先选择或输入目标文件夹。";
                return false;
            }

            string sourceFullPath = NormalizeDirectoryPath(sourceFolder);
            string destinationFullPath = NormalizeDirectoryPath(destinationFolder);
            if (string.Equals(sourceFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
            {
                message = "源文件夹和目标文件夹不能相同。";
                return false;
            }

            try
            {
                ParseFileSuffixes(fileTypeInput);
            }
            catch (ArgumentException ex)
            {
                message = ex.Message;
                return false;
            }

            message = string.Empty;
            return true;
        }

        private MoveResult MoveMatchingFiles(string sourceFolder, string destinationFolder, string fileTypeInput, IProgress<MoveProgress> progress)
        {
            HashSet<string> suffixes = ParseFileSuffixes(fileTypeInput);
            string sourceFullPath = NormalizeDirectoryPath(sourceFolder);
            string destinationFullPath = NormalizeDirectoryPath(destinationFolder);

            Directory.CreateDirectory(destinationFullPath);

            MoveResult result = new MoveResult();
            AddMoveMessage(result, progress, "源文件夹：" + sourceFullPath, 0, 0, 0, 0, "-");
            AddMoveMessage(result, progress, "目标文件夹：" + destinationFullPath, 0, 0, 0, 0, "-");
            AddMoveMessage(result, progress, "匹配后缀：" + string.Join(", ", suffixes.OrderBy(item => item)), 0, 0, 0, 0, "-");
            AddMoveMessage(result, progress, string.Empty, 0, 0, 0, 0, "-");

            List<string> matchedFiles = Directory
                .EnumerateFiles(sourceFullPath, "*", SearchOption.AllDirectories)
                .Where(filePath => !IsSameOrChildPath(filePath, destinationFullPath))
                .Where(filePath => IsMatchingFile(filePath, suffixes))
                .ToList();

            result.MatchCount = matchedFiles.Count;
            ReportMoveProgress(progress, 0, result.MatchCount, 0, 0, "-", null);

            if (matchedFiles.Count == 0)
            {
                AddMoveMessage(result, progress, "没有找到匹配的文件。", 0, 0, 0, 0, "-");
                return result;
            }

            int processedCount = 0;
            foreach (string sourceFilePath in matchedFiles)
            {
                string destinationFilePath = GetUniqueDestinationPath(
                    destinationFullPath,
                    Path.GetFileName(sourceFilePath));
                string currentFile = sourceFilePath;

                ReportMoveProgress(progress, processedCount, result.MatchCount, result.MovedCount, result.FailedCount, currentFile, null);

                try
                {
                    File.Move(sourceFilePath, destinationFilePath);
                    result.MovedCount++;
                    processedCount++;
                    AddMoveMessage(
                        result,
                        progress,
                        "已移动：" + sourceFilePath + " -> " + destinationFilePath,
                        processedCount,
                        result.MatchCount,
                        result.MovedCount,
                        result.FailedCount,
                        currentFile);
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    processedCount++;
                    AddMoveMessage(
                        result,
                        progress,
                        "移动失败：" + sourceFilePath + "；原因：" + ex.Message,
                        processedCount,
                        result.MatchCount,
                        result.MovedCount,
                        result.FailedCount,
                        currentFile);
                }
            }

            AddMoveMessage(result, progress, string.Empty, processedCount, result.MatchCount, result.MovedCount, result.FailedCount, "完成");
            AddMoveMessage(
                result,
                progress,
                string.Format("完成：匹配 {0} 个，成功移动 {1} 个，失败 {2} 个。", result.MatchCount, result.MovedCount, result.FailedCount),
                processedCount,
                result.MatchCount,
                result.MovedCount,
                result.FailedCount,
                "完成");
            return result;
        }

        private void AddMoveMessage(
            MoveResult result,
            IProgress<MoveProgress> progress,
            string message,
            int processedCount,
            int totalCount,
            int movedCount,
            int failedCount,
            string currentFile)
        {
            result.AddMessage(message);
            ReportMoveProgress(progress, processedCount, totalCount, movedCount, failedCount, currentFile, message);
        }

        private void ReportMoveProgress(
            IProgress<MoveProgress> progress,
            int processedCount,
            int totalCount,
            int movedCount,
            int failedCount,
            string currentFile,
            string message)
        {
            if (progress == null)
            {
                return;
            }

            progress.Report(new MoveProgress
            {
                ProcessedCount = processedCount,
                TotalCount = totalCount,
                MovedCount = movedCount,
                FailedCount = failedCount,
                CurrentFile = currentFile,
                Message = message
            });
        }

        private void UpdateMoveProgress(MoveProgress progress)
        {
            if (progress.TotalCount > 0)
            {
                progressBarMove.Maximum = progress.TotalCount;
                progressBarMove.Value = Math.Min(progress.ProcessedCount, progress.TotalCount);
                lblStatus.Text = string.Format(
                    "处理中：{0}/{1}，成功 {2} 个，失败 {3} 个。",
                    progress.ProcessedCount,
                    progress.TotalCount,
                    progress.MovedCount,
                    progress.FailedCount);
            }
            else
            {
                progressBarMove.Maximum = 1;
                progressBarMove.Value = 0;
            }

            if (!string.IsNullOrWhiteSpace(progress.CurrentFile))
            {
                lblCurrentFile.Text = "当前文件：" + progress.CurrentFile;
            }

            if (!string.IsNullOrEmpty(progress.Message))
            {
                AppendLog(progress.Message);
            }
        }

        private HashSet<string> ParseFileSuffixes(string input)
        {
            char[] separators = { ',', ';', '，', '；', '|', '\r', '\n', '\t', ' ' };
            string[] tokens = input
                .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0)
                .ToArray();

            HashSet<string> suffixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string rawToken in tokens)
            {
                string token = rawToken.Trim();
                string[] mappedSuffixes;
                if (FileTypeMappings.TryGetValue(token, out mappedSuffixes))
                {
                    foreach (string suffix in mappedSuffixes)
                    {
                        suffixes.Add(suffix);
                    }

                    continue;
                }

                if (token.StartsWith("*.", StringComparison.Ordinal))
                {
                    token = token.Substring(1);
                }
                else if (token.StartsWith("*", StringComparison.Ordinal))
                {
                    token = token.Substring(1);
                }

                if (token.StartsWith(".", StringComparison.Ordinal))
                {
                    suffixes.Add(token.ToLowerInvariant());
                    continue;
                }

                if (token.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    throw new ArgumentException("文件类型或后缀包含无效字符：" + rawToken);
                }

                suffixes.Add("." + token.ToLowerInvariant());
            }

            if (suffixes.Count == 0)
            {
                throw new ArgumentException("请输入有效的文件类型或文件后缀。");
            }

            return suffixes;
        }

        private bool IsMatchingFile(string filePath, HashSet<string> suffixes)
        {
            string fileName = Path.GetFileName(filePath);
            foreach (string suffix in suffixes)
            {
                if (fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private string GetUniqueDestinationPath(string destinationFolder, string fileName)
        {
            string destinationPath = Path.Combine(destinationFolder, fileName);
            if (!File.Exists(destinationPath))
            {
                return destinationPath;
            }

            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            int index = 1;

            do
            {
                destinationPath = Path.Combine(destinationFolder, string.Format("{0} ({1}){2}", nameWithoutExtension, index, extension));
                index++;
            }
            while (File.Exists(destinationPath));

            return destinationPath;
        }

        private bool IsSameOrChildPath(string filePath, string folderPath)
        {
            string normalizedFilePath = Path.GetFullPath(filePath);
            string normalizedFolderPath = NormalizeDirectoryPath(folderPath);

            return normalizedFilePath.StartsWith(normalizedFolderPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeDirectoryPath(string path)
        {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private void ResetProgress()
        {
            progressBarMove.Minimum = 0;
            progressBarMove.Maximum = 1;
            progressBarMove.Value = 0;
            lblCurrentFile.Text = "当前文件：-";
        }

        private void SetBusy(bool isBusy)
        {
            btnMoveFiles.Enabled = !isBusy;
            btnBrowseSource.Enabled = !isBusy;
            btnBrowseDestination.Enabled = !isBusy;
            txtSourceFolder.Enabled = !isBusy;
            txtDestinationFolder.Enabled = !isBusy;
            txtFileType.Enabled = !isBusy;
            UseWaitCursor = isBusy;
        }

        private void WriteLog(string message)
        {
            txtLog.Text = message;
            lblStatus.Text = message;
        }

        private void AppendLog(string message)
        {
            if (txtLog.TextLength > 0)
            {
                txtLog.AppendText(Environment.NewLine);
            }

            txtLog.AppendText(message);
        }

        private sealed class MoveResult
        {
            private const int MaxLogLines = 1000;

            public MoveResult()
            {
                Messages = new List<string>();
            }

            public int MatchCount { get; set; }

            public int MovedCount { get; set; }

            public int FailedCount { get; set; }

            public List<string> Messages { get; private set; }

            public void AddMessage(string message)
            {
                if (Messages.Count < MaxLogLines)
                {
                    Messages.Add(message);
                }
                else if (Messages.Count == MaxLogLines)
                {
                    Messages.Add("日志过多，后续明细已省略。");
                }
            }
        }

        private sealed class MoveProgress
        {
            public int ProcessedCount { get; set; }

            public int TotalCount { get; set; }

            public int MovedCount { get; set; }

            public int FailedCount { get; set; }

            public string CurrentFile { get; set; }

            public string Message { get; set; }
        }
    }
}
