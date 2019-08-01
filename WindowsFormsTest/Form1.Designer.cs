namespace WindowsFormsTest
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
            this.blockUploadFromByteArray = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // blockUploadFromByteArray
            // 
            this.blockUploadFromByteArray.Location = new System.Drawing.Point(12, 12);
            this.blockUploadFromByteArray.Name = "blockUploadFromByteArray";
            this.blockUploadFromByteArray.Size = new System.Drawing.Size(406, 73);
            this.blockUploadFromByteArray.TabIndex = 0;
            this.blockUploadFromByteArray.Text = "CloudBlobBlob.UploadFromByteArray";
            this.blockUploadFromByteArray.UseVisualStyleBackColor = true;
            this.blockUploadFromByteArray.Click += new System.EventHandler(this.BlockUploadFromByteArray_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.blockUploadFromByteArray);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button blockUploadFromByteArray;
    }
}

