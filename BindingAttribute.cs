using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace DataBinding
{
  /// <summary>
  /// Abstract base class for all binding attributes.
  /// </summary>
  [AttributeUsage( AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true )]
  public abstract class TLcBindingAttribute : Attribute
  {
    protected class BindingClosure
    {
      public BindingClosure( Action operation )
      {
        this.Execute = operation;
      }

      public Action Execute { get; private set; }

      public MethodInfo MethodInfo
      {
        get
        {
          return this.GetType().GetMethod( nameof( this.OnChange ), BindingFlags.NonPublic | BindingFlags.Instance );
        }
      }

      private void OnChange( object sender, EventArgs args )
      {
        this.Execute();
      }
    }

    #region Properties

    public string ModelPropertyName { get; set; }

    public object[] ModelPropertyIndex { get; set; }

    public string ControlPropertyName { get; set; }

    public object[] ControlPropertyIndex { get; set; }

    public string ControlEventName { get; set; }

    public Type Converter { get; set; }

    public object ConvertParameters { get; set; }

    #endregion

    #region Binding

    /// <summary>
    /// Sets up the binding and returns an action that - when invoked - cleans the binding up.
    /// </summary>
    /// <param name="control"></param>
    /// <param name="model"></param>
    /// <returns></returns>
    public abstract Action Bind( object control, INotifyPropertyChanged model );

    protected Action RegisterForwardBinding( INotifyPropertyChanged model, BindingClosure applyBindingOnce )
    {
      PropertyChangedEventHandler handler = ( sender, e ) =>
      {
        if ( e.PropertyName == this.ModelPropertyName )
        {
          applyBindingOnce.Execute();
        }
      };
      model.PropertyChanged += handler;

      return () =>
      {
        model.PropertyChanged -= handler;
        handler = null;
      };
    }

    protected Action RegisterReverseBinding( object control, BindingClosure applyReverseBindingOnce )
    {
      var changeEventInfo = control.GetType().GetEvent( this.ControlEventName );
      var handler = Delegate.CreateDelegate( changeEventInfo.EventHandlerType, applyReverseBindingOnce, applyReverseBindingOnce.MethodInfo );
      changeEventInfo.AddEventHandler( control, handler );
      return () =>
      {
        changeEventInfo.RemoveEventHandler( control, handler );
        handler = null;
      };
    }

    #endregion

    #region Binding Closure Actions

    public enum BindingDirection { Forward, Reverse };

    protected void TransferValue(
      BindingDirection direction,
      object source,
      object target,
      PropertyInfo sourcePropertyInfo,
      PropertyInfo targetPropertyInfo,
      object[] sourceIndex = null,
      object[] targetIndex = null )
    {
      var valueConverter = this.Converter != null ?
          Activator.CreateInstance( this.Converter ) as ValueConverter :
          new IdentityConverter();

      var value = this.GetSourceValue( source, sourcePropertyInfo, sourceIndex );
      var convertedValue = direction == BindingDirection.Forward ?
          valueConverter.Convert( value, null, this.ConvertParameters, CultureInfo.CurrentCulture ) :
          valueConverter.ConvertBack( value, null, this.ConvertParameters, CultureInfo.CurrentCulture );
      this.SetTargetValue( target, targetPropertyInfo, targetIndex, convertedValue );
    }

    private void SetTargetValue( object target, PropertyInfo targetPropertyInfo, object[] targetIndex, object convertedValue )
    {
      var defaultMembers = targetPropertyInfo.GetValue( target, null ).GetType().GetCustomAttributes( typeof( DefaultMemberAttribute ), true );
      if ( defaultMembers.Length > 0 && targetIndex != null )
      {
        var indexerName = ( ( DefaultMemberAttribute ) defaultMembers[ 0 ] ).MemberName;
        var indexer = targetPropertyInfo.GetValue( target, null );
        var indexerPropertyInfo = indexer.GetType().GetProperty( indexerName );
        indexerPropertyInfo.SetValue( indexer, convertedValue, targetIndex );
      }
      else
      {
        targetPropertyInfo.SetValue( target, convertedValue, null );
      }
    }

    private object GetSourceValue( object source, PropertyInfo sourcePropertyInfo, object[] sourceIndex )
    {
      var value = sourcePropertyInfo.GetValue( source, null );

      // indexed source property
      if ( value != null )
      {
        var defaultMembers = value.GetType().GetCustomAttributes( typeof( DefaultMemberAttribute ), true );
        if ( defaultMembers.Length > 0 && sourceIndex != null )
        {
          var indexerName = ( ( DefaultMemberAttribute ) defaultMembers[ 0 ] ).MemberName;
          value = value.GetType().GetProperty( indexerName ).GetValue( value, sourceIndex );
        }
      }

      return value;
    }

    protected void ExecuteCommand( object model, PropertyInfo modelPropertyInfo )
    {
      var command = modelPropertyInfo.GetValue( model, null ) as Command;
      if ( command.CanExecute( null ) )
      {
        command.Execute( null );
      }
    }

    #endregion
  }

  /// <summary>
  /// Updates the binding target when the application starts or when the data context changes.
  /// This type of binding is appropriate if you are using data where either a snapshot of the current state is appropriate to use or the data is truly static.
  /// This type of binding is also useful if you want to initialize your target property with some value from a source property and the data context is not
  /// known in advance. This is essentially a simpler form of OneWay binding that provides better performance in cases where the source value does not change.
  /// </summary>
  public class OneTimeBinding : TLcBindingAttribute
  {
    public override Action Bind( object control, INotifyPropertyChanged model )
    {
      var controlPropertyInfo = control.GetType().GetProperty( this.ControlPropertyName );
      var modelPropertyInfo = model.GetType().GetProperty( this.ModelPropertyName );
      var forwardBinding = new BindingClosure( () => this.TransferValue( BindingDirection.Forward, model, control, modelPropertyInfo, controlPropertyInfo ) );
      forwardBinding.Execute();
      var unregisterForwardBinding = this.RegisterForwardBinding( model, forwardBinding );
      return () =>
      {
        unregisterForwardBinding.Invoke();
        unregisterForwardBinding = null;
      };
    }
  }

  /// <summary>
  /// Updates the binding target (target) property when the binding source (source) changes.
  /// This type of binding is appropriate if the control being bound is implicitly read-only.
  /// </summary>
  public class OneWayBinding : TLcBindingAttribute
  {
    public override Action Bind( object control, INotifyPropertyChanged model )
    {
      var controlPropertyInfo = control.GetType().GetProperty( this.ControlPropertyName );
      var modelPropertyInfo = model.GetType().GetProperty( this.ModelPropertyName );
      var forwardBinding = new BindingClosure( () => this.TransferValue( BindingDirection.Forward, model, control, modelPropertyInfo, controlPropertyInfo, this.ModelPropertyIndex, this.ControlPropertyIndex ) );
      forwardBinding.Execute();
      var unregisterForwardBinding = this.RegisterForwardBinding( model, forwardBinding );
      return unregisterForwardBinding;
    }
  }

  /// <summary>
  /// Updates the source property when the target property changes.
  /// </summary>
  public class OneWayToSourceBinding : TLcBindingAttribute
  {
    public override Action Bind( object control, INotifyPropertyChanged model )
    {
      var controlPropertyInfo = control.GetType().GetProperty( this.ControlPropertyName );
      var modelPropertyInfo = model.GetType().GetProperty( this.ModelPropertyName );
      var reverseBinding = new BindingClosure( () => this.TransferValue( BindingDirection.Reverse, control, model, controlPropertyInfo, modelPropertyInfo, this.ControlPropertyIndex, this.ModelPropertyIndex ) );
      reverseBinding.Execute();
      var unregisterReverseBinding = this.RegisterReverseBinding( control, reverseBinding );
      return () =>
      {
        unregisterReverseBinding.Invoke();
        unregisterReverseBinding = null;
      };
    }
  }

  /// <summary>
  /// Causes changes to either the source property or the target property to automatically update the other.
  /// This type of binding is appropriate for editable forms or other fully-interactive UI scenarios.
  /// </summary>
  public class TwoWayBinding : TLcBindingAttribute
  {
    public override Action Bind( object control, INotifyPropertyChanged model )
    {
      var controlPropertyInfo = control.GetType().GetProperty( this.ControlPropertyName );
      var modelPropertyInfo = model.GetType().GetProperty( this.ModelPropertyName );
      var forwardBinding = new BindingClosure( () => this.TransferValue( BindingDirection.Forward, model, control, modelPropertyInfo, controlPropertyInfo, this.ModelPropertyIndex, this.ControlPropertyIndex ) );
      var reverseBinding = new BindingClosure( () => this.TransferValue( BindingDirection.Reverse, control, model, controlPropertyInfo, modelPropertyInfo, this.ControlPropertyIndex, this.ModelPropertyIndex ) );
      forwardBinding.Execute();

      var unregisterForwardBinding = this.RegisterForwardBinding( model, forwardBinding );
      var unregisterReverseBinding = this.RegisterReverseBinding( control, reverseBinding );

      return () =>
      {
        unregisterForwardBinding.Invoke();
        unregisterForwardBinding = null;
        unregisterReverseBinding.Invoke();
        unregisterReverseBinding = null;
      };
    }
  }

  /// <summary>
  /// Binds a <see cref="Command"/> to the event handlers that implement the command.
  /// </summary>
  public class CommandBinding : TLcBindingAttribute
  {
    public override Action Bind( object control, INotifyPropertyChanged model )
    {
      var modelPropertyInfo = model.GetType().GetProperty( this.ModelPropertyName );
      var commandBinding = new BindingClosure( () => this.ExecuteCommand( model, modelPropertyInfo ) );
      var unregisterReverseBinding = this.RegisterReverseBinding( control, commandBinding );
      return () =>
      {
        unregisterReverseBinding.Invoke();
        unregisterReverseBinding = null;
      };
    }
  }
}
