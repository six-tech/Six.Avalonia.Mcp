namespace Six.Avalonia.Mcp.Connector.Tests.Unit;

public class McpConnectorPostConfigureTests
{
  private readonly McpConnectorPostConfigure _sut = new();

  [Fact]
  public void DefaultAppName_ResolvesToEntryAssemblyName()
  {
    var options = new McpConnectorOptions { EnvironmentPrefix = null };

    _sut.PostConfigure(null, options);

    // When running under xUnit, entry assembly is the test runner — not "Avalonia App"
    options.AppName.Should().NotBe(McpConnectorOptions.DefaultAppName);
  }

  [Fact]
  public void ExplicitAppName_IsNotOverriddenByAutoDetection()
  {
    var options = new McpConnectorOptions
    {
      AppName = "My Custom App",
      EnvironmentPrefix = null
    };

    _sut.PostConfigure(null, options);

    options.AppName.Should().Be("My Custom App");
  }

  [Fact]
  public void EnvVar_OverridesDefaultServerUrl()
  {
    const string key = "SIX_MCP_ServerUrl";

    try
    {
      Environment.SetEnvironmentVariable(key, "http://custom:1234/hub");
      var options = new McpConnectorOptions();

      _sut.PostConfigure(null, options);

      options.ServerUrl.Should().Be("http://custom:1234/hub");
    }
    finally
    {
      Environment.SetEnvironmentVariable(key, null);
    }
  }

  [Fact]
  public void EnvVar_DoesNotOverrideExplicitServerUrl()
  {
    const string key = "SIX_MCP_ServerUrl";

    try
    {
      Environment.SetEnvironmentVariable(key, "http://env-value:1234/hub");
      var options = new McpConnectorOptions { ServerUrl = "http://explicit:9999/hub" };

      _sut.PostConfigure(null, options);

      options.ServerUrl.Should().Be("http://explicit:9999/hub");
    }
    finally
    {
      Environment.SetEnvironmentVariable(key, null);
    }
  }

  [Fact]
  public void EnvVar_OverridesDefaultAutoConnect()
  {
    const string key = "SIX_MCP_AutoConnect";

    try
    {
      Environment.SetEnvironmentVariable(key, "false");
      var options = new McpConnectorOptions();

      _sut.PostConfigure(null, options);

      options.AutoConnect.Should().BeFalse();
    }
    finally
    {
      Environment.SetEnvironmentVariable(key, null);
    }
  }

  [Fact]
  public void EnvVar_OverridesDefaultReconnectDelay()
  {
    const string key = "SIX_MCP_ReconnectDelay";

    try
    {
      Environment.SetEnvironmentVariable(key, "10.5");
      var options = new McpConnectorOptions();

      _sut.PostConfigure(null, options);

      options.ReconnectDelay.Should().Be(TimeSpan.FromSeconds(10.5));
    }
    finally
    {
      Environment.SetEnvironmentVariable(key, null);
    }
  }

  [Fact]
  public void MalformedAutoConnectEnvVar_IsIgnored()
  {
    const string key = "SIX_MCP_AutoConnect";

    try
    {
      Environment.SetEnvironmentVariable(key, "not-a-bool");
      var options = new McpConnectorOptions();

      _sut.PostConfigure(null, options);

      options.AutoConnect.Should().Be(McpConnectorOptions.DefaultAutoConnect);
    }
    finally
    {
      Environment.SetEnvironmentVariable(key, null);
    }
  }

  [Fact]
  public void MalformedReconnectDelayEnvVar_IsIgnored()
  {
    const string key = "SIX_MCP_ReconnectDelay";

    try
    {
      Environment.SetEnvironmentVariable(key, "not-a-number");
      var options = new McpConnectorOptions();

      _sut.PostConfigure(null, options);

      options.ReconnectDelay.Should().Be(McpConnectorOptions.DefaultReconnectDelay);
    }
    finally
    {
      Environment.SetEnvironmentVariable(key, null);
    }
  }

  [Fact]
  public void NullPrefix_DisablesEnvVarBinding()
  {
    const string key = "SIX_MCP_ServerUrl";

    try
    {
      Environment.SetEnvironmentVariable(key, "http://should-not-apply:1234/hub");
      var options = new McpConnectorOptions { EnvironmentPrefix = null };

      _sut.PostConfigure(null, options);

      options.ServerUrl.Should().Be(McpConnectorOptions.DefaultServerUrl);
    }
    finally
    {
      Environment.SetEnvironmentVariable(key, null);
    }
  }

