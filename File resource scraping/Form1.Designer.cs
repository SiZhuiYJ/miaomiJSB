namespace File_resource_scraping
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblSourceFolder = new System.Windows.Forms.Label();
            this.txtSourceFolder = new System.Windows.Forms.TextBox();
            this.btnBrowseSource = new System.Windows.Forms.Button();
            this.lblFileType = new System.Windows.Forms.Label();
            this.txtFileType = new System.Windows.Forms.TextBox();
            this.lblTypeHint = new System.Windows.Forms.Label();
            this.lblDestinationFolder = new System.Windows.Forms.Label();
            this.txtDestinationFolder = new System.Windows.Forms.TextBox();
            this.btnBrowseDestination = new System.Windows.Forms.Button();
            this.btnMoveFiles = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.progressBarMove = new System.Windows.Forms.ProgressBar();
            this.lblCurrentFile = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.sourceFolderDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.destinationFolderDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.SuspendLayout();
            // 
            // lblSourceFolder
            // 
            this.lblSourceFolder.AutoSize = true;
            this.lblSourceFolder.Location = new System.Drawing.Point(24, 24);
            this.lblSourceFolder.Name = "lblSourceFolder";
            this.lblSourceFolder.Size = new System.Drawing.Size(65, 12);
            this.lblSourceFolder.TabIndex = 0;
            this.lblSourceFolder.Text = "源文件夹：";
            // 
            // txtSourceFolder
            // 
            this.txtSourceFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSourceFolder.Location = new System.Drawing.Point(116, 20);
            this.txtSourceFolder.Name = "txtSourceFolder";
            this.txtSourceFolder.Size = new System.Drawing.Size(516, 21);
            this.txtSourceFolder.TabIndex = 1;
            // 
            // btnBrowseSource
            // 
            this.btnBrowseSource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseSource.Location = new System.Drawing.Point(648, 18);
            this.btnBrowseSource.Name = "btnBrowseSource";
            this.btnBrowseSource.Size = new System.Drawing.Size(88, 25);
            this.btnBrowseSource.TabIndex = 2;
            this.btnBrowseSource.Text = "浏览...";
            this.btnBrowseSource.UseVisualStyleBackColor = true;
            this.btnBrowseSource.Click += new System.EventHandler(this.btnBrowseSource_Click);
            // 
            // lblFileType
            // 
            this.lblFileType.AutoSize = true;
            this.lblFileType.Location = new System.Drawing.Point(24, 68);
            this.lblFileType.Name = "lblFileType";
            this.lblFileType.Size = new System.Drawing.Size(77, 12);
            this.lblFileType.TabIndex = 3;
            this.lblFileType.Text = "类型/后缀：";
            // 
            // txtFileType
            // 
            this.txtFileType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFileType.Location = new System.Drawing.Point(116, 64);
            this.txtFileType.Name = "txtFileType";
            this.txtFileType.Size = new System.Drawing.Size(620, 21);
            this.txtFileType.TabIndex = 4;
            // 
            // lblTypeHint
            // 
            this.lblTypeHint.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTypeHint.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblTypeHint.Location = new System.Drawing.Point(116, 92);
            this.lblTypeHint.Name = "lblTypeHint";
            this.lblTypeHint.Size = new System.Drawing.Size(620, 18);
            this.lblTypeHint.TabIndex = 5;
            this.lblTypeHint.Text = "可输入：图片、视频、音频、文档、压缩包、代码，或 .pdf / jpg,png / *.docx。";
            // 
            // lblDestinationFolder
            // 
            this.lblDestinationFolder.AutoSize = true;
            this.lblDestinationFolder.Location = new System.Drawing.Point(24, 127);
            this.lblDestinationFolder.Name = "lblDestinationFolder";
            this.lblDestinationFolder.Size = new System.Drawing.Size(65, 12);
            this.lblDestinationFolder.TabIndex = 6;
            this.lblDestinationFolder.Text = "目标文件夹：";
            // 
            // txtDestinationFolder
            // 
            this.txtDestinationFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDestinationFolder.Location = new System.Drawing.Point(116, 123);
            this.txtDestinationFolder.Name = "txtDestinationFolder";
            this.txtDestinationFolder.Size = new System.Drawing.Size(516, 21);
            this.txtDestinationFolder.TabIndex = 7;
            // 
            // btnBrowseDestination
            // 
            this.btnBrowseDestination.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseDestination.Location = new System.Drawing.Point(648, 121);
            this.btnBrowseDestination.Name = "btnBrowseDestination";
            this.btnBrowseDestination.Size = new System.Drawing.Size(88, 25);
            this.btnBrowseDestination.TabIndex = 8;
            this.btnBrowseDestination.Text = "浏览...";
            this.btnBrowseDestination.UseVisualStyleBackColor = true;
            this.btnBrowseDestination.Click += new System.EventHandler(this.btnBrowseDestination_Click);
            // 
            // btnMoveFiles
            // 
            this.btnMoveFiles.Location = new System.Drawing.Point(116, 166);
            this.btnMoveFiles.Name = "btnMoveFiles";
            this.btnMoveFiles.Size = new System.Drawing.Size(120, 32);
            this.btnMoveFiles.TabIndex = 9;
            this.btnMoveFiles.Text = "开始移动";
            this.btnMoveFiles.UseVisualStyleBackColor = true;
            this.btnMoveFiles.Click += new System.EventHandler(this.btnMoveFiles_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.Location = new System.Drawing.Point(252, 174);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(484, 18);
            this.lblStatus.TabIndex = 10;
            this.lblStatus.Text = "等待操作。";
            // 
            // progressBarMove
            // 
            this.progressBarMove.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBarMove.Location = new System.Drawing.Point(26, 216);
            this.progressBarMove.Name = "progressBarMove";
            this.progressBarMove.Size = new System.Drawing.Size(710, 18);
            this.progressBarMove.TabIndex = 11;
            // 
            // lblCurrentFile
            // 
            this.lblCurrentFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCurrentFile.AutoEllipsis = true;
            this.lblCurrentFile.Location = new System.Drawing.Point(26, 242);
            this.lblCurrentFile.Name = "lblCurrentFile";
            this.lblCurrentFile.Size = new System.Drawing.Size(710, 18);
            this.lblCurrentFile.TabIndex = 12;
            this.lblCurrentFile.Text = "当前文件：-";
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.BackColor = System.Drawing.SystemColors.Window;
            this.txtLog.Location = new System.Drawing.Point(26, 268);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(710, 204);
            this.txtLog.TabIndex = 13;
            // 
            // sourceFolderDialog
            // 
            this.sourceFolderDialog.Description = "请选择要扫描的源文件夹";
            // 
            // destinationFolderDialog
            // 
            this.destinationFolderDialog.Description = "请选择移动后的目标文件夹";
            // 
            // Form1
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(764, 501);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.lblCurrentFile);
            this.Controls.Add(this.progressBarMove);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnMoveFiles);
            this.Controls.Add(this.btnBrowseDestination);
            this.Controls.Add(this.txtDestinationFolder);
            this.Controls.Add(this.lblDestinationFolder);
            this.Controls.Add(this.lblTypeHint);
            this.Controls.Add(this.txtFileType);
            this.Controls.Add(this.lblFileType);
            this.Controls.Add(this.btnBrowseSource);
            this.Controls.Add(this.txtSourceFolder);
            this.Controls.Add(this.lblSourceFolder);
            this.MinimumSize = new System.Drawing.Size(720, 430);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "文件资源整理工具";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblSourceFolder;
        private System.Windows.Forms.TextBox txtSourceFolder;
        private System.Windows.Forms.Button btnBrowseSource;
        private System.Windows.Forms.Label lblFileType;
        private System.Windows.Forms.TextBox txtFileType;
        private System.Windows.Forms.Label lblTypeHint;
        private System.Windows.Forms.Label lblDestinationFolder;
        private System.Windows.Forms.TextBox txtDestinationFolder;
        private System.Windows.Forms.Button btnBrowseDestination;
        private System.Windows.Forms.Button btnMoveFiles;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ProgressBar progressBarMove;
        private System.Windows.Forms.Label lblCurrentFile;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.FolderBrowserDialog sourceFolderDialog;
        private System.Windows.Forms.FolderBrowserDialog destinationFolderDialog;
    }
}

