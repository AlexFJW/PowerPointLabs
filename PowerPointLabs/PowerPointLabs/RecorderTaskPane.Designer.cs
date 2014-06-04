﻿namespace PowerPointLabs
{
    partial class RecorderTaskPane
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.statusLabel = new System.Windows.Forms.Label();
            this.recButton = new System.Windows.Forms.Button();
            this.playButton = new System.Windows.Forms.Button();
            this.timerLabel = new System.Windows.Forms.Label();
            this.soundTrackBar = new System.Windows.Forms.TrackBar();
            this.recDisplay = new System.Windows.Forms.ListView();
            this.recNumber = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.recName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.recLength = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.scriptDisplay = new System.Windows.Forms.ListView();
            this.statusColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.scirptColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.scriptListViewLabel = new System.Windows.Forms.Label();
            this.slideShowButton = new System.Windows.Forms.Button();
            this.scriptDetialLabel = new System.Windows.Forms.Label();
            this.scriptDetailTextBox = new System.Windows.Forms.TextBox();
            this.stopButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.soundTrackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Font = new System.Drawing.Font("SimSun", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.statusLabel.ForeColor = System.Drawing.Color.Red;
            this.statusLabel.Location = new System.Drawing.Point(12, 80);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(89, 12);
            this.statusLabel.TabIndex = 0;
            this.statusLabel.Text = "Recording...";
            // 
            // recButton
            // 
            this.recButton.Location = new System.Drawing.Point(12, 22);
            this.recButton.Name = "recButton";
            this.recButton.Size = new System.Drawing.Size(43, 41);
            this.recButton.TabIndex = 1;
            this.recButton.UseVisualStyleBackColor = true;
            this.recButton.Click += new System.EventHandler(this.RecButtonClick);
            // 
            // playButton
            // 
            this.playButton.Location = new System.Drawing.Point(110, 22);
            this.playButton.Name = "playButton";
            this.playButton.Size = new System.Drawing.Size(43, 41);
            this.playButton.TabIndex = 3;
            this.playButton.UseVisualStyleBackColor = true;
            this.playButton.Click += new System.EventHandler(this.PlayButtonClick);
            // 
            // timerLabel
            // 
            this.timerLabel.AutoSize = true;
            this.timerLabel.Font = new System.Drawing.Font("Times New Roman", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.timerLabel.Location = new System.Drawing.Point(151, 69);
            this.timerLabel.Name = "timerLabel";
            this.timerLabel.Size = new System.Drawing.Size(116, 31);
            this.timerLabel.TabIndex = 4;
            this.timerLabel.Text = "00:00:00";
            // 
            // soundTrackBar
            // 
            this.soundTrackBar.Location = new System.Drawing.Point(3, 99);
            this.soundTrackBar.Maximum = 100;
            this.soundTrackBar.Name = "soundTrackBar";
            this.soundTrackBar.Size = new System.Drawing.Size(270, 45);
            this.soundTrackBar.TabIndex = 5;
            // 
            // recDisplay
            // 
            this.recDisplay.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.recNumber,
            this.recName,
            this.recLength});
            this.recDisplay.FullRowSelect = true;
            this.recDisplay.HideSelection = false;
            this.recDisplay.Location = new System.Drawing.Point(12, 156);
            this.recDisplay.MultiSelect = false;
            this.recDisplay.Name = "recDisplay";
            this.recDisplay.Size = new System.Drawing.Size(253, 122);
            this.recDisplay.TabIndex = 6;
            this.recDisplay.UseCompatibleStateImageBehavior = false;
            this.recDisplay.View = System.Windows.Forms.View.Details;
            this.recDisplay.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.RecDisplayItemSelectionChanged);
            // 
            // recNumber
            // 
            this.recNumber.Text = "No.";
            this.recNumber.Width = 34;
            // 
            // recName
            // 
            this.recName.Text = "Name";
            this.recName.Width = 138;
            // 
            // recLength
            // 
            this.recLength.Text = "Length";
            this.recLength.Width = 74;
            // 
            // scriptDisplay
            // 
            this.scriptDisplay.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.statusColumn,
            this.scirptColumn});
            this.scriptDisplay.FullRowSelect = true;
            this.scriptDisplay.HideSelection = false;
            this.scriptDisplay.Location = new System.Drawing.Point(12, 305);
            this.scriptDisplay.MultiSelect = false;
            this.scriptDisplay.Name = "scriptDisplay";
            this.scriptDisplay.Size = new System.Drawing.Size(253, 122);
            this.scriptDisplay.TabIndex = 7;
            this.scriptDisplay.UseCompatibleStateImageBehavior = false;
            this.scriptDisplay.View = System.Windows.Forms.View.Details;
            this.scriptDisplay.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.ScriptDisplayItemSelectionChanged);
            // 
            // statusColumn
            // 
            this.statusColumn.Text = "Status";
            this.statusColumn.Width = 70;
            // 
            // scirptColumn
            // 
            this.scirptColumn.Text = "Script";
            this.scirptColumn.Width = 176;
            // 
            // scriptListViewLabel
            // 
            this.scriptListViewLabel.AutoSize = true;
            this.scriptListViewLabel.Font = new System.Drawing.Font("SimSun", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.scriptListViewLabel.Location = new System.Drawing.Point(14, 284);
            this.scriptListViewLabel.Name = "scriptListViewLabel";
            this.scriptListViewLabel.Size = new System.Drawing.Size(54, 12);
            this.scriptListViewLabel.TabIndex = 8;
            this.scriptListViewLabel.Text = "Scripts";
            // 
            // slideShowButton
            // 
            this.slideShowButton.Location = new System.Drawing.Point(157, 22);
            this.slideShowButton.Name = "slideShowButton";
            this.slideShowButton.Size = new System.Drawing.Size(108, 41);
            this.slideShowButton.TabIndex = 9;
            this.slideShowButton.Text = "Record while\nSlide Show";
            this.slideShowButton.UseVisualStyleBackColor = true;
            this.slideShowButton.Click += new System.EventHandler(this.SlideShowButtonClick);
            // 
            // scriptDetialLabel
            // 
            this.scriptDetialLabel.AutoSize = true;
            this.scriptDetialLabel.Font = new System.Drawing.Font("SimSun", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.scriptDetialLabel.Location = new System.Drawing.Point(10, 436);
            this.scriptDetialLabel.Name = "scriptDetialLabel";
            this.scriptDetialLabel.Size = new System.Drawing.Size(103, 12);
            this.scriptDetialLabel.TabIndex = 10;
            this.scriptDetialLabel.Text = "Current Script";
            // 
            // scriptDetailTextBox
            // 
            this.scriptDetailTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.scriptDetailTextBox.Font = new System.Drawing.Font("SimSun", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.scriptDetailTextBox.Location = new System.Drawing.Point(12, 457);
            this.scriptDetailTextBox.Multiline = true;
            this.scriptDetailTextBox.Name = "scriptDetailTextBox";
            this.scriptDetailTextBox.Size = new System.Drawing.Size(253, 95);
            this.scriptDetailTextBox.TabIndex = 11;
            // 
            // stopButton
            // 
            this.stopButton.Image = global::PowerPointLabs.Properties.Resources.Stop;
            this.stopButton.Location = new System.Drawing.Point(61, 22);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(43, 41);
            this.stopButton.TabIndex = 2;
            this.stopButton.UseVisualStyleBackColor = true;
            this.stopButton.Click += new System.EventHandler(this.StopButtonClick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("SimSun", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(12, 137);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(40, 12);
            this.label1.TabIndex = 12;
            this.label1.Text = "Audio";
            // 
            // RecorderTaskPane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.scriptDetailTextBox);
            this.Controls.Add(this.scriptDetialLabel);
            this.Controls.Add(this.slideShowButton);
            this.Controls.Add(this.scriptListViewLabel);
            this.Controls.Add(this.scriptDisplay);
            this.Controls.Add(this.recDisplay);
            this.Controls.Add(this.soundTrackBar);
            this.Controls.Add(this.timerLabel);
            this.Controls.Add(this.playButton);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.recButton);
            this.Controls.Add(this.statusLabel);
            this.Name = "RecorderTaskPane";
            this.Size = new System.Drawing.Size(280, 573);
            this.Load += new System.EventHandler(this.RecorderPane_Load);
            ((System.ComponentModel.ISupportInitialize)(this.soundTrackBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Button recButton;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Button playButton;
        private System.Windows.Forms.Label timerLabel;
        private System.Windows.Forms.TrackBar soundTrackBar;
        private System.Windows.Forms.ListView recDisplay;
        private System.Windows.Forms.ColumnHeader recNumber;
        private System.Windows.Forms.ColumnHeader recName;
        private System.Windows.Forms.ColumnHeader recLength;
        private System.Windows.Forms.ListView scriptDisplay;
        private System.Windows.Forms.ColumnHeader statusColumn;
        private System.Windows.Forms.ColumnHeader scirptColumn;
        private System.Windows.Forms.Label scriptListViewLabel;
        private System.Windows.Forms.Button slideShowButton;
        private System.Windows.Forms.Label scriptDetialLabel;
        private System.Windows.Forms.TextBox scriptDetailTextBox;
        private System.Windows.Forms.Label label1;
    }
}