using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using YTY.ScxLib;

namespace YTY.AocScenarioTranslator
{
  public class ProgramViewModel : INotifyPropertyChanged
  {
    private List<NodeViewModel> nodes;
    private Encoding fromEncoding;
    private Encoding toEncoding;
    private List<Encoding> fromEncodings;
    private List<Encoding> toEncodings;
    private ScxFile scx;
    private bool hide;
    private bool notTranslatedHint;
    private bool sourceErrorHint;
    private bool destErrorHint;

    public bool FileOpened { get; set; }
    public string Prefix { get; set; }

    public ScxFile Scx
    {
      get { return scx; }
      set
      {
        scx = value;
        if (scx == null)
        {
          Nodes = null;
          FileOpened = false;
        }
        else
        {
          var nodes = new List<NodeViewModel>();
          nodes.Add(new NodeViewModel() { Header = "剧情任务指示", Type = NodeType.StringInfo, Index = -1, SourceBytes = scx.Instruction });
          nodes.Add(new NodeViewModel() { Header = "任务", Type = NodeType.StringInfo, Index = 0, SourceBytes = scx.StringInfos[0] });
          nodes.Add(new NodeViewModel() { Header = "提示", Type = NodeType.StringInfo, Index = 1, SourceBytes = scx.StringInfos[1] });
          nodes.Add(new NodeViewModel() { Header = "胜利", Type = NodeType.StringInfo, Index = 2, SourceBytes = scx.StringInfos[2] });
          nodes.Add(new NodeViewModel() { Header = "失败", Type = NodeType.StringInfo, Index = 3, SourceBytes = scx.StringInfos[3] });
          nodes.Add(new NodeViewModel() { Header = "历史", Type = NodeType.StringInfo, Index = 4, SourceBytes = scx.StringInfos[4] });
          nodes.Add(new NodeViewModel() { Header = "侦察", Type = NodeType.StringInfo, Index = 5, SourceBytes = scx.StringInfos[5] });
          for (var i = 0; i < scx.PlayerCount; i++)
          {
            nodes.Add(new NodeViewModel() { Header = $"玩家 {i + 1} 名称", Type = NodeType.PlayerName, Index = i, SourceBytes = scx.Players[i].Name });
          }
          for (var i = 0; i < scx.Triggers.Count; i++)
          {
            var node = new NodeViewModel(false) { Header = $"触发 {i + 1}", Type = NodeType.Trigger, Index = i };
            if (Convert.ToBoolean(scx.Triggers[i].IsObjective))
              node.IsObjective = true;
            node.Children.Add(new NodeViewModel() { Header = "名称", Type = NodeType.TriggerName, Index = i, SourceBytes = scx.Triggers[i].Name });
            node.Children.Add(new NodeViewModel() { Header = "描述", Type = NodeType.TriggerDesc, Index = i, SourceBytes = scx.Triggers[i].Discription });
            for (var j = 0; j < scx.Triggers[i].Effects.Count; j++)
            {
              switch (scx.Triggers[i].Effects[j].Type)
              {
                case EffectType.SendChat:
                  node.Children.Add(new NodeViewModel() { Header = $"效果 {j}：送出聊天", Type = NodeType.TriggerContent, Index = i, SubIndex = j, SourceBytes = scx.Triggers[i].Effects[j].Text });
                  break;
                case EffectType.DisplayInstructions:
                  node.Children.Add(new NodeViewModel() { Header = $"效果 {j}：显示指示", Type = NodeType.TriggerContent, Index = i, SubIndex = j, SourceBytes = scx.Triggers[i].Effects[j].Text });
                  break;
                case EffectType.ChangeObjectName:
                  node.Children.Add(new NodeViewModel() { Header = $"效果 {j}：改变物件名称", Type = NodeType.TriggerContent, Index = i, SubIndex = j, SourceBytes = scx.Triggers[i].Effects[j].Text });
                  break;
              }
            }
            nodes.Add(node);
          }
          Nodes = nodes;
          FileOpened = true;
          Hide = Hide;
        }
        OnPropertyChanged(nameof(Scx));
        OnPropertyChanged(nameof(FileOpened));
      }
    }

    public List<NodeViewModel> GetAllNodes()
    {
      return nodes.Concat(nodes.SelectMany(n => n.Children)).ToList();
    }

