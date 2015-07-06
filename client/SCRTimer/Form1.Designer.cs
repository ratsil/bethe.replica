namespace SCRTimer
{
	partial class Form1
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
			this.components = new System.ComponentModel.Container();
			this._ui_lblPlayerTimer = new System.Windows.Forms.Label();
			this._ui_tmrPlayerView = new System.Windows.Forms.Timer(this.components);
			this._ui_tmrPlayerGet = new System.Windows.Forms.Timer(this.components);
			this._ui_lblAdvertTimer = new System.Windows.Forms.Label();
			this._ui_tmrAdvertGet = new System.Windows.Forms.Timer(this.components);
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// _ui_lblPlayerTimer
			// 
			this._ui_lblPlayerTimer.AutoSize = true;
			this._ui_lblPlayerTimer.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Bold);
			this._ui_lblPlayerTimer.ForeColor = System.Drawing.Color.DeepPink;
			this._ui_lblPlayerTimer.Location = new System.Drawing.Point(214, 16);
			this._ui_lblPlayerTimer.Name = "_ui_lblPlayerTimer";
			this._ui_lblPlayerTimer.Size = new System.Drawing.Size(131, 29);
			this._ui_lblPlayerTimer.TabIndex = 0;
			this._ui_lblPlayerTimer.Text = "PLAYLIST";
			// 
			// _ui_tmrPlayerView
			// 
			this._ui_tmrPlayerView.Interval = 200;
			this._ui_tmrPlayerView.Tick += new System.EventHandler(this._ui_tmrPlayerView_Tick);
			// 
			// _ui_tmrPlayerGet
			// 
			this._ui_tmrPlayerGet.Interval = 3000;
			this._ui_tmrPlayerGet.Tick += new System.EventHandler(this._ui_tmrPlayerGet_Tick);
			// 
			// _ui_lblAdvertTimer
			// 
			this._ui_lblAdvertTimer.AutoSize = true;
			this._ui_lblAdvertTimer.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this._ui_lblAdvertTimer.ForeColor = System.Drawing.Color.DarkViolet;
			this._ui_lblAdvertTimer.Location = new System.Drawing.Point(12, 16);
			this._ui_lblAdvertTimer.Name = "_ui_lblAdvertTimer";
			this._ui_lblAdvertTimer.Size = new System.Drawing.Size(63, 29);
			this._ui_lblAdvertTimer.TabIndex = 1;
			this._ui_lblAdvertTimer.Text = "ADV";
			// 
			// _ui_tmrAdvertGet
			// 
			this._ui_tmrAdvertGet.Interval = 3000;
			this._ui_tmrAdvertGet.Tick += new System.EventHandler(this._ui_tmrAdvertGet_Tick);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(214, 3);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(57, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "плейлист";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 3);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(153, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "следующий рекламный блок";
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(430, 46);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this._ui_lblAdvertTimer);
			this.Controls.Add(this._ui_lblPlayerTimer);
			this.Name = "Form1";
			this.Text = "Таймер прямого эфира";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label _ui_lblPlayerTimer;
		private System.Windows.Forms.Timer _ui_tmrPlayerView;
		private System.Windows.Forms.Timer _ui_tmrPlayerGet;
		private System.Windows.Forms.Label _ui_lblAdvertTimer;
		private System.Windows.Forms.Timer _ui_tmrAdvertGet;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
	}
}

