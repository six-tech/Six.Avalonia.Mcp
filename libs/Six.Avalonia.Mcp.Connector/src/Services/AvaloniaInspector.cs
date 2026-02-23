using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Six.Avalonia.Mcp.Contracts.Models;

namespace Six.Avalonia.Mcp.Connector.Services;

/// <summary>
/// Inspects and interacts with a running Avalonia application's visual tree.
/// All Avalonia-touching logic must be dispatched to the UI thread.
/// </summary>
[PublicAPI]
public class AvaloniaInspector : IAvaloniaInspector
{
  private readonly ILogger<AvaloniaInspector> _logger;

  public AvaloniaInspector(ILogger<AvaloniaInspector> logger)
  {
    _logger = logger;
  }

  /// <summary>
  /// Gets the top-level visual root from the current application lifetime.
  /// Supports both classic desktop (Window) and single-view (mobile/WASM) lifetimes.
  /// Returns null and logs a warning if the application or top-level is unavailable.
  /// </summary>
  protected virtual TopLevel? GetTopLevel()
  {
    if(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
      // Prefer the topmost active window (e.g., modal dialog) over MainWindow.
      // Iterate in reverse since the last window is typically the most recently opened.
      for(int i = desktop.Windows.Count - 1; i >= 0; i--)
      {
        Window w = desktop.Windows[i];
        if(w.IsActive)
        {
          return w;
        }
      }

      return desktop.MainWindow;
    }

    if(Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView
       && singleView.MainView is { } mainView)
    {
      return TopLevel.GetTopLevel(mainView);
    }

    _logger.LogWarning("Could not resolve TopLevel from application lifetime");
    return null;
  }

  /// <summary>
  /// Returns all open TopLevels in the application, ordered topmost-first.
  /// For desktop lifetime, returns all windows in reverse order.
  /// For single-view, returns a single-element list.
  /// </summary>
  protected virtual IReadOnlyList<TopLevel> GetAllTopLevels()
  {
    if(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
      List<TopLevel> result = new(desktop.Windows.Count);
      for(int i = desktop.Windows.Count - 1; i >= 0; i--)
      {
        result.Add(desktop.Windows[i]);
      }

      return result;
    }

    if(Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView
       && singleView.MainView is { } mainView)
    {
      TopLevel? topLevel = TopLevel.GetTopLevel(mainView);
      if(topLevel is not null)
      {
        return [topLevel];
      }
    }

    return [];
  }

  /// <summary>
  /// Returns a specific TopLevel by window index, or falls back to <see cref="GetTopLevel"/>.
  /// </summary>
  protected TopLevel ResolveTopLevelOrThrow(int? windowIndex = null)
  {
    if(windowIndex is not null)
    {
      IReadOnlyList<TopLevel> all = GetAllTopLevels();
      if(windowIndex.Value < 0 || windowIndex.Value >= all.Count)
      {
        throw new ArgumentOutOfRangeException(nameof(windowIndex),
          $"Window index {windowIndex.Value} is out of range. There are {all.Count} window(s).");
      }

      return all[windowIndex.Value];
    }

    return GetTopLevel()
           ?? throw new InvalidOperationException("No TopLevel is available.");
  }

  /// <summary>
  /// Resolves a visual element from the tree using name, type name, or index path.
  /// Resolution order: name → typeName → path. Returns null if not found or all parameters are null.
  /// </summary>
  internal static Visual? ResolveElement(Visual root, string? name, string? typeName, string? path)
  {
    if(name is not null)
    {
      return FindByName(root, name);
    }

    if(typeName is not null)
    {
      return FindByTypeName(root, typeName);
    }

    if(path is not null)
    {
      return NavigateByPath(root, path);
    }

    return null;
  }

  /// <summary>
  /// Resolves an element across all windows, returning the containing TopLevel and the found element.
  /// If <paramref name="windowIndex"/> is specified, only that window is searched.
  /// Otherwise, all windows from <see cref="GetAllTopLevels"/> are tried in order.
  /// </summary>
  internal (TopLevel Root, Visual Element) ResolveElementAcrossWindows(
    string? name, string? typeName, string? path, int? windowIndex = null)
  {
    if(windowIndex is not null)
    {
      TopLevel root = ResolveTopLevelOrThrow(windowIndex);
      Visual element = ResolveElement(root, name, typeName, path)
                       ?? throw new InvalidOperationException(
                         $"Element not found in window {windowIndex.Value}. Provide elementName, typeName, or path.");
      return (root, element);
    }

    IReadOnlyList<TopLevel> allWindows = GetAllTopLevels();
    if(allWindows.Count == 0)
    {
      throw new InvalidOperationException("No TopLevel is available.");
    }

    foreach(TopLevel top in allWindows)
    {
      Visual? found = ResolveElement(top, name, typeName, path);
      if(found is not null)
      {
        TopLevel actualRoot = TopLevel.GetTopLevel(found) ?? top;
        return (actualRoot, found);
      }
    }

    // Fall back to the primary TopLevel for better error messages
    TopLevel primary = GetTopLevel()
                       ?? throw new InvalidOperationException("No TopLevel is available.");
    throw new InvalidOperationException(
      $"Element not found in any window. Provide elementName, typeName, or path.");
  }

  private static Visual? FindByName(Visual current, string name)
  {
    if(current.Name == name)
    {
      return current;
    }

    if(current is Control { ContextMenu: { IsOpen: true } menu })
    {
      Visual? inMenu = FindByName(menu, name);
      if(inMenu is not null) return inMenu;
    }

    foreach(Visual child in current.GetVisualChildren())
    {
      Visual? found = FindByName(child, name);
      if(found is not null)
      {
        return found;
      }
    }

    return null;
  }

  private static Visual? FindByTypeName(Visual current, string typeName)
  {
    if(current.GetType().Name == typeName)
    {
      return current;
    }

    if(current is Control { ContextMenu: { IsOpen: true } menu })
    {
      Visual? inMenu = FindByTypeName(menu, typeName);
      if(inMenu is not null) return inMenu;
    }

    foreach(Visual child in current.GetVisualChildren())
    {
      Visual? found = FindByTypeName(child, typeName);
      if(found is not null)
      {
        return found;
      }
    }

    return null;
  }

  private static Visual? NavigateByPath(Visual root, string path)
  {
    string[] indices = path.Split('/');
    Visual current = root;

    foreach(string indexStr in indices)
    {
      if(!int.TryParse(indexStr, out int index))
      {
        return null;
      }

      List<Visual> children = current.GetVisualChildren().ToList();
      if(index < 0 || index >= children.Count)
      {
        return null;
      }

      current = children[index];
    }

    return current;
  }

  /// <summary>
  /// Builds a list of child indices from root to target using DFS.
  /// Returns an empty list if target is root or not found in the tree.
  /// </summary>
  internal static IReadOnlyList<int> BuildPath(Visual root, Visual target)
  {
    if(ReferenceEquals(root, target))
    {
      return Array.Empty<int>();
    }

    List<int> path = new();
    if(BuildPathDfs(root, target, path))
    {
      return path;
    }

    return Array.Empty<int>();
  }

