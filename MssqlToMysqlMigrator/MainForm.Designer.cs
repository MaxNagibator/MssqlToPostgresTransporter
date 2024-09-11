namespace MssqlToMysqlMigrator
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.uiTableCheckBox = new System.Windows.Forms.CheckBox();
            this.uiFkCheckBox = new System.Windows.Forms.CheckBox();
            this.uiViewCheckBox = new System.Windows.Forms.CheckBox();
            this.uiDataCheckBox = new System.Windows.Forms.CheckBox();
            this.uiRunButton = new System.Windows.Forms.Button();
            this.connectionMssqlTextBox = new System.Windows.Forms.TextBox();
            this.connectionMysqlTextBox = new System.Windows.Forms.TextBox();
            this.uiShowInConsoleCheckBox = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.uiCreateDbCheckBox = new System.Windows.Forms.CheckBox();
            this.button1 = new System.Windows.Forms.Button();
            this.uiDataLogCheckBox = new System.Windows.Forms.CheckBox();
            this.button2 = new System.Windows.Forms.Button();
            this.uiMaxvalToFile = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.Location = new System.Drawing.Point(12, 12);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(681, 379);
            this.textBox1.TabIndex = 0;
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(699, 12);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(333, 27);
            this.progressBar1.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(699, 42);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "label1";
            // 
            // uiTableCheckBox
            // 
            this.uiTableCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiTableCheckBox.AutoSize = true;
            this.uiTableCheckBox.Checked = true;
            this.uiTableCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.uiTableCheckBox.Location = new System.Drawing.Point(722, 169);
            this.uiTableCheckBox.Name = "uiTableCheckBox";
            this.uiTableCheckBox.Size = new System.Drawing.Size(71, 17);
            this.uiTableCheckBox.TabIndex = 3;
            this.uiTableCheckBox.Text = "Таблицы";
            this.uiTableCheckBox.UseVisualStyleBackColor = true;
            // 
            // uiFkCheckBox
            // 
            this.uiFkCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiFkCheckBox.AutoSize = true;
            this.uiFkCheckBox.Checked = true;
            this.uiFkCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.uiFkCheckBox.Location = new System.Drawing.Point(799, 169);
            this.uiFkCheckBox.Name = "uiFkCheckBox";
            this.uiFkCheckBox.Size = new System.Drawing.Size(58, 17);
            this.uiFkCheckBox.TabIndex = 4;
            this.uiFkCheckBox.Text = "Ключи";
            this.uiFkCheckBox.UseVisualStyleBackColor = true;
            // 
            // uiViewCheckBox
            // 
            this.uiViewCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiViewCheckBox.AutoSize = true;
            this.uiViewCheckBox.Checked = true;
            this.uiViewCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.uiViewCheckBox.Location = new System.Drawing.Point(863, 169);
            this.uiViewCheckBox.Name = "uiViewCheckBox";
            this.uiViewCheckBox.Size = new System.Drawing.Size(105, 17);
            this.uiViewCheckBox.TabIndex = 5;
            this.uiViewCheckBox.Text = "Представления";
            this.uiViewCheckBox.UseVisualStyleBackColor = true;
            // 
            // uiDataCheckBox
            // 
            this.uiDataCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiDataCheckBox.AutoSize = true;
            this.uiDataCheckBox.Checked = true;
            this.uiDataCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.uiDataCheckBox.Location = new System.Drawing.Point(722, 298);
            this.uiDataCheckBox.Name = "uiDataCheckBox";
            this.uiDataCheckBox.Size = new System.Drawing.Size(67, 17);
            this.uiDataCheckBox.TabIndex = 6;
            this.uiDataCheckBox.Text = "Данные";
            this.uiDataCheckBox.UseVisualStyleBackColor = true;
            // 
            // uiRunButton
            // 
            this.uiRunButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiRunButton.Location = new System.Drawing.Point(720, 321);
            this.uiRunButton.Name = "uiRunButton";
            this.uiRunButton.Size = new System.Drawing.Size(164, 56);
            this.uiRunButton.TabIndex = 7;
            this.uiRunButton.Text = "Запуск";
            this.uiRunButton.UseVisualStyleBackColor = true;
            this.uiRunButton.Click += new System.EventHandler(this.uiRunButton_Click);
            // 
            // connectionMssqlTextBox
            // 
            this.connectionMssqlTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.connectionMssqlTextBox.Location = new System.Drawing.Point(12, 397);
            this.connectionMssqlTextBox.Name = "connectionMssqlTextBox";
            this.connectionMssqlTextBox.Size = new System.Drawing.Size(1020, 20);
            this.connectionMssqlTextBox.TabIndex = 8;
            // 
            // connectionMysqlTextBox
            // 
            this.connectionMysqlTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.connectionMysqlTextBox.Location = new System.Drawing.Point(12, 423);
            this.connectionMysqlTextBox.Name = "connectionMysqlTextBox";
            this.connectionMysqlTextBox.Size = new System.Drawing.Size(1020, 20);
            this.connectionMysqlTextBox.TabIndex = 9;
            // 
            // uiShowInConsoleCheckBox
            // 
            this.uiShowInConsoleCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiShowInConsoleCheckBox.AutoSize = true;
            this.uiShowInConsoleCheckBox.Location = new System.Drawing.Point(722, 205);
            this.uiShowInConsoleCheckBox.Name = "uiShowInConsoleCheckBox";
            this.uiShowInConsoleCheckBox.Size = new System.Drawing.Size(162, 17);
            this.uiShowInConsoleCheckBox.TabIndex = 10;
            this.uiShowInConsoleCheckBox.Text = "Вывести скрипт в консоль";
            this.uiShowInConsoleCheckBox.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(719, 153);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(172, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "Сформировать скрипт создания";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(717, 282);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(62, 13);
            this.label3.TabIndex = 12;
            this.label3.Text = "Перенести";
            // 
            // uiCreateDbCheckBox
            // 
            this.uiCreateDbCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiCreateDbCheckBox.AutoSize = true;
            this.uiCreateDbCheckBox.Checked = true;
            this.uiCreateDbCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.uiCreateDbCheckBox.Location = new System.Drawing.Point(722, 228);
            this.uiCreateDbCheckBox.Name = "uiCreateDbCheckBox";
            this.uiCreateDbCheckBox.Size = new System.Drawing.Size(120, 17);
            this.uiCreateDbCheckBox.TabIndex = 13;
            this.uiCreateDbCheckBox.Text = "Выполнить скрипт";
            this.uiCreateDbCheckBox.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(699, 58);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(90, 31);
            this.button1.TabIndex = 14;
            this.button1.Text = "<- Очистить";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // uiDataLogCheckBox
            // 
            this.uiDataLogCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uiDataLogCheckBox.AutoSize = true;
            this.uiDataLogCheckBox.Checked = true;
            this.uiDataLogCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.uiDataLogCheckBox.Location = new System.Drawing.Point(821, 298);
            this.uiDataLogCheckBox.Name = "uiDataLogCheckBox";
            this.uiDataLogCheckBox.Size = new System.Drawing.Size(147, 17);
            this.uiDataLogCheckBox.TabIndex = 15;
            this.uiDataLogCheckBox.Text = "Подробности в консоль";
            this.uiDataLogCheckBox.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.Location = new System.Drawing.Point(863, 58);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(169, 31);
            this.button2.TabIndex = 16;
            this.button2.Text = "Перенести БД";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.Button2_Click);
            // 
            // uiMaxvalToFile
            // 
            this.uiMaxvalToFile.Location = new System.Drawing.Point(863, 106);
            this.uiMaxvalToFile.Name = "uiMaxvalToFile";
            this.uiMaxvalToFile.Size = new System.Drawing.Size(169, 23);
            this.uiMaxvalToFile.TabIndex = 17;
            this.uiMaxvalToFile.Text = "Сохранить ключи в файл";
            this.uiMaxvalToFile.UseVisualStyleBackColor = true;
            this.uiMaxvalToFile.Click += new System.EventHandler(this.uiMaxvalToFile_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1044, 450);
            this.Controls.Add(this.uiMaxvalToFile);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.uiDataLogCheckBox);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.uiCreateDbCheckBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.uiShowInConsoleCheckBox);
            this.Controls.Add(this.connectionMysqlTextBox);
            this.Controls.Add(this.connectionMssqlTextBox);
            this.Controls.Add(this.uiRunButton);
            this.Controls.Add(this.uiDataCheckBox);
            this.Controls.Add(this.uiViewCheckBox);
            this.Controls.Add(this.uiFkCheckBox);
            this.Controls.Add(this.uiTableCheckBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.textBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "mssql -> mysql";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox uiTableCheckBox;
        private System.Windows.Forms.CheckBox uiFkCheckBox;
        private System.Windows.Forms.CheckBox uiViewCheckBox;
        private System.Windows.Forms.CheckBox uiDataCheckBox;
        private System.Windows.Forms.Button uiRunButton;
        private System.Windows.Forms.TextBox connectionMssqlTextBox;
        private System.Windows.Forms.TextBox connectionMysqlTextBox;
        private System.Windows.Forms.CheckBox uiShowInConsoleCheckBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox uiCreateDbCheckBox;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox uiDataLogCheckBox;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button uiMaxvalToFile;
    }
}

