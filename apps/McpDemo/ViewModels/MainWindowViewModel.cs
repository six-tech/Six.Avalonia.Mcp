using System.Collections.ObjectModel;
using System.Windows.Input;
using Six.Avalonia.Mcp.App.Shared;
using Six.Avalonia.Mcp.App.Shared.ViewModels;

namespace Six.Avalonia.Mcp.App.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
  private string _statusMessage = "Ready";
  private string _inputText = string.Empty;
  private bool _isToggled;
  private double _sliderValue;
  private int _clickCount;
  private string _clickCountMessage = "Clicks: 0";
  private string _dialogResult = "No dialog opened";
  private string _contextMenuResult = "No menu action";
  private double _progressValue = 42;
  private double _numericValue = 10;
  private double _thumbX = 20;
  private double _thumbY = 20;
  private string _hoverStatus = "Not hovered";

  public MainWindowViewModel()
  {
    Items = new ObservableCollection<string>
    {
      "Item 1",
      "Item 2",
      "Item 3",
      "Item 4",
      "Item 5"
    };

    ScrollableItems = new ObservableCollection<string>();
    for(int i = 1; i <= 50; i++)
    {
      ScrollableItems.Add($"Scroll Item {i}");
    }

    IncrementCommand = new RelayCommand(OnIncrement);
    CopyCommand = new RelayCommand(() => ContextMenuResult = "Copied");
    PasteCommand = new RelayCommand(() => ContextMenuResult = "Pasted");
    DeleteCommand = new RelayCommand(() => ContextMenuResult = "Deleted");
  }

  public string StatusMessage
  {
    get => _statusMessage;
    set => SetField(ref _statusMessage, value);
  }

  public string InputText
  {
    get => _inputText;
    set => SetField(ref _inputText, value);
  }

  public bool IsToggled
  {
    get => _isToggled;
    set => SetField(ref _isToggled, value);
  }

  public double SliderValue
  {
    get => _sliderValue;
    set => SetField(ref _sliderValue, value);
  }

  public int ClickCount
  {
    get => _clickCount;
    set
    {
      if (SetField(ref _clickCount, value))
      {
        ClickCountMessage = $"Clicks: {value}";
      }
    }
  }

  public string ClickCountMessage
  {
    get => _clickCountMessage;
    set => SetField(ref _clickCountMessage, value);
  }

  public string DialogResult
  {
    get => _dialogResult;
    set => SetField(ref _dialogResult, value);
  }

  public string ContextMenuResult
  {
    get => _contextMenuResult;
    set => SetField(ref _contextMenuResult, value);
  }

  public ObservableCollection<string> Items { get; }

  public ObservableCollection<string> ScrollableItems { get; }

  public double ProgressValue
  {
    get => _progressValue;
    set => SetField(ref _progressValue, value);
  }

  public double NumericValue
  {
    get => _numericValue;
    set => SetField(ref _numericValue, value);
  }

  public double ThumbX
  {
    get => _thumbX;
    set => SetField(ref _thumbX, value);
  }

  public double ThumbY
  {
    get => _thumbY;
    set => SetField(ref _thumbY, value);
  }

  public string HoverStatus
  {
    get => _hoverStatus;
    set => SetField(ref _hoverStatus, value);
  }

  public ICommand IncrementCommand { get; }

  public ICommand CopyCommand { get; }

  public ICommand PasteCommand { get; }

  public ICommand DeleteCommand { get; }

  public void OnActionButtonClick()
  {
    StatusMessage = $"Button clicked at {DateTime.Now:HH:mm:ss.fff}";
  }

  private void OnIncrement()
  {
    ClickCount++;
    StatusMessage = $"Increment clicked. Count: {ClickCount}";
  }
}