  [Fact]
  public void CustomPrefix_ReadsCorrectEnvVars()
  {
    const string key = "MYAPP_ServerUrl";

    try
    {
      Environment.SetEnvironmentVariable(key, "http://custom-prefix:5555/hub");
      var options = new McpConnectorOptions { EnvironmentPrefix = "MYAPP_" };

      _sut.PostConfigure(null, options);

      options.ServerUrl.Should().Be("http://custom-prefix:5555/hub");
    }
    finally
    {
      Environment.SetEnvironmentVariable(key, null);
    }
  }

  [Fact]
  public void EnvVarAppName_TakesPriorityOverAssemblyAutoDetection()
  {
    const string key = "SIX_MCP_AppName";

    try
    {
      Environment.SetEnvironmentVariable(key, "EnvOverride");
      var options = new McpConnectorOptions();

      _sut.PostConfigure(null, options);

      options.AppName.Should().Be("EnvOverride");
    }
    finally
    {
      Environment.SetEnvironmentVariable(key, null);
    }
  }

  [Fact]
  public void EnvVar_OverridesDefaultScreenshotDirectory()
  {
    const string key = "SIX_MCP_ScreenshotDirectory";

    try
    {
      Environment.SetEnvironmentVariable(key, "/custom/screenshots");
      var options = new McpConnectorOptions();

      _sut.PostConfigure(null, options);

      options.ScreenshotDirectory.Should().Be("/custom/screenshots");
    }
    finally
    {
      Environment.SetEnvironmentVariable(key, null);
    }
  }

  [Fact]
  public void EnvVar_DoesNotOverrideExplicitScreenshotDirectory()
  {
    const string key = "SIX_MCP_ScreenshotDirectory";

    try
    {
      Environment.SetEnvironmentVariable(key, "/env/screenshots");
      var options = new McpConnectorOptions { ScreenshotDirectory = "/explicit/screenshots" };

      _sut.PostConfigure(null, options);

      options.ScreenshotDirectory.Should().Be("/explicit/screenshots");
    }
    finally
    {
      Environment.SetEnvironmentVariable(key, null);
    }
  }

  [Fact]
  public void NegativeReconnectDelay_IsIgnored()
  {
    const string key = "SIX_MCP_ReconnectDelay";

    try
    {
      Environment.SetEnvironmentVariable(key, "-5");
      var options = new McpConnectorOptions();

      _sut.PostConfigure(null, options);

      options.ReconnectDelay.Should().Be(McpConnectorOptions.DefaultReconnectDelay);
    }
    finally
    {
      Environment.SetEnvironmentVariable(key, null);
    }
  }

  [Fact]
  public void EnvVar_OverridesDefaultMaxRetryAttempts()
  {
    const string key = "SIX_MCP_MaxRetryAttempts";

    try
    {
      Environment.SetEnvironmentVariable(key, "10");
      var options = new McpConnectorOptions();

      _sut.PostConfigure(null, options);

      options.MaxRetryAttempts.Should().Be(10);
    }
    finally
    {
      Environment.SetEnvironmentVariable(key, null);
    }
  }

  [Fact]
  public void MalformedMaxRetryAttemptsEnvVar_IsIgnored()
  {
    const string key = "SIX_MCP_MaxRetryAttempts";

    try
    {
      Environment.SetEnvironmentVariable(key, "not-a-number");
      var options = new McpConnectorOptions();

      _sut.PostConfigure(null, options);

      options.MaxRetryAttempts.Should().Be(McpConnectorOptions.DefaultMaxRetryAttempts);
    }
    finally
    {
      Environment.SetEnvironmentVariable(key, null);
    }
  }

  [Fact]
  public void NegativeMaxRetryAttempts_IsIgnored()
  {
    const string key = "SIX_MCP_MaxRetryAttempts";

    try
    {
      Environment.SetEnvironmentVariable(key, "-3");
      var options = new McpConnectorOptions();

      _sut.PostConfigure(null, options);

      options.MaxRetryAttempts.Should().Be(McpConnectorOptions.DefaultMaxRetryAttempts);
    }
    finally
    {
      Environment.SetEnvironmentVariable(key, null);
    }
  }
}
