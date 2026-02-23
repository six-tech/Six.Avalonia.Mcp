using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging;
using Six.Avalonia.Mcp.Contracts.Models;
using Six.Avalonia.Mcp.Connector.Services;

namespace Six.Avalonia.Mcp.Tests.Unit.Services;

public class AvaloniaInspectorTests
{
  private static AvaloniaInspector CreateInspector()
  {
    ILogger<AvaloniaInspector>? logger = Substitute.For<ILogger<AvaloniaInspector>>();
    return new AvaloniaInspector(logger);
  }

  /// <summary>
  /// Builds a test visual tree:
  ///   StackPanel (root)
  ///   ├── [0] Border (Name = "header")
  ///   │   └── [0] TextBlock (Name = "title")
  ///   ├── [1] StackPanel (Name = "content")
  ///   │   ├── [0] TextBox (Name = "input")
  ///   │   └── [1] Button (Name = "submit")
  ///   └── [2] TextBlock (Name = "footer")
  /// </summary>
  private static StackPanel BuildTestTree()
  {
    TextBlock title = new() { Name = "title" };
    Border header = new() { Name = "header", Child = title };
    TextBox input = new() { Name = "input" };
    Button submit = new() { Name = "submit" };
    StackPanel content = new() { Name = "content" };
    content.Children.Add(input);
    content.Children.Add(submit);
    TextBlock footer = new() { Name = "footer" };

    StackPanel root = new();
    root.Children.Add(header);
    root.Children.Add(content);
    root.Children.Add(footer);

    return root;
  }

  public class ResolveElementTests : AvaloniaInspectorTests
  {
    [AvaloniaFact]
    public void ResolveElement_ByName_FindsDirectChild()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      Visual? result = AvaloniaInspector.ResolveElement(root, "header", null, null);

      result.Should().NotBeNull();
      result!.Name.Should().Be("header");
    }

    [AvaloniaFact]
    public void ResolveElement_ByName_FindsNestedChild()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      Visual? result = AvaloniaInspector.ResolveElement(root, "title", null, null);

