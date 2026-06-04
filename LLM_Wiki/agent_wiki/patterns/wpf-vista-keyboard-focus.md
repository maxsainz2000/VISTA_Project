---
type: pattern
module: Infrastructure
agent: antigravity
date: 2026-06-04
tags: [wpf, xaml, vb-net, keyboard-focus, accessibility]
---

# WPF Keyboard Focus & Accessibility Foundation

This pattern establishes a robust keyboard navigation, visible focus indicators, and hotkey/cancel routing structure for standard controls and custom in-view modal overlays in VISTA's WPF applications.

## Context

When implementing keyboard accessibility and navigation floor (`UX-17`):
- Controls must display theme-aware visual focus indicators.
- Users must be able to navigate logically via the Tab key across views and sub-panels.
- Dialogs and modal overlays must focus the first input field upon becoming visible, and route standard confirmation/cancellation actions (`Enter`/`Escape`) safely without bubbling or breaking input fields.

## The Pattern

### 1. Application-wide Theme-Aware Focus Ring

Define a focus visual style dynamically referencing the `AccentBrush` inside the application-wide dictionary (e.g., `Themes/Controls.xaml`):

```xml
<Style x:Key="AppFocusVisual">
    <Setter Property="Control.Template">
        <Setter.Value>
            <ControlTemplate>
                <Border BorderBrush="{DynamicResource AccentBrush}"
                        BorderThickness="1.5"
                        CornerRadius="{DynamicResource RadiusSmall}"
                        Margin="1"/>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

Apply this custom template implicitly across all primary interactive controls by adding a setter to their default style:

```xml
<Setter Property="FocusVisualStyle" Value="{StaticResource AppFocusVisual}"/>
```

### 2. In-view Overlay Dialog Focus & Key Interception

For overlay elements representing modal popup "dialogs", handle focus transfer and key mapping through code-behind to guarantee UI thread accuracy:

1. **Focus First Field on Open**: Hook the `IsVisibleChanged` event on the overlay Grid/Border container. Focus the target textbox using `Dispatcher.BeginInvoke` to ensure layout has computed visibility.
2. **Handle Enter/Escape Hotkeys**: Hook `PreviewKeyDown` at the root `UserControl` level. Intercept Escape and Enter only when the respective overlay is active, execute the VM command, and mark the event `Handled = True`.

#### XAML Configuration

```xml
<UserControl x:Class="Views.MyView"
             ...
             PreviewKeyDown="UserControl_PreviewKeyDown">
    <Grid>
        <!-- Main Panel -->
        ...
        
        <!-- Overlay Panel -->
        <Grid x:Name="OverlayPanel"
              Visibility="{Binding IsOverlayOpen, Converter={StaticResource BoolToVis}}"
              IsVisibleChanged="OverlayPanel_IsVisibleChanged">
              
              <TextBox x:Name="FirstInputTextBox" TabIndex="11"/>
              
              <Button Content="_Confirm" TabIndex="12"/>
              <Button Content="Ca_ncel" TabIndex="13"/>
        </Grid>
    </Grid>
</UserControl>
```

#### Code-Behind (VB.NET)

```vb
Private Sub OverlayPanel_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
    If CType(e.NewValue, Boolean) Then
        Dispatcher.BeginInvoke(Sub()
                                   FirstInputTextBox.Focus()
                               End Sub, System.Windows.Threading.DispatcherPriority.Input)
    End If
End Sub

Private Sub UserControl_PreviewKeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs)
    Dim vm = TryCast(DataContext, MyViewModel)
    If vm IsNot Nothing AndAlso vm.IsOverlayOpen Then
        If e.Key = System.Windows.Input.Key.Escape Then
            If vm.CancelCommand.CanExecute(Nothing) Then
                vm.CancelCommand.Execute(Nothing)
            End If
            e.Handled = True
        ElseIf e.Key = System.Windows.Input.Key.Enter Then
            If vm.ConfirmCommand.CanExecute(Nothing) Then
                vm.ConfirmCommand.Execute(Nothing)
            End If
            e.Handled = True
        End If
    End If
End Sub
```

## Why It Works

- **Dynamic Theme Synchronization**: Setting the focus visual implicitly and using `{DynamicResource AccentBrush}` ensures focus rings automatically adjust color schemes between Light and Dark mode.
- **Asynchronous Focus Dispatch**: The `Dispatcher.BeginInvoke` with `Input` priority yields control back to WPF to complete layout processing. Attempting to call `.Focus()` synchronously before a control's visibility is finalized fails silent focus evaluation.
- **Preview Gated Key Interception**: Preview events run in the tunneling phase (from the root down). Intercepting keys at the root `UserControl` and marking them handled ensures that standard window bindings don't capture the key, and dialog hotkeys execute cleanly while textboxes inside the dialog still process local key events correctly.

## Rules

- **Access Key Mnemonics**: Prefix the access key letter with an underscore (`_`) on buttons (e.g. `_Save`, `Ca_ncel`). Ensure these letters are unique within each view.
- **Keyboard Navigation Sequence**: Set explicit `TabIndex` sequential integers on every interactive control (inputs, buttons, grids) while bypassing static labels.
- **Gated Handled Events**: Always mark routed keystrokes as `e.Handled = True` when executing overlay actions to prevent keyboard events from triggering duplicate underlying shell actions.
- **No Await in VB Catch/Finally**: Keep UI dispatcher code simple; do not use asynchronous operations or `Await` statements inside exception handling blocks.
