using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace DataBinding
{
    /// <summary>
    /// Glues the view and view model together based on the binding attributes of the view.
    /// </summary>
    public class DataBinder
    {
        private object mView;
        private INotifyPropertyChanged mModel;
        private List<Action> mUnbindActions = new List<Action>();
        public bool BindingsApplied { get; private set; }

        public DataBinder( object view, INotifyPropertyChanged model )
        {
          this.mView = view;
          this.mModel = model;
          this.BindingsApplied = false;
        }

        public void ApplyBindings()
        {
            if ( !this.BindingsApplied && this.mModel != null && this.mModel is INotifyPropertyChanged )
            {
                var bundedMembers = this.GetBoundedMembers( this.mView );

                foreach ( var memberInfo in bundedMembers )
                {
                    var bindingAttributes = memberInfo.GetCustomAttributes( false ).ToList().Select( item => item as BindingAttribute );

                    foreach ( var bindingAttribute in bindingAttributes )
                    {
                        var control = GetControl( memberInfo );
                        var unbind = bindingAttribute.Bind( control, this.mModel );
                        this.mUnbindActions.Add( unbind );
                    }
                }

                this.BindingsApplied = true;
            }
        }

        public void DetachBindings()
        {
          if ( this.BindingsApplied )
          {
              this.mUnbindActions.ForEach( action => action.Invoke() );
              this.mUnbindActions.Clear();
              this.BindingsApplied = false;
          }
        }

        private object GetControl( MemberInfo memberInfo )
        {
            return memberInfo is FieldInfo ?
              ( memberInfo as FieldInfo ).GetValue( this.mView ) :
              ( memberInfo as PropertyInfo ).GetValue( this.mView, null );
        }

        private IEnumerable<MemberInfo> GetBoundedMembers( object view )
        {
            var members = new List<MemberInfo>();

            var fields = view.GetType().GetFields( System.Reflection.BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
            members.AddRange( fields );

            var properties = view.GetType().GetProperties( System.Reflection.BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
            members.AddRange( properties );

            return members.Where( memberInfo =>
                memberInfo.IsDefined( typeof( OneWayBinding ), false ) ||
                memberInfo.IsDefined( typeof( OneTimeBinding ), false ) ||
                memberInfo.IsDefined( typeof( TwoWayBinding ), false ) ||
                memberInfo.IsDefined( typeof( OneWayToSourceBinding ), false ) ||
                memberInfo.IsDefined( typeof( CommandBinding ), false ) ).ToList();
        }
    }
}