      result.Should().NotBeNull();
      result!.Name.Should().Be("title");
    }

    [AvaloniaFact]
    public void ResolveElement_ByName_ReturnsNullWhenNotFound()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      Visual? result = AvaloniaInspector.ResolveElement(root, "nonexistent", null, null);

      result.Should().BeNull();
    }

    [AvaloniaFact]
    public void ResolveElement_ByTypeName_FindsFirstMatch()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      Visual? result = AvaloniaInspector.ResolveElement(root, null, "Button", null);

      result.Should().NotBeNull();
      result!.Name.Should().Be("submit");
    }

    [AvaloniaFact]
    public void ResolveElement_ByTypeName_ReturnsNullWhenNotFound()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      Visual? result = AvaloniaInspector.ResolveElement(root, null, "Slider", null);

      result.Should().BeNull();
    }

    [AvaloniaFact]
    public void ResolveElement_ByPath_NavigatesByChildIndex()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      // Path "1/1" → root's child[1] (content StackPanel) → child[1] (submit Button)
      Visual? result = AvaloniaInspector.ResolveElement(root, null, null, "1/1");

      result.Should().NotBeNull();
      result!.Name.Should().Be("submit");
    }

    [AvaloniaFact]
    public void ResolveElement_ByPath_NavigatesToDirectChild()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      // Path "0" → root's child[0] (header Border)
      Visual? result = AvaloniaInspector.ResolveElement(root, null, null, "0");

      result.Should().NotBeNull();
      result!.Name.Should().Be("header");
    }

    [AvaloniaFact]
    public void ResolveElement_ByPath_ReturnsNullForInvalidIndex()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      // Path "5" → out of bounds
      Visual? result = AvaloniaInspector.ResolveElement(root, null, null, "5");

      result.Should().BeNull();
    }

    [AvaloniaFact]
    public void ResolveElement_ByPath_ReturnsNullForInvalidNestedIndex()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      // Path "0/5" → header has only 1 child
      Visual? result = AvaloniaInspector.ResolveElement(root, null, null, "0/5");

      result.Should().BeNull();
    }

    [AvaloniaFact]
    public void ResolveElement_AllNullParameters_ReturnsNull()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      Visual? result = AvaloniaInspector.ResolveElement(root, null, null, null);

      result.Should().BeNull();
    }

    [AvaloniaFact]
    public void ResolveElement_NameTakesPrecedence_OverTypeName()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      // Name "title" is a TextBlock, but if we also pass typeName "Button", name should take precedence
      Visual? result = AvaloniaInspector.ResolveElement(root, "title", "Button", null);

      result.Should().NotBeNull();
      result!.Name.Should().Be("title");
    }
  }

  public class BuildPathTests : AvaloniaInspectorTests
  {
    [AvaloniaFact]
    public void BuildPath_DirectChild_ReturnsSingleIndex()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();
      Visual header = root.GetVisualChildren().First();

      IReadOnlyList<int> path = AvaloniaInspector.BuildPath(root, header);

      path.Should().Equal(0);
    }

    [AvaloniaFact]
    public void BuildPath_SecondChild_ReturnsCorrectIndex()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();
      Visual content = root.GetVisualChildren().Skip(1).First();

      IReadOnlyList<int> path = AvaloniaInspector.BuildPath(root, content);

      path.Should().Equal(1);
    }

    [AvaloniaFact]
    public void BuildPath_NestedChild_ReturnsFullIndexPath()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();
      // Navigate to submit button: root → child[1] (content) → child[1] (submit)
      Visual? submit = AvaloniaInspector.ResolveElement(root, "submit", null, null);

      IReadOnlyList<int> path = AvaloniaInspector.BuildPath(root, submit!);

      path.Should().Equal(1, 1);
    }

    [AvaloniaFact]
    public void BuildPath_DeeplyNested_ReturnsFullPath()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();
      // Navigate to title: root → child[0] (header) → child[0] (title)
      Visual? title = AvaloniaInspector.ResolveElement(root, "title", null, null);

      IReadOnlyList<int> path = AvaloniaInspector.BuildPath(root, title!);

      path.Should().Equal(0, 0);
    }

    [AvaloniaFact]
    public void BuildPath_LastChild_ReturnsCorrectIndex()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();
      Visual? footer = AvaloniaInspector.ResolveElement(root, "footer", null, null);

      IReadOnlyList<int> path = AvaloniaInspector.BuildPath(root, footer!);

      path.Should().Equal(2);
    }

    [AvaloniaFact]
    public void BuildPath_RootIsTarget_ReturnsEmptyPath()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      IReadOnlyList<int> path = AvaloniaInspector.BuildPath(root, root);

      path.Should().BeEmpty();
    }

    [AvaloniaFact]
    public void BuildPath_TargetNotInTree_ReturnsEmptyPath()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();
      TextBlock orphan = new() { Name = "orphan" };

      IReadOnlyList<int> path = AvaloniaInspector.BuildPath(root, orphan);

      path.Should().BeEmpty();
    }
  }

  public class GenerateElementIdTests : AvaloniaInspectorTests
  {
    [AvaloniaFact]
    public void GenerateElementId_DirectChild_ReturnsPathFormat()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();
      Visual header = root.GetVisualChildren().First();

      string id = AvaloniaInspector.GenerateElementId(root, header);

      id.Should().Be("path:0");
    }

    [AvaloniaFact]
    public void GenerateElementId_NestedChild_ReturnsSlashSeparatedPath()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();
      Visual? submit = AvaloniaInspector.ResolveElement(root, "submit", null, null);

      string id = AvaloniaInspector.GenerateElementId(root, submit!);

      id.Should().Be("path:1/1");
    }

    [AvaloniaFact]
    public void GenerateElementId_DeeplyNested_ReturnsFullPath()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();
      Visual? title = AvaloniaInspector.ResolveElement(root, "title", null, null);

      string id = AvaloniaInspector.GenerateElementId(root, title!);

      id.Should().Be("path:0/0");
    }

    [AvaloniaFact]
    public void GenerateElementId_Root_ReturnsPathWithEmptyIndices()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      string id = AvaloniaInspector.GenerateElementId(root, root);

      id.Should().Be("path:");
    }
  }

  public class BuildTreeNodeTests : AvaloniaInspectorTests
  {
    [AvaloniaFact]
    public void BuildTreeNode_ReturnsCorrectTypeForRoot()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      VisualTreeNode node = AvaloniaInspector.BuildTreeNode(root, 0, 1);

      node.Type.Should().Be("StackPanel");
    }

    [AvaloniaFact]
    public void BuildTreeNode_ReturnsNameForNamedControl()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();
      Visual header = root.GetVisualChildren().First();

      VisualTreeNode node = AvaloniaInspector.BuildTreeNode(header, 0, 1);

      node.Name.Should().Be("header");
    }

    [AvaloniaFact]
    public void BuildTreeNode_ReturnsNullNameForUnnamedControl()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree(); // root StackPanel has no name

      VisualTreeNode node = AvaloniaInspector.BuildTreeNode(root, 0, 1);

      node.Name.Should().BeNull();
    }

    [AvaloniaFact]
    public void BuildTreeNode_ReturnsBoundsInfo()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      VisualTreeNode node = AvaloniaInspector.BuildTreeNode(root, 0, 1);

      node.Bounds.Should().NotBeNull();
    }

    [AvaloniaFact]
    public void BuildTreeNode_ReturnsVisibilityState()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      VisualTreeNode node = AvaloniaInspector.BuildTreeNode(root, 0, 1);

      node.IsVisible.Should().BeTrue();
      node.IsEnabled.Should().BeTrue();
    }

    [AvaloniaFact]
    public void BuildTreeNode_ReturnsOpacity()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      VisualTreeNode node = AvaloniaInspector.BuildTreeNode(root, 0, 1);

      node.Opacity.Should().Be(1.0);
    }

    [AvaloniaFact]
    public void BuildTreeNode_ReturnsChildrenAtDepth()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      VisualTreeNode node = AvaloniaInspector.BuildTreeNode(root, 0, 2);

      // At depth 2 from root (depth 0), children (depth 1) should be populated
      node.Children.Should().HaveCount(3);
      node.Children[0].Type.Should().Be("Border");
      node.Children[1].Type.Should().Be("StackPanel");
      node.Children[2].Type.Should().Be("TextBlock");
    }

    [AvaloniaFact]
    public void BuildTreeNode_StopsAtMaxDepth()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      // maxDepth = 1, currentDepth = 0 → root's children are included (depth 0 < 1)
      // but children's children are NOT (depth 1 >= 1)
      VisualTreeNode node = AvaloniaInspector.BuildTreeNode(root, 0, 1);

      node.Children.Should().HaveCount(3);
      node.Children[0].Children.Should().BeEmpty(); // Border child (TextBlock) NOT included
      node.Children[1].Children.Should().BeEmpty(); // StackPanel children NOT included
    }

    [AvaloniaFact]
    public void BuildTreeNode_RecursesFullDepth()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();

      // maxDepth = 10 → should recurse the full tree
      VisualTreeNode node = AvaloniaInspector.BuildTreeNode(root, 0, 10);

      // header → Border has TextBlock child
      node.Children[0].Children.Should().HaveCount(1);
      node.Children[0].Children[0].Type.Should().Be("TextBlock");
      node.Children[0].Children[0].Name.Should().Be("title");

      // content → StackPanel has TextBox and Button
      node.Children[1].Children.Should().HaveCount(2);
      node.Children[1].Children[0].Name.Should().Be("input");
      node.Children[1].Children[1].Name.Should().Be("submit");
    }

    [AvaloniaFact]
    public void BuildTreeNode_ReturnsClassesForStyledElement()
    {
      AvaloniaInspector inspector = CreateInspector();
      Button button = new() { Name = "test" };
      button.Classes.Add("primary");

      VisualTreeNode node = AvaloniaInspector.BuildTreeNode(button, 0, 1);

      node.Classes.Should().Contain("primary");
    }
  }

  public class FindElementAsyncTests : AvaloniaInspectorTests
  {
    [AvaloniaFact]
    public async Task FindElementAsync_ThrowsWhenNoMainWindow()
    {
      AvaloniaInspector inspector = CreateInspector();

      Func<Task<IReadOnlyList<FindElementResult>>> act = () => inspector.FindElementAsync("test");

      await act.Should().ThrowAsync<InvalidOperationException>();
    }
  }

  public class ParseSelectorTests : AvaloniaInspectorTests
  {
    [Fact]
    public void ParseSelector_TypeAndName_ParsesCorrectly()
    {
      (string? type, string? name, string? className) = AvaloniaInspector.ParseSelector("Button#myBtn");

      type.Should().Be("Button");
      name.Should().Be("myBtn");
      className.Should().BeNull();
    }

    [Fact]
    public void ParseSelector_TypeAndClass_ParsesCorrectly()
    {
      (string? type, string? name, string? className) = AvaloniaInspector.ParseSelector("TextBox.error");

      type.Should().Be("TextBox");
      name.Should().BeNull();
      className.Should().Be("error");
    }

    [Fact]
    public void ParseSelector_NameOnly_ParsesCorrectly()
    {
      (string? type, string? name, string? className) = AvaloniaInspector.ParseSelector("#myBtn");

      type.Should().BeNull();
      name.Should().Be("myBtn");
      className.Should().BeNull();
    }

    [Fact]
    public void ParseSelector_ClassOnly_ParsesCorrectly()
    {
      (string? type, string? name, string? className) = AvaloniaInspector.ParseSelector(".error");

      type.Should().BeNull();
      name.Should().BeNull();
      className.Should().Be("error");
    }

    [Fact]
    public void ParseSelector_TypeOnly_ParsesCorrectly()
    {
      (string? type, string? name, string? className) = AvaloniaInspector.ParseSelector("Button");

      type.Should().Be("Button");
      name.Should().BeNull();
      className.Should().BeNull();
    }
  }

  public class MatchesFilterTests : AvaloniaInspectorTests
  {
    [AvaloniaFact]
    public void MatchesFilter_ByName_MatchesCorrectElement()
    {
      Button button = new() { Name = "submit" };

      bool result = AvaloniaInspector.MatchesFilter(button, "submit", null, null);

      result.Should().BeTrue();
    }

    [AvaloniaFact]
    public void MatchesFilter_ByName_DoesNotMatchWrongName()
    {
      Button button = new() { Name = "cancel" };

      bool result = AvaloniaInspector.MatchesFilter(button, "submit", null, null);

      result.Should().BeFalse();
    }

    [AvaloniaFact]
    public void MatchesFilter_ByTypeName_MatchesCorrectType()
    {
      Button button = new() { Name = "test" };

      bool result = AvaloniaInspector.MatchesFilter(button, null, "Button", null);

      result.Should().BeTrue();
    }

    [AvaloniaFact]
    public void MatchesFilter_ByClassName_MatchesWhenClassPresent()
    {
      Button button = new() { Name = "test" };
      button.Classes.Add("primary");

      bool result = AvaloniaInspector.MatchesFilter(button, null, null, "primary");

      result.Should().BeTrue();
    }

    [AvaloniaFact]
    public void MatchesFilter_ByClassName_DoesNotMatchWhenClassAbsent()
    {
      Button button = new() { Name = "test" };

      bool result = AvaloniaInspector.MatchesFilter(button, null, null, "primary");

      result.Should().BeFalse();
    }

    [AvaloniaFact]
    public void MatchesFilter_AllCriteria_MustAllMatch()
    {
      Button button = new() { Name = "submit" };
      button.Classes.Add("primary");

      bool result = AvaloniaInspector.MatchesFilter(button, "submit", "Button", "primary");

      result.Should().BeTrue();
    }

    [AvaloniaFact]
    public void MatchesFilter_AllCriteria_FailsIfAnyDoesNotMatch()
    {
      Button button = new() { Name = "submit" };

      bool result = AvaloniaInspector.MatchesFilter(button, "submit", "TextBox", null);

      result.Should().BeFalse();
    }

    [AvaloniaFact]
    public void MatchesFilter_AllNull_MatchesEverything()
    {
      Button button = new() { Name = "test" };

      bool result = AvaloniaInspector.MatchesFilter(button, null, null, null);

      result.Should().BeTrue();
    }
  }

  public class GetElementPropertiesAsyncTests : AvaloniaInspectorTests
  {
    [AvaloniaFact]
    public async Task GetElementPropertiesAsync_ThrowsWhenNoMainWindow()
    {
      AvaloniaInspector inspector = CreateInspector();

      Func<Task<ElementProperties>> act = () => inspector.GetElementPropertiesAsync("test");

      await act.Should().ThrowAsync<InvalidOperationException>();
    }
  }

  public class BuildElementPropertiesTests : AvaloniaInspectorTests
  {
    [AvaloniaFact]
    public void BuildElementProperties_ReturnsCorrectType()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();
      Visual button = AvaloniaInspector.ResolveElement(root, "submit", null, null)!;

      ElementProperties props = AvaloniaInspector.BuildElementProperties(root, button);

      props.Type.Should().Be("Button");
      props.Name.Should().Be("submit");
    }

    [AvaloniaFact]
    public void BuildElementProperties_ReturnsElementId()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();
      Visual button = AvaloniaInspector.ResolveElement(root, "submit", null, null)!;

      ElementProperties props = AvaloniaInspector.BuildElementProperties(root, button);

      props.ElementId.Should().Be("path:1/1");
    }

    [AvaloniaFact]
    public void BuildElementProperties_ReturnsBounds()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();
      Visual button = AvaloniaInspector.ResolveElement(root, "submit", null, null)!;

      ElementProperties props = AvaloniaInspector.BuildElementProperties(root, button);

      props.Bounds.Should().NotBeNull();
    }

    [AvaloniaFact]
    public void BuildElementProperties_ReturnsStateFlags()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();
      Visual button = AvaloniaInspector.ResolveElement(root, "submit", null, null)!;

      ElementProperties props = AvaloniaInspector.BuildElementProperties(root, button);

      props.IsVisible.Should().BeTrue();
      props.IsEnabled.Should().BeTrue();
      props.Opacity.Should().Be(1.0);
    }

    [AvaloniaFact]
    public void BuildElementProperties_ReturnsClasses()
    {
      AvaloniaInspector inspector = CreateInspector();
      Button button = new() { Name = "test" };
      button.Classes.Add("primary");
      StackPanel root = new();
      root.Children.Add(button);

      ElementProperties props = AvaloniaInspector.BuildElementProperties(root, button);

      props.Classes.Should().Contain("primary");
    }

    [AvaloniaFact]
    public void BuildElementProperties_ReturnsAlignmentStrings()
    {
      AvaloniaInspector inspector = CreateInspector();
      StackPanel root = BuildTestTree();
      Visual button = AvaloniaInspector.ResolveElement(root, "submit", null, null)!;

      ElementProperties props = AvaloniaInspector.BuildElementProperties(root, button);

      // Default alignment should be a valid string
      props.HorizontalAlignment.Should().NotBeNullOrEmpty();
      props.VerticalAlignment.Should().NotBeNullOrEmpty();
    }
  }

  public class GetAncestorChainAsyncTests : AvaloniaInspectorTests
  {
    [AvaloniaFact]
    public async Task GetAncestorChainAsync_ThrowsWhenNoMainWindow()
    {
      AvaloniaInspector inspector = CreateInspector();

      Func<Task<IReadOnlyList<VisualTreeNode>>> act = () => inspector.GetAncestorChainAsync("test");

      await act.Should().ThrowAsync<InvalidOperationException>();
    }
  }

  public class GetVisualTreeAsyncTests : AvaloniaInspectorTests
  {
    [AvaloniaFact]
    public async Task GetVisualTreeAsync_ThrowsWhenNoMainWindow()
    {
      AvaloniaInspector inspector = CreateInspector();

      Func<Task<VisualTreeNode>> act = () => inspector.GetVisualTreeAsync();

      await act.Should().ThrowAsync<InvalidOperationException>();
    }
  }

  public class GetAppMetadataAsyncTests : AvaloniaInspectorTests
  {
    [AvaloniaFact]
    public async Task GetAppMetadataAsync_ThrowsWhenNoMainWindow()
    {
      // In the headless test environment, ApplicationLifetime is null,
      // so GetTopLevel() returns null → should throw InvalidOperationException.
      AvaloniaInspector inspector = CreateInspector();

      Func<Task<AppMetadata>> act = () => inspector.GetAppMetadataAsync();

      await act.Should().ThrowAsync<InvalidOperationException>();
    }
  }

  public class SimulateScrollAsyncTests : AvaloniaInspectorTests
  {
    [AvaloniaFact]
    public async Task SimulateScrollAsync_ThrowsWhenNoMainWindow()
    {
      AvaloniaInspector inspector = CreateInspector();

      Func<Task<InteractionResult>> act = () => inspector.SimulateScrollAsync("someElement");

      await act.Should().ThrowAsync<InvalidOperationException>();
    }
  }
}