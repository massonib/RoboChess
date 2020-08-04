using System;
using System.Windows.Input;

namespace RoboChess
{
    public class CommandWithCondition : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly Action _execute;

        private readonly Func<bool> _canExecute;

        public CommandWithCondition(Action execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        }
        bool lastCanState = false;
        public void CanExecute(object sender, EventArgs e)
        {
            CanExecute(sender);
        }
        public bool CanExecute(object parameter)
        {
            if (_canExecute == null) return true;
            var newState = _canExecute();
            if (newState == lastCanState) return lastCanState;
            lastCanState = newState;
            OnCanExecuteChanged();
            return newState;
        }

        public void Execute(object parameter) => _execute();

        public void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class CommandWithCondition<T> : ICommand
    {
        private readonly Action<T> _execute;

        private readonly Func<T, bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public CommandWithCondition(Action<T> execute, Func<T, bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        }
        bool lastCanState = false;
        public void CanExecute(object sender, EventArgs e)
        {
            CanExecute(sender);
        }
        public bool CanExecute(object parameter)
        {
            if (_canExecute == null) return true;
            var newState = _canExecute((T)parameter);
            if (newState == lastCanState) return lastCanState;
            lastCanState = newState;
            OnCanExecuteChanged();
            return newState;
        }

        public void Execute(object parameter) => _execute((T)parameter);

        public void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
