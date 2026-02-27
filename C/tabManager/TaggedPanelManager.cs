using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace TaggedPanelExample
{
	[DefaultProperty("CorePanel")]
	public class TaggedPanelManager : Component
	{
		public TaggedPanelManager() { }
		public TaggedPanelManager(IContainer container)
		{
			container.Add(this);
		}

		private Panel corePanel;
		/// <summary>
		/// 要承载切换控件的核心 Panel（在 Designer 或 代码中设置）。
		/// </summary>
		[Browsable(true)]
		[Description("The main panel that will host the switched controls.")]
		public Panel CorePanel
		{
			get => corePanel;
			set => corePanel = value;
		}

		/// <summary>
		/// 如果为 true，切入 CorePanel 的控件会被 Dock = Fill（一般是需要的）。
		/// </summary>
		[DefaultValue(true)]
		public bool AutoDockInPanel { get; set; } = true;

		private readonly Dictionary<string, ControlRecord> registry = new Dictionary<string, ControlRecord>(StringComparer.OrdinalIgnoreCase);

		private string activeTag = null;
		/// <summary>
		/// 当前激活的标签（如果有的话）。
		/// </summary>
		[Browsable(false)]
		public string ActiveTag => activeTag;

		public event EventHandler<SwitchEventArgs> Switching;
		public event EventHandler<SwitchEventArgs> Switched;

		/// <summary>
		/// 注册控件并给它一个标签（注册时记录它的原始父容器与布局信息）。
		/// 若同一 tag 已存在会抛出异常。
		/// </summary>
		public void Register(Control control, string tag)
		{
			if (control == null) throw new ArgumentNullException(nameof(control));
			if (string.IsNullOrWhiteSpace(tag)) throw new ArgumentNullException(nameof(tag));
			if (registry.ContainsKey(tag)) throw new ArgumentException($"Tag already registered: {tag}");

			var rec = new ControlRecord
			{
				Control = control,
				OriginalParent = control.Parent,
				OriginalIndex = control.Parent != null ? control.Parent.Controls.GetChildIndex(control) : -1,
				OrigDock = control.Dock,
				OrigAnchor = control.Anchor,
				OrigLocation = control.Location,
				OrigSize = control.Size,
				OrigVisible = control.Visible
			};
			registry[tag] = rec;
		}

		/// <summary>
		/// 注销某个标签（不会改变控件父子关系，仅从管理表中移除）。
		/// </summary>
		public bool Unregister(string tag)
		{
			if (string.IsNullOrWhiteSpace(tag)) return false;
			return registry.Remove(tag);
		}

		/// <summary>
		/// 切换到指定标签对应的控件。
		/// 切换前会触发 Switching，切换完成触发 Switched。
		/// 返回 true 表示成功切换；false 表示标签未注册。
		/// </summary>
		public bool SwitchTo(string tag)
		{
			if (corePanel == null) throw new InvalidOperationException("CorePanel is not set.");
			if (string.IsNullOrWhiteSpace(tag)) throw new ArgumentNullException(nameof(tag));
			if (!registry.TryGetValue(tag, out var target)) return false;

			var argsBefore = new SwitchEventArgs(activeTag, tag);
			Switching?.Invoke(this, argsBefore);

			// 1) 如果已有激活标签，把它从 corePanel 移走并还原到原来父容器
			if (activeTag != null && registry.TryGetValue(activeTag, out var current))
			{
				if (current.Control.Parent == corePanel)
				{
					corePanel.Controls.Remove(current.Control);

					if (current.OriginalParent != null)
					{
						// 添加回原父容器，并尝试恢复原来索引（若索引不合法则放到末尾）
						current.OriginalParent.Controls.Add(current.Control);
						if (current.OriginalIndex >= 0 && current.OriginalIndex < current.OriginalParent.Controls.Count)
							current.OriginalParent.Controls.SetChildIndex(current.Control, current.OriginalIndex);
					}

					// 恢复属性
					current.Control.Dock = current.OrigDock;
					current.Control.Anchor = current.OrigAnchor;
					current.Control.Location = current.OrigLocation;
					current.Control.Size = current.OrigSize;
					current.Control.Visible = current.OrigVisible;
				}
			}

			// 2) 将目标控件从它原来的父容器移（如果还在别的容器中），放到 corePanel
			if (target.Control.Parent != null && target.Control.Parent != corePanel)
			{
				target.Control.Parent.Controls.Remove(target.Control);
			}

			corePanel.Controls.Add(target.Control);
			if (AutoDockInPanel)
			{
				target.Control.Dock = DockStyle.Fill;
			}
			target.Control.Visible = true;

			activeTag = tag;

			var argsAfter = new SwitchEventArgs(argsBefore.FromTag, tag);
			Switched?.Invoke(this, argsAfter);

			return true;
		}

		/// <summary>
		/// 列出所有已注册标签（只读拷贝）。
		/// </summary>
		public IList<string> GetRegisteredTags()
		{
			return new List<string>(registry.Keys);
		}

		/// <summary>
		/// 如果 tag 存在则返回对应控件，否则 null。
		/// </summary>
		public Control GetControlByTag(string tag)
		{
			if (tag == null) return null;
			if (registry.TryGetValue(tag, out var r)) return r.Control;
			return null;
		}

		#region 内部类型
		private class ControlRecord
		{
			public Control Control;
			public Control OriginalParent;
			public int OriginalIndex;
			public DockStyle OrigDock;
			public AnchorStyles OrigAnchor;
			public Point OrigLocation;
			public Size OrigSize;
			public bool OrigVisible;
		}

		public class SwitchEventArgs : EventArgs
		{
			public string FromTag { get; }
			public string ToTag { get; }
			public SwitchEventArgs(string fromTag, string toTag)
			{
				FromTag = fromTag;
				ToTag = toTag;
			}
		}
		#endregion
	}
}
