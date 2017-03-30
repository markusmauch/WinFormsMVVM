using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace DataBinder
{
    public interface Command
    {
        bool CanExecute( object parameter );
        void Execute( object parameter );
        event EventHandler CanExecuteChanged;
    }

    public class RelayCommand : Command
    {
        private Predicate<object> mCanExecute;
        private Action<object> mExecute;
        public event EventHandler CanExecuteChanged;

        public RelayCommand( Action<object> execute, Predicate<object> canExecute = null )
        {
            this.mExecute = execute;
            this.mCanExecute = canExecute ?? ( p => true );
        }

        public bool CanExecute( object parameter )
        {
            return this.mCanExecute( parameter );
        }

        public void Execute( object parameter )
        {
            if ( this.mCanExecute( parameter ) )
            {
                this.mExecute( parameter );
            }
        }

        public void OnCanExecuteChanged( bool canExecute )
        {
            this.CanExecuteChanged?.Invoke( this, new EventArgs() );
        }
    }
}