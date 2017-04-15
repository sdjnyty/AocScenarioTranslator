using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using YTY.ScxLib;

namespace YTY.AocScenarioTranslator
{
  public class OpenScenarioCommand : ICommand
  {
    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
      return true;
    }

    public void Execute(object parameter)
    {
      var ofd = new OpenFileDialog();
      ofd.Filter = "帝国时代Ⅱ场景文件|*.scx";
      if (!ofd.ShowDialog().Value) return;
      My.ProgramViewModel.Scx = new ScxFile(ofd.FileName);
    }
  }

  public class SaveScenarioCommand : ICommand
  {
    public event EventHandler CanExecuteChanged
    {
      add { CommandManager.RequerySuggested += value; }
      remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object parameter)
    {
      return My.ProgramViewModel.FileOpened;
    }

    public void Execute(object parameter)
    {
      My.ProgramViewModel.ApplyChanges();
      My.ProgramViewModel.Scx.Save();
    }
  }

  public class SaveScenarioAsCommand : ICommand
  {
    public event EventHandler CanExecuteChanged
    {
      add { CommandManager.RequerySuggested += value; }
      remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object parameter)
    {
      return My.ProgramViewModel.FileOpened;
    }

    public void Execute(object parameter)
    {
      var sfd = new SaveFileDialog();
      sfd.Filter = "帝国时代Ⅱ场景文件|*.scx";
      sfd.FileName = My.ProgramViewModel.Scx.FileName;
      if (!sfd.ShowDialog().Value) return;
      My.ProgramViewModel.ApplyChanges();
      My.ProgramViewModel.Scx.SaveAs(sfd.FileName);
    }
  }

  public class CloseScenarioCommand : ICommand
  {
    public event EventHandler CanExecuteChanged
    {
      add { CommandManager.RequerySuggested += value; }
      remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object parameter)
    {
      return My.ProgramViewModel.FileOpened;
    }

    public void Execute(object parameter)
    {
      My.ProgramViewModel.Scx = null;
    }
  }

  public class CopyAllCommand : ICommand
  {
    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
      return true;
    }

    public void Execute(object parameter)
    {
      if (MessageBox.Show("该操作将重写所有条目内容，确认继续？", string.Empty, MessageBoxButton.OKCancel) == MessageBoxResult.Cancel) return;
      foreach (var node in My.ProgramViewModel.GetAllNodes().Where(n => n.HasContent))
      {
        node.To = node.Source;
      }
    }
  }

  public class EmptyAllNamesCommand : ICommand
  {
    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
      return true;
    }

    public void Execute(object parameter)
    {
      if (MessageBox.Show("该操作将清空所有【触发-名称】条目内容，确认继续？", string.Empty, MessageBoxButton.OKCancel) == MessageBoxResult.Cancel) return;
      foreach (var node in My.ProgramViewModel.GetAllNodes().Where(n => n.Type == NodeType.TriggerName))
      {
        node.To = string.Empty;
      }
    }
  }

  public class NumberNamesCommand : ICommand
  {
    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
      return true;
    }

    public void Execute(object parameter)
    {
      if (MessageBox.Show("该操作将重写所有【触发-名称】条目内容，确认继续？", string.Empty, MessageBoxButton.OKCancel) == MessageBoxResult.Cancel) return;
      foreach (var node in My.ProgramViewModel.GetAllNodes().Where(n => n.Type == NodeType.TriggerName).Select((n, i) => new { i = i, n = n }))
      {
        node.n.To = $"{My.ProgramViewModel.Prefix}{node.i + 1}";
      }
    }
  }

  public class ExportScenarioCommand : ICommand
  {
    public event EventHandler CanExecuteChanged
    {
      add { CommandManager.RequerySuggested += value; }
      remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object parameter)
    {
      return My.ProgramViewModel.FileOpened;
    }

    public void Execute(object parameter)
    {
      var sfd = new SaveFileDialog();
      sfd.Filter = "文本文件|*.txt";
      sfd.FileName = Path.ChangeExtension(My.ProgramViewModel.Scx.FileName, "txt");
      if (!sfd.ShowDialog().Value) return;
      My.ProgramViewModel.Export(sfd.FileName);
    }
  }

  public class ImportScenarioCommand : ICommand
  {
    public event EventHandler CanExecuteChanged
    {
      add { CommandManager.RequerySuggested += value; }
      remove { CommandManager.RequerySuggested -= value; }
    }


    public bool CanExecute(object parameter)
    {
      return My.ProgramViewModel.FileOpened;
    }

    public void Execute(object parameter)
    {
      var ofd = new OpenFileDialog();
      ofd.Filter = "文本文件 (UTF-8)|*.txt";
      if (!ofd.ShowDialog().Value) return;
      My.ProgramViewModel.Import(ofd.FileName);
    }
  }
  public class CloseWindowCommand : ICommand
  {
    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
      return true;
    }

    public void Execute(object parameter)
    {
      (parameter as Window).Close();
    }
  }
}
