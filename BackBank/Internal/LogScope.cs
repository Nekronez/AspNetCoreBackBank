using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BackBank.Internal
{
    public class LogScope
    {
        private static AsyncLocal<LogScope> _value = new AsyncLocal<LogScope>();

        private readonly object _state;

        internal LogScope(string key, object state)
        {
            _state = new NamedState(key, state);
        }

        public static LogScope Current
        {
            get
            {
                return _value.Value;
            }

            private set
            {
                _value.Value = value;
            }
        }

        public LogScope Parent { get; private set; }

        public static IDisposable Push(string key, object state)
        {
            var temp = Current;
            Current = new LogScope(key, state);
            Current.Parent = temp;

            return new DisposableScope();
        }

        private class DisposableScope : IDisposable
        {
            public void Dispose()
            {
                Current = Current.Parent;
            }
        }

        private class NamedState
        {
            public NamedState(string name, object state)
            {
                Name = name;
                State = state;
            }

            public string Name { get; private set; }

            public object State { get; private set; }

            public override string ToString()
            {
                return $"{Name}={State}";
            }
        }
    }
}