  private static bool BuildPathDfs(Visual current, Visual target, List<int> path)
  {
    List<Visual> children = current.GetVisualChildren().ToList();
    for(int i = 0; i < children.Count; i++)
    {
      if(ReferenceEquals(children[i], target))
      {
        path.Add(i);
        return true;
      }

      path.Add(i);
      if(BuildPathDfs(children[i], target, path))
      {
        return true;
      }

      path.RemoveAt(path.Count - 1);
    }

    return false;
  }

  /// <summary>
  /// Generates a stable element ID in the format "path:0/2/1" using <see cref="BuildPath"/>.
  /// </summary>
  internal static string GenerateElementId(Visual root, Visual target)
  {
    IReadOnlyList<int> path = BuildPath(root, target);
    return "path:" + string.Join("/", path);
  }

  /// <summary>
  /// Builds a <see cref="VisualTreeNode"/> from the given visual, recursively including children up to maxDepth.
  /// </summary>
  internal static VisualTreeNode BuildTreeNode(Visual visual, int currentDepth, int maxDepth)
  {
    Control? control = visual as Control;
    InputElement? inputElement = visual as InputElement;
    StyledElement? styledElement = visual as StyledElement;

    List<VisualTreeNode> children = new();
    if(currentDepth < maxDepth)
    {
      foreach(Visual child in visual.GetVisualChildren())
      {
        children.Add(BuildTreeNode(child, currentDepth + 1, maxDepth));
      }
    }

    List<string> classes = new();
    foreach(string? cls in styledElement.Classes)
    {
      classes.Add(cls);
    }

    return new VisualTreeNode
    {
      Type = visual.GetType().Name,
      Name = control?.Name,
      Bounds = new BoundsInfo
      {
        X = visual.Bounds.X, Y = visual.Bounds.Y, Width = visual.Bounds.Width, Height = visual.Bounds.Height
      },
      IsVisible = control?.IsVisible ?? true,
      IsEnabled = control?.IsEffectivelyEnabled ?? true,
      Opacity = visual.Opacity,
      IsFocused = inputElement?.IsFocused ?? false,
      IsPointerOver = inputElement?.IsPointerOver ?? false,
      Classes = classes,
      Children = children
    };
  }

