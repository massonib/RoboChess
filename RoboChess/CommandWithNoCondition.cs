using System;
using System.Windows.Input;

namespace RoboChess
{
    public class CommandWithNoCondition : ICommand
    {
        private readonly Action _execute;

        public CommandWithNoCondition(Action execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => _execute();

    }

    public class CommandWithNoCondition<T> : ICommand
    {
        private readonly Action<T> _execute;


        public event EventHandler CanExecuteChanged;

        public CommandWithNoCondition(Action<T> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => _execute((T)parameter);
    }
}
