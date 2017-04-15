using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace YTY.AocScenarioTranslator
{
  public class NodeViewModel : INotifyPropertyChanged
  {
    private bool sourceError;
    private bool destError;
    private bool notTranslatedHint;
    private string header;
    private byte[] sourceBytes;
    private string source;
    private byte[] toBytes;
    private string to;
    private List<NodeViewModel> children;
    private Visibility visibility;
    private string sourceErrorText;
    private string destErrorText;

    public bool HasContent { get; set; }

    public string Header
    {
      get { return header; }
      set
      {
        header = value;
        OnPropertyChanged(nameof(Header));
      }
    }

    public byte[] SourceBytes
    {
      get { return sourceBytes; }
      set
      {
        sourceBytes = value;
        SetSource();
      }
    }

    public string Source
    {
      get { return source; }
      set
      {
        source = value;
        OnPropertyChanged(nameof(Source));
      }
    }

    public bool SourceError
    {
      get { return sourceError; }
      set
      {
        sourceError = value;
        OnPropertyChanged(nameof(SourceError));
      }
    }

    public bool DestError
    {
      get { return destError; }
      set
      {
        destError = value;
        OnPropertyChanged(nameof(DestError));
      }
    }

    public string To
    {
      get { return to; }
      set
      {
        to = value;
        OnPropertyChanged(nameof(To));
        OnSetDest();
      }
    }

    public byte[] ToBytes { get { return toBytes; } }

    public List<NodeViewModel> Children
    {
      get { return children; }
      set
      {
        children = value;
        OnPropertyChanged(nameof(Children));
      }
    }

    public Visibility Visibility
    {
      get { return visibility; }
      set
      {
        visibility = value;
        OnPropertyChanged(nameof(Visibility));
      }
    }

    public string SourceErrorText
    {
      get { return sourceErrorText; }
      set
      {
        sourceErrorText = value;
        OnPropertyChanged(nameof(SourceErrorText));
      }
    }

    public string DestErrorText
    {
      get { return destErrorText; }
      set
      {
        destErrorText = value;
        OnPropertyChanged(nameof(DestErrorText));
      }
    }

    public NodeType Type { get; set; }

    public int Index { get; set; }

    public int SubIndex { get; set; }

    public bool IsObjective { get; set; }

    public bool NotTranslatedHint
    {
      get { return notTranslatedHint; }
      set
      {
        notTranslatedHint = value;
        OnPropertyChanged(nameof(NotTranslatedHint));
      }
    }

    public NodeViewModel(bool hasContent = true)
    {
      children = new List<NodeViewModel>();
      HasContent = hasContent;
      My.ProgramViewModel.PropertyChanged += ScenarioTranslatorViewModel_PropertyChanged;
      To = string.Empty;
    }

    private void SetSource()
    {
      if (!HasContent) return;
      if (My.ProgramViewModel.SourceErrorHint)
      {
        try
        {
          SourceError = false;
          Source = My.ProgramViewModel.FromEncoding.GetString(sourceBytes).TrimEnd('\0');
        }
        catch (DecoderFallbackException ex)
        {
          SourceError = true;
          SourceErrorText = $"【警告】位于字节流位置 {ex.Index} 处的字节 {BitConverter.ToString(ex.BytesUnknown)} 无法使用 {My.ProgramViewModel.FromEncoding.EncodingName} 解码。\n请检查原文编码是否合适。";
          Source = Encoding.GetEncoding(My.ProgramViewModel.FromEncoding.CodePage).GetString(sourceBytes).TrimEnd('\0');
        }
      }
      else
      {
        SourceError = false;
        Source = Encoding.GetEncoding(My.ProgramViewModel.FromEncoding.CodePage).GetString(sourceBytes).TrimEnd('\0');
      }
    }

    private void OnSetDest()
    {
      if (My.ProgramViewModel.NotTranslatedHint)
      {
        NotTranslatedHint = !CalcIfTranslated();
      }
      else
      {
        NotTranslatedHint = false;
      }

      if (My.ProgramViewModel.DestErrorHint)
      {
        try
        {
          DestError = false;
          toBytes = My.ProgramViewModel.ToEncoding.GetBytes(To + '\0');
        }
        catch (EncoderFallbackException ex)
        {
          DestError = true;
          DestErrorText = $"【警告】位于字符串位置 {ex.Index} 处的字符 {ex.CharUnknown} 无法使用 {My.ProgramViewModel.ToEncoding.EncodingName} 编码。\n请检查译文字符串是否错误，译文编码是否合适。";
          toBytes = Encoding.GetEncoding(My.ProgramViewModel.ToEncoding.CodePage).GetBytes(To + '\0');
        }
      }
      else
      {
        DestError = false;
        toBytes = Encoding.GetEncoding(My.ProgramViewModel.ToEncoding.CodePage).GetBytes(To + '\0');
      }
    }

    private void ScenarioTranslatorViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      switch (e.PropertyName)
      {
        case nameof(ProgramViewModel.FromEncoding):
        case nameof(ProgramViewModel.SourceErrorHint):
          SetSource();
          break;
        case nameof(ProgramViewModel.ToEncoding):
        case nameof(ProgramViewModel.DestErrorHint):
        case nameof(ProgramViewModel.NotTranslatedHint):
          OnSetDest();
          break;
        case nameof(ProgramViewModel.Hide):
          Visibility = CalcVisibility();
          break;
      }
    }

    private Visibility CalcVisibility()
    {
      if (!My.ProgramViewModel.Hide)
        return Visibility.Visible;
      if (Type == NodeType.TriggerName)
        return Visibility.Collapsed;
      if (Type == NodeType.TriggerDesc || Type == NodeType.TriggerContent)
        return string.IsNullOrWhiteSpace(Source) ? Visibility.Collapsed : Visibility.Visible;
      if (Type != NodeType.Trigger)
        return Visibility.Visible;
      if (IsObjective)
        return Visibility.Visible;
      if (Children.Where(n => n.Type == NodeType.TriggerContent || n.Type == NodeType.TriggerDesc).Any(n => !string.IsNullOrWhiteSpace(n.Source)))
        return Visibility.Visible;
      return Visibility.Collapsed;
    }

    private bool CalcIfTranslated()
    {
      if (Type == NodeType.Trigger)
        return true;
      if (string.IsNullOrWhiteSpace(To))
        return false;
      if (Source.Equals(To))
        return false;
      return true;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }

  public enum NodeType
  {
    None,
    StringInfo,
    PlayerName,
    Trigger,
    TriggerName,
    TriggerContent,
    TriggerDesc
  }
}
