using System.ComponentModel.Design.Serialization;
using System.ComponentModel;
using System.Globalization;
using FrmControl.C.CMenu_.Node_;

public class CTreeNodeConverter : TypeConverter
{
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destType)
        => destType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destType);

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
    {
        if (destType == typeof(InstanceDescriptor) && value is CTreeNodeTxt tn)
        {
            // 假设 TextNode 有一个 (string text) 的构造函数
            var ctor = typeof(CTreeNodeTxt).GetConstructor(new Type[] { });
            return new InstanceDescriptor(ctor, new object[] { });
        }
        return base.ConvertTo(context, culture, value, destType);
    }

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType)
        => srcType == typeof(string) || base.CanConvertFrom(context, srcType);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string s)
        {
            // 简单示例：解析 "TextNode:内容"
            if (s.StartsWith("CTreeNodeTxt:"))
                return new CTreeNodeTxt();
        }
        return base.ConvertFrom(context, culture, value);
    }
}