    public void Export(string fileName)
    {
      using (var sw = new StreamWriter(fileName))
      {
        sw.WriteLine("//场景指示");
        sw.WriteLine(Nodes.FirstOrDefault(n => n.Type == NodeType.StringInfo && n.Index == -1).To);
        sw.WriteLine("//场景指南");
        sw.WriteLine(Nodes.FirstOrDefault(n => n.Type == NodeType.StringInfo && n.Index == 0).To);
        sw.WriteLine("//提示");
        sw.WriteLine(Nodes.FirstOrDefault(n => n.Type == NodeType.StringInfo && n.Index == 1).To);
        sw.WriteLine("//胜利");
        sw.WriteLine(Nodes.FirstOrDefault(n => n.Type == NodeType.StringInfo && n.Index == 2).To);
        sw.WriteLine("//失败");
        sw.WriteLine(Nodes.FirstOrDefault(n => n.Type == NodeType.StringInfo && n.Index == 3).To);
        sw.WriteLine("//历史");
        sw.WriteLine(Nodes.FirstOrDefault(n => n.Type == NodeType.StringInfo && n.Index == 4).To);
        sw.WriteLine("//侦察");
        sw.WriteLine(Nodes.FirstOrDefault(n => n.Type == NodeType.StringInfo && n.Index == 5).To);
        foreach (var p in Nodes.Where(n => n.Type == NodeType.PlayerName))
        {
          sw.WriteLine($"//玩家{p.Index + 1}名称");
          sw.WriteLine(p.To);
        }
        foreach (var t in Nodes.Where(n => n.Type == NodeType.Trigger && n.Visibility == Visibility.Visible))
        {
          foreach (var s in t.Children.Where(s => s.Visibility == Visibility.Visible))
          {
            sw.Write($"//触发{t.Index}");
            switch (s.Type)
            {
              case NodeType.TriggerName:
                sw.WriteLine("名称");
                break;
              case NodeType.TriggerDesc:
                sw.WriteLine("描述");
                break;
              case NodeType.TriggerContent:
                sw.WriteLine($"效果{s.SubIndex}");
                break;
            }
            sw.WriteLine(s.To);
          }
        }
        sw.Write("//结束");
      }
    }

    public void Import(string fileName)
    {
      using (var sr = new StreamReader(fileName, Encoding.GetEncoding(65001, EncoderFallback.ReplacementFallback, DecoderFallback.ExceptionFallback)))
      {
        var playerNameRegex = new Regex(@"玩家([1-8])名称");
        var triggerNameRegex = new Regex(@"触发(\d+)名称");
        var triggerDescRegex = new Regex(@"触发(\d+)描述");
        var triggerEffectRegex = new Regex(@"触发(\d+)效果(\d+)");
        var type = NodeType.None;
        var index = 0;
        var subIndex = 0;
        var buffer = string.Empty;
        try
        {
          while (!sr.EndOfStream)
          {
            var line = sr.ReadLine();
            if (line.TrimStart().StartsWith("//"))
            {
              switch (type)
              {
                case NodeType.StringInfo:
                case NodeType.PlayerName:
                  Nodes.FirstOrDefault(n => n.Type == type && n.Index == index).To = buffer.TrimEnd('\n');
                  break;
                case NodeType.TriggerName:
                case NodeType.TriggerDesc:
                  GetAllNodes().FirstOrDefault(n => n.Type == type && n.Index == index).To = buffer.TrimEnd('\n');
                  break;
                case NodeType.TriggerContent:
                  GetAllNodes().FirstOrDefault(n => n.Type == type && n.Index == index && n.SubIndex == subIndex).To = buffer.TrimEnd('\n');
                  break;
                default:
                  break;
              }
              buffer = string.Empty;
              if (line.Contains("场景指示"))
              {
                type = NodeType.StringInfo;
                index = -1;
                continue;
              }
              if (line.Contains("场景指南"))
              {
                type = NodeType.StringInfo;
                index = 0;
                continue;
              }
              if (line.Contains("提示"))
              {
                type = NodeType.StringInfo;
                index = 1;
                continue;
              }
              if (line.Contains("胜利"))
              {
                type = NodeType.StringInfo;
                index = 2;
                continue;
              }
              if (line.Contains("失败"))
              {
                type = NodeType.StringInfo;
                index = 3;
                continue;
              }
              if (line.Contains("历史"))
              {
                type = NodeType.StringInfo;
                index = 4;
                continue;
              }
              if (line.Contains("侦察"))
              {
                type = NodeType.StringInfo;
                index = 5;
                continue;
              }
              if (line.Contains("结束"))
                break;
              var match = playerNameRegex.Match(line);
              if (match.Success)
              {
                type = NodeType.PlayerName;
                index = int.Parse(match.Groups[1].Value) - 1;
                continue;
              }
              match = triggerNameRegex.Match(line);
              if (match.Success)
              {
                type = NodeType.TriggerName;
                index = int.Parse(match.Groups[1].Value);
                continue;
              }
              match = triggerDescRegex.Match(line);
              if (match.Success)
              {
                type = NodeType.TriggerDesc;
                index = int.Parse(match.Groups[1].Value);
                continue;
              }
              match = triggerEffectRegex.Match(line);
              if (match.Success)
              {
                type = NodeType.TriggerContent;
                index = int.Parse(match.Groups[1].Value);
                subIndex = int.Parse(match.Groups[2].Value);
                continue;
              }
              type = NodeType.None;
            }
            else
            {
              buffer += line + "\r\n";
            }
          }
        }
        catch (DecoderFallbackException ex)
        {
          MessageBox.Show($"该文件不是合法的 UTF-8 格式，无法载入。\n\n【错误详情】\n{ex.Message}");
        }
      }
    }

