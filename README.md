# WinFormsMVVM
MVVM layer for Windows Forms

The binding takes place in the generated \*.designer (code behind) file.

_Examples:_

```
[OneWayBinding(
  ModelPropertyName = nameof( MyModel.ButtonText ),
  ControlPropertyName = nameof( Button.Caption ) )]
public Button myButton;
```
Binds the model property ```ButtonText``` to the control property ```Caption```.

```
[TwoWayBinding(
  ModelPropertyName = nameof( MyModel.Text ),
  ControlEventName = nameof( TextBox.OnChange ),
  ControlPropertyName = nameof( TextBox.Text ) )]
public TextBox myButton;
```
Binds the model property ```Text``` to the control property ```Text``` and vice versa.

In order to apply the bindings, create an instance of the ```DataBinder``` in the ```FormLoad``` event or from where ever it is convenient and invoke the ```ApplyBindings``` method.

*Available bindings*:

- OneWayBinding
- TwoWayBinding
- OneWayToSourceBinding
- OneTimeBinding

*Available value converters*:
- StringFormatConverter
- InverseBooleanConverter
- DoubleToIntConverter
- EnumToIntConverter

This is a work in progress and does not claim to be complete. Feel free to contribute!
