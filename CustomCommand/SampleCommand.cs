using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace wpf522.CustomCommand
{
    public class SampleCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public Action<object> ExecuteAction { get; set; }

        public Func<object, bool> ExecuteFunc { get; set; }

        public SampleCommand(Func<object, bool> func, Action<object> action)
        {
            this.ExecuteAction = action;
            this.ExecuteFunc = func;
        }

        public bool CanExecute(object? parameter)
        {
            return ExecuteFunc?.Invoke(parameter) ?? true;
        }

        public void Execute(object? parameter)
        {
            ExecuteAction?.Invoke(parameter);
        }
    }
}

