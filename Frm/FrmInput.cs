using FrmControl;
using FrmControl.FrmBase_;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using YourNamespace;

namespace COMIEEE
{
    public partial class FrmInput : FrmBase
    {
		public static string ShowDialog(Form parent,string title, string defaultv ="",Action<FrmInput> Do = null) {
			FrmInput fm = new FrmInput();
			fm.label4.Text = title;
			fm.textBox1.Text = defaultv;

			fm.Owner = parent;
			Do?.Invoke(fm);
			//fm.textBox1.Text = defaulttext;
			var r = fm.ShowDialog(parent);
			
			if (r != DialogResult.OK)
			{
				return null;
			}
			return fm.textBox1.Text;
		}
		public static string ShowDialog(string title,string defaultv)
		{
			FrmInput fm = new FrmInput();
			fm.label4.Text = title;
			fm.textBox1.Text = defaultv;
			var r = fm.ShowDialog();
			if (r != DialogResult.OK)
			{
				return null;
			}
			return fm.textBox1.Text;
		}

		public FrmInput()
		{
			InitializeComponent();


		}

		private void button1_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
		}

		private void button2_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void panel5_Paint(object sender, EventArgs e)
		{
			this.Close();

		}
		public static string ShowDialog(
			string title,
			string defaultValue = "",
			Action<FrmInput> setup = null)
		{
			using (FrmInput fm = new FrmInput())
			{
				// 基本初始化
				fm.label4.Text = title;
				fm.textBox1.Text = defaultValue;

				// 允许外部设置属性（大小、位置、校验、样式等）
				setup?.Invoke(fm);

				// ⚠️ 不指定 Owner，确保在主窗体创建前可用
				DialogResult result = fm.ShowDialog();

				if (result != DialogResult.OK)
				{
					return null;
				}

				return fm.textBox1.Text;
			}
		}

		private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
			if (e.KeyCode == Keys.Enter)
			{
				this.DialogResult = DialogResult.OK;

            }
        }
    }
}