  /// <inheritdoc />
  public async Task<AppMetadata> GetAppMetadataAsync(CancellationToken ct = default)
  {
    return await Dispatcher.UIThread.InvokeAsync(() =>
    {
      TopLevel topLevel = GetTopLevel()
                      ?? throw new InvalidOperationException("No TopLevel is available.");

      using Process process = Process.GetCurrentProcess();

      string avaloniaVersion = typeof(Application).Assembly.GetName().Version?.ToString() ?? "unknown";

      return new AppMetadata
      {
        ProcessName = process.ProcessName,
        ProcessId = process.Id,
        MainWindowTitle = (topLevel as Window)?.Title,
        AvaloniaVersion = avaloniaVersion,
        DotNetVersion = Environment.Version.ToString(),
        WindowCount = GetAllTopLevels().Count,
        WindowWidth = topLevel.Bounds.Width,
        WindowHeight = topLevel.Bounds.Height
      };
    });
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<WindowInfo>> ListWindowsAsync(CancellationToken ct = default)
  {
    return await Dispatcher.UIThread.InvokeAsync(() =>
    {
      IReadOnlyList<TopLevel> allTopLevels = GetAllTopLevels();
      List<WindowInfo> windows = new(allTopLevels.Count);

      for(int i = 0; i < allTopLevels.Count; i++)
      {
        TopLevel tl = allTopLevels[i];
        windows.Add(new WindowInfo
        {
          Index = i,
          Title = (tl as Window)?.Title,
          Type = tl.GetType().Name,
          Bounds = new BoundsInfo
          {
            X = tl.Bounds.X, Y = tl.Bounds.Y, Width = tl.Bounds.Width, Height = tl.Bounds.Height
          },
          IsActive = (tl as Window)?.IsActive ?? false,
          IsVisible = (tl as Window)?.IsVisible ?? true
        });
      }

      return (IReadOnlyList<WindowInfo>)windows;
    });
  }

  /// <inheritdoc />
  public async Task<VisualTreeNode> GetVisualTreeAsync(int maxDepth = 15, string? elementName = null,
    int? windowIndex = null, CancellationToken ct = default)
  {
    return await Dispatcher.UIThread.InvokeAsync(() =>
    {
      int clampedDepth = Math.Clamp(maxDepth, 1, 50);

      TopLevel topLevel = ResolveTopLevelOrThrow(windowIndex);

      Visual root = topLevel;
      if(elementName is not null)
      {
        root = ResolveElement(topLevel, elementName, null, null)
               ?? throw new InvalidOperationException($"Element '{elementName}' not found.");
      }

      return BuildTreeNode(root, 0, clampedDepth);
    });
  }

  /// <summary>
  /// Parses a simple CSS-like selector into type, name, and class components.
  /// Supports: "Button#myBtn", "TextBox.error", "#myBtn", ".error", "Button".
  /// </summary>
  internal static (string? Type, string? Name, string? ClassName) ParseSelector(string selector)
  {
    string? type = null;
    string? name = null;
    string? className = null;

    int hashIndex = selector.IndexOf('#');
    int dotIndex = selector.IndexOf('.');

    if(hashIndex >= 0)
    {
      type = hashIndex > 0 ? selector[..hashIndex] : null;
      name = selector[(hashIndex + 1)..];
    }
    else if(dotIndex >= 0)
    {
      type = dotIndex > 0 ? selector[..dotIndex] : null;
      className = selector[(dotIndex + 1)..];
    }
    else
    {
      type = selector;
    }

    return (type, name, className);
  }

  /// <summary>
  /// Checks whether a visual matches the given filter criteria. All non-null criteria must match.
  /// </summary>
  internal static bool MatchesFilter(Visual visual, string? name, string? typeName, string? className)
  {
    if(name is not null && visual.Name != name)
    {
      return false;
    }

    if(typeName is not null && visual.GetType().Name != typeName)
    {
      return false;
    }

    if(className is not null)
    {
      if(visual is StyledElement styled)
      {
        if(!styled.Classes.Contains(className))
        {
          return false;
        }
      }
      else
      {
        return false;
      }
    }

    return true;
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<FindElementResult>> FindElementAsync(
    string? name = null,
    string? typeName = null,
    string? className = null,
    string? selector = null,
    int maxResults = 10,
    int? windowIndex = null,
    CancellationToken ct = default)
  {
    return await Dispatcher.UIThread.InvokeAsync(() =>
    {
      // If selector is provided, parse it into name/typeName/className
      string? filterName = name;
      string? filterType = typeName;
      string? filterClass = className;

      if(selector is not null)
      {
        (string? sType, string? sName, string? sClass) = ParseSelector(selector);
        filterName ??= sName;
        filterType ??= sType;
        filterClass ??= sClass;
      }

      List<FindElementResult> results = new();

      if(windowIndex is not null)
      {
        TopLevel topLevel = ResolveTopLevelOrThrow(windowIndex);
        WalkTree(topLevel, topLevel, filterName, filterType, filterClass, maxResults, results, windowIndex.Value);
      }
      else
      {
        // Search all windows
        IReadOnlyList<TopLevel> allWindows = GetAllTopLevels();
        if(allWindows.Count == 0)
        {
          throw new InvalidOperationException("No TopLevel is available.");
        }

        for(int i = 0; i < allWindows.Count && results.Count < maxResults; i++)
        {
          WalkTree(allWindows[i], allWindows[i], filterName, filterType, filterClass, maxResults, results, i);
        }
      }

      return (IReadOnlyList<FindElementResult>)results;
    });
  }

  private static void WalkTree(Visual root, Visual current, string? name, string? typeName, string? className, int maxResults,
    List<FindElementResult> results, int? windowIndex = null)
  {
    if(results.Count >= maxResults)
    {
      return;
    }

    if(MatchesFilter(current, name, typeName, className))
    {
      IReadOnlyList<int> path = BuildPath(root, current);
      results.Add(new FindElementResult
      {
        Type = current.GetType().Name,
        Name = (current as Control)?.Name,
        Bounds = new BoundsInfo
        {
          X = current.Bounds.X, Y = current.Bounds.Y, Width = current.Bounds.Width, Height = current.Bounds.Height
        },
        Path = path,
        ElementId = GenerateElementId(root, current),
        WindowIndex = windowIndex
      });
    }

    if(current is Control { ContextMenu: { IsOpen: true } menu })
    {
      WalkTree(root, menu, name, typeName, className, maxResults, results, windowIndex);
    }

    foreach(Visual child in current.GetVisualChildren())
    {
      if(results.Count >= maxResults)
      {
        return;
      }

      WalkTree(root, child, name, typeName, className, maxResults, results, windowIndex);
    }
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<VisualTreeNode>> GetAncestorChainAsync(string elementName,
    int? windowIndex = null, CancellationToken ct = default)
  {
    return await Dispatcher.UIThread.InvokeAsync(() =>
    {
      (TopLevel _, Visual element) = ResolveElementAcrossWindows(elementName, null, null, windowIndex);

      List<VisualTreeNode> ancestors = new();
      Visual? current = element;

      while(current is not null)
      {
        ancestors.Add(BuildTreeNode(current, 0, 0));
        current = current.GetVisualParent();
      }

      return (IReadOnlyList<VisualTreeNode>)ancestors;
    });
  }

  /// <summary>
  /// Builds a detailed <see cref="ElementProperties"/> record from a resolved visual element.
  /// </summary>
  internal static ElementProperties BuildElementProperties(Visual root, Visual visual)
  {
    Control? control = visual as Control;
    InputElement? inputElement = visual as InputElement;
    StyledElement? styledElement = visual as StyledElement;

    List<string> classes = new();
    List<string> pseudoClasses = new();
    if(styledElement is not null)
    {
      foreach(string? cls in styledElement.Classes)
      {
        if(cls.StartsWith(':'))
        {
          pseudoClasses.Add(cls);
        }
        else
        {
          classes.Add(cls);
        }
      }
    }

    string? GetBrushString(AvaloniaProperty property)
    {
      return visual.GetValue(property)?.ToString();
    }

    return new ElementProperties
    {
      Type = visual.GetType().Name,
      Name = control?.Name,
      ElementId = GenerateElementId(root, visual),
      Bounds = new BoundsInfo
      {
        X = visual.Bounds.X, Y = visual.Bounds.Y, Width = visual.Bounds.Width, Height = visual.Bounds.Height
      },
      Width = visual.Bounds.Width,
      Height = visual.Bounds.Height,
      Margin = (control as Layoutable)?.Margin.ToString(),
      Padding = control switch
      {
        Decorator d => d.Padding.ToString(),
        ContentPresenter cp => cp.Padding.ToString(),
        TemplatedControl tc => tc.Padding.ToString(),
        _ => null
      },
      IsVisible = control?.IsVisible ?? true,
      IsEnabled = control?.IsEffectivelyEnabled ?? true,
      IsFocused = inputElement?.IsFocused ?? false,
      IsHitTestVisible = inputElement?.IsHitTestVisible ?? true,
      ClipToBounds = visual.ClipToBounds,
      Opacity = visual.Opacity,
      Background = visual.IsSet(Panel.BackgroundProperty) || visual is Panel
        ? GetBrushString(Panel.BackgroundProperty)
        : control is TemplatedControl tc2
          ? tc2.GetValue(TemplatedControl.BackgroundProperty)?.ToString()
          : null,
      Foreground =
        control is TemplatedControl tc3 ? tc3.GetValue(TemplatedControl.ForegroundProperty)?.ToString() : null,
      BorderBrush =
        control is TemplatedControl tc4 ? tc4.GetValue(TemplatedControl.BorderBrushProperty)?.ToString() : null,
      BorderThickness =
        control is TemplatedControl tc5 ? tc5.GetValue(TemplatedControl.BorderThicknessProperty).ToString() : null,
      CornerRadius =
        control is TemplatedControl tc6 ? tc6.GetValue(TemplatedControl.CornerRadiusProperty).ToString() : null,
      HorizontalAlignment = (control as Layoutable)?.HorizontalAlignment.ToString(),
      VerticalAlignment = (control as Layoutable)?.VerticalAlignment.ToString(),
      FlowDirection = (control as Visual)?.FlowDirection.ToString(),
      DataContextType = styledElement?.DataContext?.GetType().FullName,
      Text = visual switch
      {
        TextBlock tb => tb.Text,
        TextBox tbox => tbox.Text,
        ContentPresenter cp => cp.Content?.ToString(),
        HeaderedContentControl hcc => hcc.Header?.ToString(),
        ContentControl cc => cc.Content?.ToString(),
        _ => null
      },
      Classes = classes,
      PseudoClasses = pseudoClasses
    };
  }

  /// <inheritdoc />
  public async Task<ElementProperties> GetElementPropertiesAsync(
    string? elementName = null,
    string? typeName = null,
    string? path = null,
    int? windowIndex = null,
    CancellationToken ct = default)
  {
    return await Dispatcher.UIThread.InvokeAsync(() =>
    {
      (TopLevel root, Visual element) = ResolveElementAcrossWindows(elementName, typeName, path, windowIndex);
      return BuildElementProperties(root, element);
    });
  }

  /// <summary>
  /// Renders a visual to a PNG file at the given scale factor.
  /// Must be called on the UI thread.
  /// </summary>
  static ScreenshotResult RenderToFile(Visual visual, string outputPath, double scale)
  {
    Rect bounds = visual.Bounds;
    int pixelWidth = (int)(bounds.Width * scale);
    int pixelHeight = (int)(bounds.Height * scale);

    if(pixelWidth <= 0 || pixelHeight <= 0)
    {
      return new ScreenshotResult
      {
        Success = false,
        Error = $"Visual has zero or negative size ({bounds.Width}x{bounds.Height}). Cannot capture screenshot."
      };
    }

    PixelSize pixelSize = new(pixelWidth, pixelHeight);
    Vector dpi = new(96 * scale, 96 * scale);

    using RenderTargetBitmap renderTarget = new(pixelSize, dpi);
    renderTarget.Render(visual);

    string? directory = Path.GetDirectoryName(outputPath);
    if(!string.IsNullOrEmpty(directory))
    {
      Directory.CreateDirectory(directory);
    }

    using(FileStream fs = new(outputPath, FileMode.Create, FileAccess.Write))
    {
      renderTarget.Save(fs);
    }

    return new ScreenshotResult
    {
      Success = true,
      FilePath = outputPath,
      Width = pixelWidth,
      Height = pixelHeight
    };
  }

  /// <inheritdoc />
  public async Task<ScreenshotResult> TakeScreenshotAsync(
    string outputPath,
    string? elementName = null,
    double scaleFactor = 1.0,
    int? windowIndex = null,
    bool composite = true,
    CancellationToken ct = default)
  {
    double clampedScale = Math.Clamp(scaleFactor, 0.5, 3.0);

    return await Dispatcher.UIThread.InvokeAsync(() =>
    {
      if(composite && elementName is null && windowIndex is null)
      {
        return RenderCompositeToFile(outputPath, clampedScale);
      }

      Visual target;
      if(elementName is not null)
      {
        (TopLevel _, Visual element) = ResolveElementAcrossWindows(elementName, null, null, windowIndex);
        target = element;
      }
      else
      {
        target = ResolveTopLevelOrThrow(windowIndex);
      }

      return RenderToFile(target, outputPath, clampedScale);
    });
  }

  /// <summary>
  /// Renders all open windows composited into a single image, positioned according to their screen coordinates.
  /// Windows are drawn back-to-front so dialogs and child windows appear on top of the main window.
  /// </summary>
  ScreenshotResult RenderCompositeToFile(string outputPath, double scale)
  {
    IReadOnlyList<TopLevel> allTopLevels = GetAllTopLevels();

    // Collect open popup roots (context menus, flyouts, combo dropdowns, etc.)
    // that are hosted as native windows (PopupRoot) and not included in desktop.Windows.
    List<TopLevel> popupRoots = new();
    HashSet<TopLevel> seenPopups = new(ReferenceEqualityComparer.Instance);
    foreach(TopLevel tl in allTopLevels)
    {
      CollectOpenPopupRoots(tl, popupRoots, seenPopups);
    }

    // If only one window and no open popups, use simple single-window render
    if(allTopLevels.Count <= 1 && popupRoots.Count == 0)
    {
      TopLevel single = allTopLevels.Count == 1
        ? allTopLevels[0]
        : GetTopLevel() ?? throw new InvalidOperationException("No TopLevel available.");
      return RenderToFile(single, outputPath, scale);
    }

    // Main window is last in topmost-first order — use it as position reference
    TopLevel mainWindow = allTopLevels[^1];
    double renderScaling = mainWindow.RenderScaling;
    PixelPoint mainPos = GetTopLevelScreenPosition(mainWindow);

    // Render each window and collect position data (back-to-front = reverse of topmost-first)
    var layers = new List<(RenderTargetBitmap Bitmap, double OffsetX, double OffsetY, double Width, double Height)>();
    bool allSamePosition = true;
    for(int i = allTopLevels.Count - 1; i >= 0; i--)
    {
      TopLevel tl = allTopLevels[i];
      double w = tl.Bounds.Width;
      double h = tl.Bounds.Height;
      if(w <= 0 || h <= 0)
      {
        continue;
      }

      int pixelW = (int)(w * scale);
      int pixelH = (int)(h * scale);
      RenderTargetBitmap rtb = new(new PixelSize(pixelW, pixelH), new Vector(96 * scale, 96 * scale));
      rtb.Render(tl);

      // Convert screen position to logical offset relative to main window
      PixelPoint pos = GetTopLevelScreenPosition(tl);
      double offsetX = (pos.X - mainPos.X) / renderScaling;
      double offsetY = (pos.Y - mainPos.Y) / renderScaling;

      if(i < allTopLevels.Count - 1 && (pos.X != mainPos.X || pos.Y != mainPos.Y))
      {
        allSamePosition = false;
      }

      layers.Add((rtb, offsetX, offsetY, w, h));
    }

    // Render open popup roots (context menus, flyouts, etc.) as topmost layers
    foreach(TopLevel popupTl in popupRoots)
    {
      double pw = popupTl.Bounds.Width;
      double ph = popupTl.Bounds.Height;
      if(pw <= 0 || ph <= 0)
      {
        continue;
      }

      int ppxW = (int)(pw * scale);
      int ppxH = (int)(ph * scale);
      RenderTargetBitmap prtb = new(new PixelSize(ppxW, ppxH), new Vector(96 * scale, 96 * scale));
      prtb.Render(popupTl);

      PixelPoint ppos = GetTopLevelScreenPosition(popupTl);
      double pOffsetX = (ppos.X - mainPos.X) / renderScaling;
      double pOffsetY = (ppos.Y - mainPos.Y) / renderScaling;

      if(ppos.X != mainPos.X || ppos.Y != mainPos.Y)
      {
        allSamePosition = false;
      }

      layers.Add((prtb, pOffsetX, pOffsetY, pw, ph));
    }

    if(layers.Count == 0)
    {
      return new ScreenshotResult { Success = false, Error = "No windows have visible content." };
    }

    // If all windows report the same position (e.g. headless or unsupported platform),
    // center child windows on the main window for a useful composite.
    if(allSamePosition && layers.Count > 1)
    {
      (_, _, _, double mainW, double mainH) = layers[0];
      for(int i = 1; i < layers.Count; i++)
      {
        var (bmp, _, _, cw, ch) = layers[i];
        layers[i] = (bmp, (mainW - cw) / 2, (mainH - ch) / 2, cw, ch);
      }
    }

    // Calculate bounding box in logical units
    double minX = layers.Min(l => l.OffsetX);
    double minY = layers.Min(l => l.OffsetY);
    double maxX = layers.Max(l => l.OffsetX + l.Width);
    double maxY = layers.Max(l => l.OffsetY + l.Height);
    double totalW = maxX - minX;
    double totalH = maxY - minY;
    int compositePixelW = (int)(totalW * scale);
    int compositePixelH = (int)(totalH * scale);

    if(compositePixelW <= 0 || compositePixelH <= 0)
    {
      foreach(var (rtb, _, _, _, _) in layers)
      {
        rtb.Dispose();
      }

      return new ScreenshotResult { Success = false, Error = "Composite dimensions are zero." };
    }

    // Build a lightweight visual that draws all layers back-to-front
    var drawLayers = layers
      .Select(l => ((IImage)l.Bitmap, new Rect(l.OffsetX - minX, l.OffsetY - minY, l.Width, l.Height)))
      .ToList();
    CompositeVisual visual = new(drawLayers, new Size(totalW, totalH));
    visual.Measure(new Size(totalW, totalH));
    visual.Arrange(new Rect(0, 0, totalW, totalH));

    using RenderTargetBitmap composite = new(
      new PixelSize(compositePixelW, compositePixelH),
      new Vector(96 * scale, 96 * scale));
    composite.Render(visual);

    string? directory = Path.GetDirectoryName(outputPath);
    if(!string.IsNullOrEmpty(directory))
    {
      Directory.CreateDirectory(directory);
    }

    using(FileStream fs = new(outputPath, FileMode.Create, FileAccess.Write))
    {
      composite.Save(fs);
    }

    foreach(var (rtb, _, _, _, _) in layers)
    {
      rtb.Dispose();
    }

    return new ScreenshotResult
    {
      Success = true,
      FilePath = outputPath,
      Width = compositePixelW,
      Height = compositePixelH
    };
  }

  /// <summary>
  /// Lightweight control that draws pre-rendered window bitmaps in layer order.
  /// Used for composite screenshots where multiple windows are composited into a single image.
  /// </summary>
  private sealed class CompositeVisual(IReadOnlyList<(IImage Image, Rect Bounds)> layers, Size size) : Control
  {
    public override void Render(DrawingContext context)
    {
      foreach(var (image, bounds) in layers)
      {
        context.DrawImage(image, bounds);
      }
    }

    protected override Size MeasureOverride(Size availableSize) => size;

    protected override Size ArrangeOverride(Size finalSize) => size;
  }

  /// <summary>
  /// Collects all open PopupRoot instances (native window popups) from the visual tree.
  /// These include context menus, flyouts, combo dropdowns, date pickers, and other popup content
  /// that is hosted as a separate native window and not included in <c>desktop.Windows</c>.
  /// OverlayPopupHost-based popups are already captured when rendering the parent TopLevel.
  /// </summary>
  private static void CollectOpenPopupRoots(Visual current, List<TopLevel> results, HashSet<TopLevel> seen)
  {
    // Template-based Popup controls in the visual tree (ComboBox dropdown, DatePicker, etc.)
    if(current is Popup { IsOpen: true, Host: TopLevel popupHost } && popupHost is not Window)
    {
      if(seen.Add(popupHost))
      {
        results.Add(popupHost);
        // Recurse into popup's visual tree for nested popups (e.g., submenu cascades)
        CollectOpenPopupRoots(popupHost, results, seen);
      }
    }

    // Context menus (not in visual tree — accessed via Control.ContextMenu property)
    if(current is Control { ContextMenu: { IsOpen: true } contextMenu }
       && contextMenu.GetVisualRoot() is TopLevel menuRoot
       && menuRoot is not Window
       && seen.Add(menuRoot))
    {
      results.Add(menuRoot);
      CollectOpenPopupRoots(menuRoot, results, seen);
    }

    // Programmatic popups (flyouts, tooltips) are logical children of their target control,
    // not visual children. Check logical children for open Popup instances.
    if(current is StyledElement styled)
    {
      foreach(object? logicalChild in ((ILogical)styled).LogicalChildren)
      {
        if(logicalChild is Popup { IsOpen: true, Host: TopLevel logicalHost }
           && logicalHost is not Window
           && seen.Add(logicalHost))
        {
          results.Add(logicalHost);
          CollectOpenPopupRoots(logicalHost, results, seen);
        }
      }
    }

    foreach(Visual child in current.GetVisualChildren())
    {
      CollectOpenPopupRoots(child, results, seen);
    }
  }

  /// <summary>
  /// Gets the screen position of a TopLevel. Uses <see cref="Window.Position"/> for windows,
  /// falls back to <c>PointToScreen</c> for popup roots and other TopLevel types.
  /// Returns (0,0) if position cannot be determined (e.g., headless platform).
  /// </summary>
  private static PixelPoint GetTopLevelScreenPosition(TopLevel topLevel)
  {
    if(topLevel is Window window)
    {
      return window.Position;
    }

    try
    {
      return topLevel.PointToScreen(new Point(0, 0));
    }
    catch
    {
      return default;
    }
  }

  /// <summary>
  /// Maps a button name string to <see cref="MouseButton"/> and the corresponding
  /// <see cref="PointerUpdateKind"/> for pressed and released events.
  /// </summary>
  internal static (MouseButton Button, PointerUpdateKind Pressed, PointerUpdateKind Released)
    MapMouseButton(string button)
  {
    return button.ToLowerInvariant() switch
    {
      "left" => (MouseButton.Left, PointerUpdateKind.LeftButtonPressed, PointerUpdateKind.LeftButtonReleased),
      "right" => (MouseButton.Right, PointerUpdateKind.RightButtonPressed, PointerUpdateKind.RightButtonReleased),
      "middle" => (MouseButton.Middle, PointerUpdateKind.MiddleButtonPressed, PointerUpdateKind.MiddleButtonReleased),
      _ => throw new ArgumentException($"Unknown mouse button: '{button}'. Use 'left', 'right', or 'middle'.")
    };
  }

  /// <summary>
  /// Applies control-specific side effects after a synthetic click, because
  /// <c>RaiseEvent</c> does not update <c>IsPointerOver</c> and many controls
  /// guard their click handling behind that flag.
  /// Only fires on left-click; right-click is handled separately via ContextRequestedEvent.
  /// </summary>
  internal static void ApplyControlSpecificClickBehavior(Visual visual, MouseButton mouseButton)
  {
    if(mouseButton != MouseButton.Left)
    {
      return;
    }

    // Order matters: more-derived types must appear before their base types.
    // RadioButton → ToggleButton → Button, ComboBoxItem → ListBoxItem
    switch(visual)
    {
      case MenuItem menuItem:
        // Capture command before raising ClickEvent — Avalonia's built-in handler
        // closes the context menu synchronously, which disconnects bindings and
        // nullifies menuItem.Command before we can invoke it.
        System.Windows.Input.ICommand? miCommand = menuItem.Command;
        object? miCommandParameter = menuItem.CommandParameter;
        menuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        if(miCommand is not null && miCommand.CanExecute(miCommandParameter))
        {
          miCommand.Execute(miCommandParameter);
        }

        break;

      case RadioButton radio:
        radio.IsChecked = true;
        break;

      // CheckBox inherits from ToggleButton — matched here
      case ToggleButton toggle:
        toggle.IsChecked = !(toggle.IsChecked ?? false);
        break;

      // Button is the base of ToggleButton/RadioButton — must come after them
      case Button btn:
        btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(btn.Command is { } btnCommand && btnCommand.CanExecute(btn.CommandParameter))
        {
          btnCommand.Execute(btn.CommandParameter);
        }

        // OnClick() opens flyouts but isn't called by synthetic ClickEvent dispatch —
        // open the flyout explicitly so AI agents can interact with flyout content.
        btn.Flyout?.ShowAt(btn);
        break;

      case Expander expander:
        expander.IsExpanded = !expander.IsExpanded;
        break;

      case ComboBoxItem comboBoxItem:
        comboBoxItem.IsSelected = true;
        break;

      case ListBoxItem listBoxItem:
        listBoxItem.IsSelected = true;
        break;

      case TreeViewItem treeViewItem:
        treeViewItem.IsSelected = true;
        break;

      case TabItem tabItem:
        tabItem.IsSelected = true;
        break;
    }
  }

  /// <summary>
  /// Parses a modifier string like "ctrl+shift" into <see cref="KeyModifiers"/> and
  /// corresponding <see cref="RawInputModifiers"/> flags.
  /// </summary>
  internal static (KeyModifiers Key, RawInputModifiers Raw) ParseModifiers(string? modifiers)
  {
    if(string.IsNullOrWhiteSpace(modifiers))
    {
      return (KeyModifiers.None, RawInputModifiers.None);
    }

    KeyModifiers keyMods = KeyModifiers.None;
    RawInputModifiers rawMods = RawInputModifiers.None;

    foreach(string part in modifiers.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
      switch(part.ToLowerInvariant())
      {
        case "ctrl" or "control":
          keyMods |= KeyModifiers.Control;
          rawMods |= RawInputModifiers.Control;
          break;
        case "shift":
          keyMods |= KeyModifiers.Shift;
          rawMods |= RawInputModifiers.Shift;
          break;
        case "alt":
          keyMods |= KeyModifiers.Alt;
          rawMods |= RawInputModifiers.Alt;
          break;
        case "meta" or "win" or "cmd":
          keyMods |= KeyModifiers.Meta;
          rawMods |= RawInputModifiers.Meta;
          break;
        default:
          throw new ArgumentException($"Unknown modifier: '{part}'. Use 'ctrl', 'shift', 'alt', or 'meta'.");
      }
    }

    return (keyMods, rawMods);
  }

  /// <summary>
  /// Parses a key combination string like "Ctrl+S", "Ctrl+Shift+Z", or "Alt+F4"
  /// into <see cref="KeyModifiers"/> and <see cref="Key"/> components.
  /// The last non-modifier token is treated as the primary key.
  /// </summary>
  internal static (KeyModifiers Modifiers, Key PrimaryKey) ParseKeyCombination(string keys)
  {
    if(string.IsNullOrWhiteSpace(keys))
    {
      throw new ArgumentException("Key combination cannot be empty.", nameof(keys));
    }

    string[] parts = keys.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if(parts.Length == 0)
    {
      throw new ArgumentException("Key combination cannot be empty.", nameof(keys));
    }

    KeyModifiers modifiers = KeyModifiers.None;
    string? primaryKeyStr = null;

    foreach(string part in parts)
    {
      switch(part.ToLowerInvariant())
      {
        case "ctrl" or "control":
          modifiers |= KeyModifiers.Control;
          break;
        case "shift":
          modifiers |= KeyModifiers.Shift;
          break;
        case "alt":
          modifiers |= KeyModifiers.Alt;
          break;
        case "meta" or "win" or "cmd":
          modifiers |= KeyModifiers.Meta;
          break;
        default:
          primaryKeyStr = part;
          break;
      }
    }

    if(primaryKeyStr is null)
    {
      throw new ArgumentException($"No primary key found in key combination: '{keys}'.", nameof(keys));
    }

    if(!Enum.TryParse<Key>(primaryKeyStr, true, out Key primaryKey))
    {
      throw new ArgumentException($"Unknown key: '{primaryKeyStr}'. Must be a valid Avalonia.Input.Key enum value.",
        nameof(keys));
    }

    return (modifiers, primaryKey);
  }

  /// <summary>
  /// Builds an <see cref="InteractionResult"/> capturing the current state of a visual element.
  /// </summary>
  private static InteractionResult BuildInteractionResult(Visual visual, bool success, string? errorMessage = null)
  {
    Control? control = visual as Control;
    InputElement? inputElement = visual as InputElement;

    return new InteractionResult
    {
      Success = success,
      ErrorMessage = errorMessage,
      ElementType = visual.GetType().Name,
      ElementName = control?.Name,
      IsFocused = inputElement?.IsFocused,
      IsEnabled = control?.IsEffectivelyEnabled,
      IsVisible = control?.IsVisible,
      IsPointerOver = inputElement?.IsPointerOver
    };
  }

  /// <inheritdoc />
  public async Task<InteractionResult> SimulateClickAsync(
    string elementName,
    string button = "left",
    double? x = null,
    double? y = null,
    string? modifiers = null,
    int clickCount = 1,
    int? windowIndex = null,
    CancellationToken ct = default)
  {
    return await Dispatcher.UIThread.InvokeAsync(() =>
    {
      (TopLevel topLevel, Visual visual) = ResolveElementAcrossWindows(elementName, null, null, windowIndex);

      if(visual is not Interactive element)
      {
        throw new InvalidOperationException(
          $"Element '{elementName}' ({visual.GetType().Name}) is not an Interactive element and cannot receive pointer events.");
      }

      (MouseButton mouseButton, PointerUpdateKind pressedKind, PointerUpdateKind releasedKind) = MapMouseButton(button);
      (KeyModifiers keyModifiers, RawInputModifiers rawModifiers) = ParseModifiers(modifiers);

      // Calculate position: use center of element bounds if x/y not specified
      Rect bounds = visual.Bounds;
      double posX = x ?? bounds.Width / 2;
      double posY = y ?? bounds.Height / 2;
      Point position = new(posX, posY);

      // Add mouse button to raw modifiers for press event
      RawInputModifiers pressRawModifiers = rawModifiers | mouseButton switch
      {
        MouseButton.Left => RawInputModifiers.LeftMouseButton,
        MouseButton.Right => RawInputModifiers.RightMouseButton,
        MouseButton.Middle => RawInputModifiers.MiddleMouseButton,
        _ => RawInputModifiers.None
      };

      ulong timestamp = (ulong)Environment.TickCount64;
      Pointer pointer = MockPointer.Create();

      // Pre-click hover to best-effort set visual state
      PointerPointProperties hoverProperties = new(RawInputModifiers.None, PointerUpdateKind.Other);
#pragma warning disable CS0618 // Obsolete pointer event ctors — no alternative for runtime inspection (IHeadlessWindow is test-only)
      PointerEventArgs enterArgs = new(InputElement.PointerEnteredEvent, element, pointer, topLevel, position, timestamp, hoverProperties, KeyModifiers.None);
      element.RaiseEvent(enterArgs);
      PointerEventArgs moveArgs = new(InputElement.PointerMovedEvent, element, pointer, topLevel, position, timestamp + 1, hoverProperties, KeyModifiers.None);
      element.RaiseEvent(moveArgs);

      PointerPointProperties pressProperties = new(pressRawModifiers, pressedKind);
      PointerPressedEventArgs pressArgs = new(
        element,
        pointer,
        topLevel,
        position,
        timestamp + 2,
        pressProperties,
        keyModifiers,
        clickCount);
      element.RaiseEvent(pressArgs);

      PointerPointProperties releaseProperties = new(rawModifiers, releasedKind);
      PointerReleasedEventArgs releaseArgs = new(
        element,
        pointer,
        topLevel,
        position,
        timestamp + 3,
        releaseProperties,
        keyModifiers,
        mouseButton);
#pragma warning restore CS0618
      element.RaiseEvent(releaseArgs);

      // Right-click: dispatch ContextRequestedEvent so context menus open
      if(mouseButton == MouseButton.Right)
      {
        element.RaiseEvent(new ContextRequestedEventArgs());
      }

      // Workaround: synthetic pointer events don't update IsPointerOver, so many
      // controls silently ignore the click. Apply control-specific side effects.
      ApplyControlSpecificClickBehavior(visual, mouseButton);

      return BuildInteractionResult(visual, true);
    });
  }

  /// <inheritdoc />
  public async Task<InteractionResult> SimulateTypeAsync(string elementName, string text,
    int? windowIndex = null, CancellationToken ct = default)
  {
    return await Dispatcher.UIThread.InvokeAsync(() =>
    {
      (TopLevel _, Visual visual) = ResolveElementAcrossWindows(elementName, null, null, windowIndex);

      if(visual is not Interactive element)
      {
        throw new InvalidOperationException(
          $"Element '{elementName}' ({visual.GetType().Name}) is not an Interactive element and cannot receive input events.");
      }

      // Focus the element first
      (element as InputElement)?.Focus();

      foreach(char ch in text)
      {
        switch(ch)
        {
          case '\b':
            RaiseKeyEvent(element, Key.Back);
            break;
          case '\n' or '\r':
            RaiseKeyEvent(element, Key.Enter);
            break;
          case '\t':
            RaiseKeyEvent(element, Key.Tab);
            break;
          default:
            element.RaiseEvent(new TextInputEventArgs
            {
              RoutedEvent = InputElement.TextInputEvent, Source = element, Text = ch.ToString()
            });
            break;
        }
      }

      return BuildInteractionResult(visual, true);
    });
  }

  /// <summary>
  /// Raises KeyDown followed by KeyUp for the specified key on the target element.
  /// </summary>
  private static void RaiseKeyEvent(Interactive target, Key key, KeyModifiers modifiers = KeyModifiers.None)
  {
    target.RaiseEvent(new KeyEventArgs
    {
      RoutedEvent = InputElement.KeyDownEvent, Source = target, Key = key, KeyModifiers = modifiers
    });

    target.RaiseEvent(new KeyEventArgs
    {
      RoutedEvent = InputElement.KeyUpEvent, Source = target, Key = key, KeyModifiers = modifiers
    });
  }

  /// <inheritdoc />
  public async Task<InteractionResult> SetControlStateAsync(string elementName, string stateType, bool value,
    int? windowIndex = null, CancellationToken ct = default)
  {
    return await Dispatcher.UIThread.InvokeAsync(() =>
    {
      (TopLevel topLevel, Visual visual) = ResolveElementAcrossWindows(elementName, null, null, windowIndex);

      switch(stateType.ToLowerInvariant())
      {
        case "focus":
          if(visual is InputElement inputElement)
          {
            if(value)
            {
              inputElement.Focus(NavigationMethod.Unspecified, KeyModifiers.None);
            }
            else
            {
              // Blur by focusing the top-level instead
              (topLevel as InputElement)?.Focus(NavigationMethod.Unspecified, KeyModifiers.None);
            }
          }
          else
          {
            throw new InvalidOperationException(
              $"Element '{elementName}' ({visual.GetType().Name}) is not an InputElement and cannot be focused.");
          }

          break;

        case "enabled":
          if(visual is Control enableControl)
          {
            enableControl.IsEnabled = value;
          }
          else
          {
            throw new InvalidOperationException(
              $"Element '{elementName}' ({visual.GetType().Name}) is not a Control and does not have IsEnabled.");
          }

          break;

        case "visible":
          if(visual is Control visControl)
          {
            visControl.IsVisible = value;
          }
          else
          {
            throw new InvalidOperationException(
              $"Element '{elementName}' ({visual.GetType().Name}) is not a Control and does not have IsVisible.");
          }

          break;

        default:
          throw new ArgumentException(
            $"Unknown state type: '{stateType}'. Use 'focus', 'enabled', or 'visible'.");
      }

      return BuildInteractionResult(visual, true);
    });
  }

  /// <inheritdoc />
  public async Task<InteractionResult> SimulateHoverAsync(string elementName, bool exitAfter = false,
    int? windowIndex = null, CancellationToken ct = default)
  {
    return await Dispatcher.UIThread.InvokeAsync(() =>
    {
      (TopLevel topLevel, Visual visual) = ResolveElementAcrossWindows(elementName, null, null, windowIndex);

      if(visual is not Interactive element)
      {
        throw new InvalidOperationException(
          $"Element '{elementName}' ({visual.GetType().Name}) is not an Interactive element and cannot receive pointer events.");
      }

      Rect bounds = visual.Bounds;
      Point center = new(bounds.Width / 2, bounds.Height / 2);
      ulong timestamp = (ulong)Environment.TickCount64;
      Pointer pointer = MockPointer.Create();
      PointerPointProperties properties = new(RawInputModifiers.None, PointerUpdateKind.Other);

      // Raise PointerEntered
#pragma warning disable CS0618 // Obsolete pointer event ctors — no alternative for runtime inspection (IHeadlessWindow is test-only)
      PointerEventArgs enterArgs = new(
        InputElement.PointerEnteredEvent,
        element,
        pointer,
        topLevel,
        center,
        timestamp,
        properties,
        KeyModifiers.None);
      element.RaiseEvent(enterArgs);

      // Raise PointerMoved
      PointerEventArgs moveArgs = new(
        InputElement.PointerMovedEvent,
        element,
        pointer,
        topLevel,
        center,
        timestamp + 1,
        properties,
        KeyModifiers.None);
      element.RaiseEvent(moveArgs);

      // Optionally raise PointerExited
      if(exitAfter)
      {
        PointerEventArgs exitArgs = new(
          InputElement.PointerExitedEvent,
          element,
          pointer,
          topLevel,
          center,
          timestamp + 2,
          properties,
          KeyModifiers.None);
        element.RaiseEvent(exitArgs);
      }
#pragma warning restore CS0618

      return BuildInteractionResult(visual, true);
    });
  }

  /// <inheritdoc />
  public async Task<InteractionResult> SimulateDragAsync(
    string elementName,
    double startX,
    double startY,
    double endX,
    double endY,
    string button = "left",
    int steps = 10,
    int? windowIndex = null,
    CancellationToken ct = default)
  {
    return await Dispatcher.UIThread.InvokeAsync(() =>
    {
      (TopLevel topLevel, Visual visual) = ResolveElementAcrossWindows(elementName, null, null, windowIndex);

      if(visual is not Interactive element)
      {
        throw new InvalidOperationException(
          $"Element '{elementName}' ({visual.GetType().Name}) is not an Interactive element and cannot receive pointer events.");
      }

      (MouseButton mouseButton, PointerUpdateKind pressedKind, PointerUpdateKind releasedKind) = MapMouseButton(button);
      Pointer pointer = MockPointer.Create();
      ulong timestamp = (ulong)Environment.TickCount64;

      RawInputModifiers buttonRawMod = mouseButton switch
      {
        MouseButton.Left => RawInputModifiers.LeftMouseButton,
        MouseButton.Right => RawInputModifiers.RightMouseButton,
        MouseButton.Middle => RawInputModifiers.MiddleMouseButton,
        _ => RawInputModifiers.None
      };

      // Press at start position
      Point startPos = new(startX, startY);
      PointerPointProperties pressProperties = new(buttonRawMod, pressedKind);
#pragma warning disable CS0618 // Obsolete pointer event ctors — no alternative for runtime inspection (IHeadlessWindow is test-only)
      PointerPressedEventArgs pressArgs = new(
        element,
        pointer,
        topLevel,
        startPos,
        timestamp,
        pressProperties,
        KeyModifiers.None);
      element.RaiseEvent(pressArgs);

      // Interpolate intermediate move events
      int clampedSteps = Math.Max(1, steps);
      for(int i = 1; i <= clampedSteps; i++)
      {
        double t = (double)i / clampedSteps;
        double moveX = startX + ((endX - startX) * t);
        double moveY = startY + ((endY - startY) * t);
        Point movePos = new(moveX, moveY);

        PointerPointProperties moveProperties = new(buttonRawMod, PointerUpdateKind.Other);
        PointerEventArgs moveArgs = new(
          InputElement.PointerMovedEvent,
          element,
          pointer,
          topLevel,
          movePos,
          timestamp + (ulong)i,
          moveProperties,
          KeyModifiers.None);
        element.RaiseEvent(moveArgs);
      }

      // Release at end position
      Point endPos = new(endX, endY);
      PointerPointProperties releaseProperties = new(RawInputModifiers.None, releasedKind);
      PointerReleasedEventArgs releaseArgs = new(
        element,
        pointer,
        topLevel,
        endPos,
        timestamp + (ulong)(clampedSteps + 1),
        releaseProperties,
        KeyModifiers.None,
        mouseButton);
#pragma warning restore CS0618
      element.RaiseEvent(releaseArgs);

      return BuildInteractionResult(visual, true);
    });
  }

  /// <inheritdoc />
  public async Task<InteractionResult> SimulateKeyboardShortcutAsync(string keys, string? elementName = null,
    int? windowIndex = null, CancellationToken ct = default)
  {
    return await Dispatcher.UIThread.InvokeAsync(() =>
    {
      TopLevel topLevel = ResolveTopLevelOrThrow(windowIndex);

      Interactive target;

      if(elementName is not null)
      {
        Visual visual = ResolveElement(topLevel, elementName, null, null)
                        ?? throw new InvalidOperationException($"Element '{elementName}' not found.");

        if(visual is not Interactive interactiveElement)
        {
          throw new InvalidOperationException(
            $"Element '{elementName}' ({visual.GetType().Name}) is not an Interactive element and cannot receive keyboard events.");
        }

        // Focus the element
        (interactiveElement as InputElement)?.Focus(NavigationMethod.Unspecified, KeyModifiers.None);
        target = interactiveElement;
      }
      else
      {
        // FocusManager may be shared across windows, so verify the focused element
        // actually belongs to this window's visual tree. Without this check, keyboard
        // events targeting a dialog could be misdirected to the main window's focused
        // element, preventing handlers like OnKeyDown from firing on the dialog.
        Interactive? focused = topLevel.FocusManager?.GetFocusedElement() as Interactive;
        if(focused is Visual focusedVisual && focusedVisual.GetVisualRoot() != topLevel)
        {
          focused = null;
        }

        // Fall back to a visual child so the event bubbles up through the tree,
        // reaching class handlers like OnKeyDown on the window.
        target = focused
                 ?? topLevel.GetVisualChildren().OfType<Interactive>().FirstOrDefault()
                 ?? topLevel;
      }

      (KeyModifiers modifiers, Key primaryKey) = ParseKeyCombination(keys);

      // Raise KeyDown for the primary key with modifiers
      target.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyDownEvent, Source = target, Key = primaryKey, KeyModifiers = modifiers
      });

      // Raise KeyUp for the primary key
      target.RaiseEvent(new KeyEventArgs
      {
        RoutedEvent = InputElement.KeyUpEvent, Source = target, Key = primaryKey, KeyModifiers = modifiers
      });

      // Alt+F4: close the window (platform shortcut, not handled by Avalonia input)
      if(primaryKey == Key.F4 && modifiers == KeyModifiers.Alt && topLevel is Window altF4Window)
      {
        altF4Window.Close();
      }

      Visual visual2 = target as Visual ?? topLevel as Visual;
      return BuildInteractionResult(visual2, true);
    });
  }

  /// <inheritdoc />
  public async Task<InteractionResult> SimulateScrollAsync(
    string elementName,
    double deltaX = 0,
    double deltaY = -120,
    string? modifiers = null,
    int? windowIndex = null,
    CancellationToken ct = default)
  {
    return await Dispatcher.UIThread.InvokeAsync(() =>
    {
      (TopLevel topLevel, Visual visual) = ResolveElementAcrossWindows(elementName, null, null, windowIndex);

      if(visual is not Interactive element)
      {
        throw new InvalidOperationException(
          $"Element '{elementName}' ({visual.GetType().Name}) is not an Interactive element and cannot receive pointer events.");
      }

      (KeyModifiers keyModifiers, RawInputModifiers rawModifiers) = ParseModifiers(modifiers);

      Rect bounds = visual.Bounds;
      Point center = new(bounds.Width / 2, bounds.Height / 2);
      ulong timestamp = (ulong)Environment.TickCount64;
      Pointer pointer = MockPointer.Create();

      Vector delta = new(deltaX, deltaY);
      PointerPointProperties properties = new(rawModifiers, PointerUpdateKind.Other);

#pragma warning disable CS0618 // Obsolete pointer event ctors
      PointerWheelEventArgs wheelArgs = new(
        element,
        pointer,
        topLevel,
        center,
        timestamp,
        properties,
        keyModifiers,
        delta);
#pragma warning restore CS0618

      element.RaiseEvent(wheelArgs);

      return BuildInteractionResult(visual, true);
    });
  }
}
