using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using GetIt.UIV2.ViewModels;

namespace GetIt.UIV2;

public class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        if (data == null)
        {
            return new TextBlock { Text = "View not found - data was null." };
        }
        string? viewModelTypeName = data.GetType().FullName;
        if (viewModelTypeName == null)
        {
            return new TextBlock { Text = "View not found - View model type name was null." };
        }

        var type = Type.GetType(viewModelTypeName.Replace("ViewModel", "View"));
        if (type == null)
        {
            return new TextBlock { Text = $"View not found for {viewModelTypeName}." };
        }
        return (Control)Activator.CreateInstance(type)!;
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
