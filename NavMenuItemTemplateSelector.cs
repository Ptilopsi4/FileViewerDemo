using FileViewerDemo.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace FileViewerDemo;

public class NavMenuItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ItemTemplate { get; set; }
    public DataTemplate? HeaderTemplate { get; set; }
    public DataTemplate? FallbackTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        if (item is DriveItem driveItem)
        {
            // 确保返回非空模板
            if (driveItem.IsHeader)
            {
                return HeaderTemplate ?? ItemTemplate;  // 有备用
            }
            else
            {
                return ItemTemplate ?? HeaderTemplate;  // 有备用
            }
        }
        
        return FallbackTemplate;
    }
}
