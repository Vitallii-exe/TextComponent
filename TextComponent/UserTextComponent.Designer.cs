namespace TextComponent
{
    partial class UserTextComponent
    {
        /// <summary> 
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором компонентов

        /// <summary> 
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.richTextBox = new CustomRichTextBox();
            this.DistortionButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // richTextBox
            // 
            this.richTextBox.Location = new System.Drawing.Point(0, 0);
            this.richTextBox.Name = "richTextBox";
            this.richTextBox.Size = new System.Drawing.Size(700, 200);
            this.richTextBox.TabIndex = 0;
            this.richTextBox.Text = "";
            this.richTextBox.SelectionChanged += new System.EventHandler(this.RichTextBoxSelectionChanged);
            this.richTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.richTextBoxKeyDown);
            this.richTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.richTextBoxKeyPress);
            // 
            // DistortionButton
            // 
            this.DistortionButton.Location = new System.Drawing.Point(24, 206);
            this.DistortionButton.Name = "DistortionButton";
            this.DistortionButton.Size = new System.Drawing.Size(94, 29);
            this.DistortionButton.TabIndex = 1;
            this.DistortionButton.Text = "Исказить";
            this.DistortionButton.UseVisualStyleBackColor = true;
            this.DistortionButton.Click += new System.EventHandler(this.DistortionButtonClick);
            // 
            // UserTextComponent
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.DistortionButton);
            this.Controls.Add(this.richTextBox);
            this.Name = "UserTextComponent";
            this.Size = new System.Drawing.Size(700, 246);
            this.ResumeLayout(false);

        }

        #endregion

        private CustomRichTextBox richTextBox;
        private Button DistortionButton;
    }
}