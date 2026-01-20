using System;
using System.Drawing;
using System.Windows.Forms;

public class PlaceholderTextBox : TextBox
{
    private string _placeholderText;
    private bool _isPlaceholderActive;

    // 新增属性
    public char PasswordChars { get; set; }
    public Color PlaceholderColor { get; set; }

    public PlaceholderTextBox()
    {
        this.PlaceholderColor = Color.Gray; // 默认占位符颜色
        this.Text = _placeholderText;

        // 监听控件的事件
        this.Enter += (sender, e) =>
        {
            if (this.Text == _placeholderText)
            {
                this.Text = "";
                this.ForeColor = System.Drawing.Color.Black;
                _isPlaceholderActive = false;
            }

            // 如果设置了密码字符
            if (this.PasswordChars != '\0')
            {
                this.UseSystemPasswordChar = true;
            }
        };

        this.Leave += (sender, e) =>
        {
            if (string.IsNullOrWhiteSpace(this.Text))
            {
                this.Text = _placeholderText;
                this.ForeColor = PlaceholderColor; // 使用指定的占位符颜色
                _isPlaceholderActive = true;
                this.UseSystemPasswordChar = false; // 取消密码字符显示
            }
        };
    }

    public string PlaceholderText
    {
        get { return _placeholderText; }
        set { _placeholderText = value; }
    }

    // 覆盖OnTextChanged事件，使得密码框始终能显示密码字符
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);

        if (this.PasswordChars != '\0' && !string.IsNullOrEmpty(this.Text) && this.Text != _placeholderText)
        {
            this.UseSystemPasswordChar = true;
        }
        else
        {
            this.UseSystemPasswordChar = false;
        }
    }
}
