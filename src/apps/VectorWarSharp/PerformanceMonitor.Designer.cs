namespace VectorWar
{
    partial class PerformanceMonitor
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.picNetworkGraph = new System.Windows.Forms.PictureBox();
            this.lblPacketLoss = new System.Windows.Forms.Label();
            this.lblBandwidth = new System.Windows.Forms.Label();
            this.lblFrameLag = new System.Windows.Forms.Label();
            this.lblNetworkLag = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.picFairnessGraph = new System.Windows.Forms.PictureBox();
            this.lblRemoteAhead = new System.Windows.Forms.Label();
            this.lblLocalAhead = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.lblPid = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picNetworkGraph)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picFairnessGraph)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.picNetworkGraph);
            this.groupBox1.Controls.Add(this.lblPacketLoss);
            this.groupBox1.Controls.Add(this.lblBandwidth);
            this.groupBox1.Controls.Add(this.lblFrameLag);
            this.groupBox1.Controls.Add(this.lblNetworkLag);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Location = new System.Drawing.Point(13, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(459, 168);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Network";
            // 
            // picNetworkGraph
            // 
            this.picNetworkGraph.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.picNetworkGraph.Location = new System.Drawing.Point(9, 20);
            this.picNetworkGraph.Name = "picNetworkGraph";
            this.picNetworkGraph.Size = new System.Drawing.Size(444, 96);
            this.picNetworkGraph.TabIndex = 8;
            this.picNetworkGraph.TabStop = false;
            this.picNetworkGraph.Paint += new System.Windows.Forms.PaintEventHandler(this.networkGraph_Paint);
            // 
            // lblPacketLoss
            // 
            this.lblPacketLoss.AutoSize = true;
            this.lblPacketLoss.Location = new System.Drawing.Point(339, 145);
            this.lblPacketLoss.Name = "lblPacketLoss";
            this.lblPacketLoss.Size = new System.Drawing.Size(24, 13);
            this.lblPacketLoss.TabIndex = 7;
            this.lblPacketLoss.Text = "0 %";
            // 
            // lblBandwidth
            // 
            this.lblBandwidth.AutoSize = true;
            this.lblBandwidth.Location = new System.Drawing.Point(339, 129);
            this.lblBandwidth.Name = "lblBandwidth";
            this.lblBandwidth.Size = new System.Drawing.Size(79, 13);
            this.lblBandwidth.TabIndex = 6;
            this.lblBandwidth.Text = "0 kilobytes/sec";
            // 
            // lblFrameLag
            // 
            this.lblFrameLag.AutoSize = true;
            this.lblFrameLag.Location = new System.Drawing.Point(104, 145);
            this.lblFrameLag.Name = "lblFrameLag";
            this.lblFrameLag.Size = new System.Drawing.Size(47, 13);
            this.lblFrameLag.TabIndex = 5;
            this.lblFrameLag.Text = "0 frames";
            // 
            // lblNetworkLag
            // 
            this.lblNetworkLag.AutoSize = true;
            this.lblNetworkLag.Location = new System.Drawing.Point(104, 129);
            this.lblNetworkLag.Name = "lblNetworkLag";
            this.lblNetworkLag.Size = new System.Drawing.Size(29, 13);
            this.lblNetworkLag.TabIndex = 4;
            this.lblNetworkLag.Text = "0 ms";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(230, 129);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(103, 13);
            this.label7.TabIndex = 3;
            this.label7.Text = "Network Bandwidth:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(230, 145);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(95, 13);
            this.label6.TabIndex = 2;
            this.label6.Text = "Packet Loss Rate:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 129);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(91, 13);
            this.label5.TabIndex = 1;
            this.label5.Text = "Network Latency:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 145);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(80, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "Frame Latency:";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.picFairnessGraph);
            this.groupBox2.Controls.Add(this.lblRemoteAhead);
            this.groupBox2.Controls.Add(this.lblLocalAhead);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Location = new System.Drawing.Point(13, 187);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(459, 193);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Syncrhonization";
            // 
            // picFairnessGraph
            // 
            this.picFairnessGraph.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.picFairnessGraph.Location = new System.Drawing.Point(9, 20);
            this.picFairnessGraph.Name = "picFairnessGraph";
            this.picFairnessGraph.Size = new System.Drawing.Size(444, 121);
            this.picFairnessGraph.TabIndex = 4;
            this.picFairnessGraph.TabStop = false;
            this.picFairnessGraph.Paint += new System.Windows.Forms.PaintEventHandler(this.fairnessGraph_Paint);
            // 
            // lblRemoteAhead
            // 
            this.lblRemoteAhead.AutoSize = true;
            this.lblRemoteAhead.Location = new System.Drawing.Point(92, 169);
            this.lblRemoteAhead.Name = "lblRemoteAhead";
            this.lblRemoteAhead.Size = new System.Drawing.Size(47, 13);
            this.lblRemoteAhead.TabIndex = 3;
            this.lblRemoteAhead.Text = "0 frames";
            // 
            // lblLocalAhead
            // 
            this.lblLocalAhead.AutoSize = true;
            this.lblLocalAhead.Location = new System.Drawing.Point(92, 152);
            this.lblLocalAhead.Name = "lblLocalAhead";
            this.lblLocalAhead.Size = new System.Drawing.Size(47, 13);
            this.lblLocalAhead.TabIndex = 2;
            this.lblLocalAhead.Text = "0 frames";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 152);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Local Status:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 169);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Remote Status:";
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(397, 386);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 2;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 396);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Process Id:";
            // 
            // lblPid
            // 
            this.lblPid.AutoSize = true;
            this.lblPid.Location = new System.Drawing.Point(79, 396);
            this.lblPid.Name = "lblPid";
            this.lblPid.Size = new System.Drawing.Size(43, 13);
            this.lblPid.TabIndex = 4;
            this.lblPid.Text = "123456";
            // 
            // PerformanceMonitor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(484, 421);
            this.ControlBox = false;
            this.Controls.Add(this.lblPid);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "PerformanceMonitor";
            this.Text = "Performance Monitor";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picNetworkGraph)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picFairnessGraph)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblPid;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblPacketLoss;
        private System.Windows.Forms.Label lblBandwidth;
        private System.Windows.Forms.Label lblFrameLag;
        private System.Windows.Forms.Label lblNetworkLag;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblRemoteAhead;
        private System.Windows.Forms.Label lblLocalAhead;
        private System.Windows.Forms.PictureBox picNetworkGraph;
        private System.Windows.Forms.PictureBox picFairnessGraph;
    }
}