// 3. 创建自定义的 CTreeNodeCollection 类，继承 BindingList<ICTreeNode>
using FrmControl.C.CMenu_.Node_;
using System.Collections;
using System.ComponentModel;
[Serializable]
public class CTreeNodeCollection : BindingList<ICTreeNode>
{
    // 添加 ListChanged 事件
    public event ListChangedEventHandler ListChanged;

    public CTreeNodeCollection()
    {
        // 订阅 ListChanged 事件
        base.ListChanged += CTreeNodeCollection_ListChanged;
    }
    public CTreeNodeCollection(IEnumerable ic):base(ic.Cast<ICTreeNode>().ToArray())
    {
        // 订阅 ListChanged 事件
        base.ListChanged += CTreeNodeCollection_ListChanged;
    }

    // 在这里处理 ListChanged 事件的逻辑
    private void CTreeNodeCollection_ListChanged(object sender, ListChangedEventArgs e)
    {
       
        // 如果需要也可以在这里抛出 ListChanged 事件
        ListChanged?.Invoke(sender, e);
    }

    // 可以扩展更多方法或自定义操作

    public void AddTextNode(string text)
    {
        Add(new CTreeNodeTxt() {  Text = text});
    }

    public void RemoveNode(ICTreeNode node)
    {
        Remove(node);
    }
}