namespace conda_infor_project
{
    partial class login
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            label2 = new Label();
            loginBox = new TextBox();
            passwordBox = new TextBox();
            label3 = new Label();
            label4 = new Label();
            loginEnterButton = new Button();
            registerLinkLabel = new LinkLabel();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 20F);
            label1.Location = new Point(182, 9);
            label1.Name = "label1";
            label1.Size = new Size(452, 46);
            label1.TabIndex = 0;
            label1.Text = "Добро пожаловать в Conda";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 10F);
            label2.Location = new Point(321, 69);
            label2.Name = "label2";
            label2.Size = new Size(156, 23);
            label2.TabIndex = 1;
            label2.Text = "Войдите в систему";
            // 
            // loginBox
            // 
            loginBox.BorderStyle = BorderStyle.FixedSingle;
            loginBox.Font = new Font("Segoe UI", 15F);
            loginBox.Location = new Point(249, 153);
            loginBox.Name = "loginBox";
            loginBox.Size = new Size(305, 41);
            loginBox.TabIndex = 2;
            // 
            // passwordBox
            // 
            passwordBox.BorderStyle = BorderStyle.FixedSingle;
            passwordBox.Font = new Font("Segoe UI", 15F);
            passwordBox.Location = new Point(248, 224);
            passwordBox.Name = "passwordBox";
            passwordBox.Size = new Size(305, 41);
            passwordBox.TabIndex = 3;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 10F);
            label3.Location = new Point(248, 127);
            label3.Name = "label3";
            label3.Size = new Size(124, 23);
            label3.TabIndex = 4;
            label3.Text = "Введите логин";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 10F);
            label4.Location = new Point(249, 197);
            label4.Name = "label4";
            label4.Size = new Size(135, 23);
            label4.TabIndex = 5;
            label4.Text = "Введите пароль";
            // 
            // loginEnterButton
            // 
            loginEnterButton.Location = new Point(353, 327);
            loginEnterButton.Name = "loginEnterButton";
            loginEnterButton.Size = new Size(94, 29);
            loginEnterButton.TabIndex = 6;
            loginEnterButton.Text = "Войти";
            loginEnterButton.UseVisualStyleBackColor = true;
            loginEnterButton.Click += loginEnterButton_Click;
            // 
            // registerLinkLabel
            // 
            registerLinkLabel.AutoSize = true;
            registerLinkLabel.Location = new Point(328, 357);
            registerLinkLabel.Name = "registerLinkLabel";
            registerLinkLabel.Size = new Size(156, 20);
            registerLinkLabel.TabIndex = 7;
            registerLinkLabel.TabStop = true;
            registerLinkLabel.Text = "У меня нету аккаунта";
            registerLinkLabel.Click += this.registerLinkLabel_Click;
            // 
            // login
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(registerLinkLabel);
            Controls.Add(loginEnterButton);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(passwordBox);
            Controls.Add(loginBox);
            Controls.Add(label2);
            Controls.Add(label1);
            Name = "login";
            Text = "Вход";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private TextBox loginBox;
        private TextBox passwordBox;
        private Label label3;
        private Label label4;
        private Button loginEnterButton;
        private LinkLabel registerLinkLabel;
    }
}
