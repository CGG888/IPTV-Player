using System;
using System.Threading;
using System.Windows.Threading;

namespace LibmpvIptvClient.Tests
{
    internal static class WpfTestHost
    {
        static readonly ManualResetEventSlim Ready = new(false);
        static Dispatcher? _dispatcher;
        static Thread? _thread;

        static Dispatcher Dispatcher
        {
            get
            {
                EnsureStarted();
                return _dispatcher!;
            }
        }

        static void EnsureStarted()
        {
            if (_dispatcher != null) return;
            lock (Ready)
            {
                if (_dispatcher != null) return;
                _thread = new Thread(() =>
                {
                    _dispatcher = Dispatcher.CurrentDispatcher;
                    Ready.Set();
                    Dispatcher.Run();
                });
                _thread.IsBackground = true;
                _thread.SetApartmentState(ApartmentState.STA);
                _thread.Start();
                Ready.Wait(TimeSpan.FromSeconds(5));
            }
        }

        public static void Invoke(Action action)
        {
            Exception? error = null;
            Dispatcher.Invoke(() =>
            {
                try { action(); }
                catch (Exception ex) { error = ex; }
            });
            if (error != null) throw error;
        }
    }
}
