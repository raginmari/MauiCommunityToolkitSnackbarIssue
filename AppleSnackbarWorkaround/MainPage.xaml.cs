using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Accessibility;
#if IOS
using UIKit;
#endif

namespace AppleSnackbarWorkaround;

public partial class MainPage
{
    private int _count = 0;

    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnCounterClicked(object sender, EventArgs e)
    {
        _count++;

        if (_count == 1)
            CounterBtn.Text = $"Clicked {_count} time";
        else
            CounterBtn.Text = $"Clicked {_count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);

        await ShowSnackbarAsync(CounterBtn.Text);
    }

    private async Task ShowSnackbarAsync(string message)
    {
        var snackbarOptions = new SnackbarOptions();
        var snackbar = Snackbar.Make(message, null, "OK", TimeSpan.FromSeconds(2.5), snackbarOptions); //, HelloWorld); // <-- AnchorView

        // On iOS, the following line calls:
        // 1. https://github.com/CommunityToolkit/Maui/blob/main/src/CommunityToolkit.Maui/Alerts/Snackbar/Snackbar.macios.cs#L76
        // 2. https://github.com/CommunityToolkit/Maui/blob/main/src/CommunityToolkit.Maui/Alerts/Toast/Toast.shared.cs#L81
        // 3. https://github.com/CommunityToolkit/Maui/blob/main/src/CommunityToolkit.Maui.Core/Views/Alert/AlertView.macios.cs#L52 (among other things)
        // 4. https://github.com/CommunityToolkit/Maui/blob/main/src/CommunityToolkit.Maui.Core/Views/Alert/AlertView.macios.cs#L73
        await snackbar.Show(CancellationToken.None);

        // FixSnackbarOnPlatform();
    }

    private void FixSnackbarOnPlatform()
    {
#if IOS
        // At this point, an "AlertView" (type UIView) has been added to the key window

        if (Handler?.PlatformView is not UIView view) return;

        // Find the AlertView in the window's immediate children (works even if the snackbar is "anchored" to another view)
        var snackbarViewTypeName = typeof(CommunityToolkit.Maui.Core.Views.AlertView).FullName!;
        var snackbarView = view.Window.Subviews.FirstOrDefault(x => (x.GetType().FullName ?? "").Equals(snackbarViewTypeName, StringComparison.InvariantCulture));

        if (snackbarView is null) return;

        // Find all active constraints that refer to the AlertView
        var parent = view.Window;
        var candidateConstraints = parent.Constraints
            .Where(x => x.Active)
            .Where(x =>
                x.FirstItem is UILayoutGuide { OwningView: not null } guide1 && guide1.OwningView.Equals(snackbarView) ||
                x.SecondItem is UILayoutGuide { OwningView: not null } guide2 && guide2.OwningView.Equals(snackbarView));

        // Replace the flexible leading and trailing constraints with fix ones
        foreach (var constraint in candidateConstraints)
        {
            switch (constraint.FirstAttribute, constraint.SecondAttribute)
            {
                case (NSLayoutAttribute.Leading, NSLayoutAttribute.Leading):
                    constraint.Active = false;
                    snackbarView.SafeLeadingAnchor().ConstraintEqualTo(parent.SafeLeadingAnchor(), (nfloat)Math.Abs(constraint.Constant)).Active = true;
                    break;
                case (NSLayoutAttribute.Trailing, NSLayoutAttribute.Trailing):
                    constraint.Active = false;
                    snackbarView.SafeTrailingAnchor().ConstraintEqualTo(parent.SafeTrailingAnchor(), -(nfloat)Math.Abs(constraint.Constant)).Active = true;
                    break;
            }
        }
#endif
    }
}