    public void ApplyChanges()
    {
      foreach (var n in GetAllNodes())
      {
        var bytes = n.ToBytes ?? new byte[] { 0 };
        switch (n.Type)
        {
          case NodeType.StringInfo:
            if (n.Index == -1)
              scx.Instruction = bytes;
            else
              Scx.StringInfos[n.Index] = bytes;
            break;
          case NodeType.PlayerName:
            Scx.Players[n.Index].Name = bytes;
            break;
          case NodeType.TriggerName:
            Scx.Triggers[n.Index].Name = bytes;
            break;
          case NodeType.TriggerDesc:
            Scx.Triggers[n.Index].Discription = bytes;
            break;
          case NodeType.TriggerContent:
            Scx.Triggers[n.Index].Effects[n.SubIndex].Text = bytes;
            break;
        }
      }
    }

    public List<Encoding> FromEncodings
    {
      get
      {
        if (fromEncodings == null)
        {
          var codepages = Encoding.GetEncodings().Select(e => Encoding.GetEncoding(e.CodePage).WindowsCodePage).Distinct().Concat(new[] { 65001 });
          fromEncodings = codepages.Select(cp => Encoding.GetEncoding(cp, EncoderFallback.ReplacementFallback, DecoderFallback.ExceptionFallback)).ToList();
        }
        return fromEncodings;
      }
    }

    public List<Encoding> ToEncodings
    {
      get
      {
        if (toEncodings == null)
        {
          var codepages = Encoding.GetEncodings().Select(e => Encoding.GetEncoding(e.CodePage).WindowsCodePage).Distinct().Concat(new[] { 65001 });
          toEncodings = codepages.Select(cp => Encoding.GetEncoding(cp, EncoderFallback.ExceptionFallback, DecoderFallback.ReplacementFallback)).ToList();
        }
        return toEncodings;
      }
    }

    public Encoding FromEncoding
    {
      get { return fromEncoding; }
      set
      {
        fromEncoding = value;
        OnPropertyChanged(nameof(FromEncoding));
      }
    }

    public Encoding ToEncoding
    {
      get { return toEncoding; }
      set
      {
        toEncoding = value;
        OnPropertyChanged(nameof(ToEncoding));
      }
    }

    public List<NodeViewModel> Nodes
    {
      get { return nodes; }
      set
      {
        nodes = value;
        OnPropertyChanged(nameof(Nodes));
      }
    }

    public bool Hide
    {
      get { return hide; }
      set
      {
        hide = value;
        OnPropertyChanged(nameof(Hide));
      }
    }

    public bool NotTranslatedHint
    {
      get { return notTranslatedHint; }
      set
      {
        notTranslatedHint = value;
        OnPropertyChanged(nameof(NotTranslatedHint));
      }
    }

    public bool SourceErrorHint
    {
      get { return sourceErrorHint; }
      set
      {
        sourceErrorHint = value;
        OnPropertyChanged(nameof(SourceErrorHint));
      }
    }

    public bool DestErrorHint
    {
      get { return destErrorHint; }
      set
      {
        destErrorHint = value;
        OnPropertyChanged(nameof(DestErrorHint));
      }
    }

    public ProgramViewModel()
    {
      Prefix = "触发事件 ";
      Hide = true;
      SourceErrorHint = true;
      DestErrorHint = true;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
