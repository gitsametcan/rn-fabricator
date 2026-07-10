using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Fabricator.Desktop.ViewModels;
using Fabricator.Desktop.Views;

namespace Fabricator.Tests.Desktop;

public sealed class DesktopShellSmokeTests
{
    [AvaloniaFact]
    public void MainWindowRendersStableShell()
    {
        var window = new MainWindow
        {
            DataContext = new MainViewModel()
        };

        window.Show();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);

        var visibleText = window
            .GetVisualDescendants()
            .OfType<TextBlock>()
            .Select(textBlock => textBlock.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();

        Assert.Equal("rn-fabricator", window.Title);
        Assert.IsType<MainViewModel>(window.DataContext);
        Assert.Contains("rn-fabricator", visibleText);
        Assert.Contains("Project workspace", visibleText);
        Assert.Contains("Desktop foundation", visibleText);
        Assert.Contains("Command boundary", visibleText);
        Assert.Contains("Planned workflows", visibleText);
    }

    [Fact]
    public void MainViewModelExposesExpectedShellSections()
    {
        var viewModel = new MainViewModel();

        Assert.Equal("Project workspace", viewModel.PageTitle);
        Assert.Equal(4, viewModel.NavigationItems.Count);
        Assert.Equal(4, viewModel.WorkflowCards.Count);
        Assert.Contains(viewModel.NavigationItems, item => item.Title == "Doctor");
        Assert.Contains(viewModel.NavigationItems, item => item.Title == "Templates");
        Assert.Contains(viewModel.WorkflowCards, item => item.Title == "Guided setup");
        Assert.Contains("Fabricator.Core", viewModel.CommandBoundary);
    }
}
