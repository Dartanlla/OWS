using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using SimpleInjector.Lifestyles;


namespace OWSInstanceLauncher.Services
{
    public class TimedHostedService<TService> : IHostedService, IDisposable where TService : class
    {
        private readonly SimpleInjector.Container container;
        private readonly Settings settings;
        //private readonly ILogger logger;
        private readonly Timer timer;

        public TimedHostedService(SimpleInjector.Container container, Settings settings/*, ILogger logger*/)
        {
            this.container = container;
            this.settings = settings;
            //this.logger = logger;
            this.timer = new Timer(callback: _ => this.DoWork());
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Verify that TService can be resolved
            this.container.GetRegistration(typeof(TService), true);
            // Start the timer
            this.timer.Change(dueTime: TimeSpan.Zero, period: this.settings.Interval);
            return Task.CompletedTask;
        }

        private void DoWork()
        {
            try
            {
                using (AsyncScopedLifestyle.BeginScope(this.container))
                {
                    var service = this.container.GetInstance<TService>();
                    this.settings.Action(service);
                    if (settings.RunOnce)
                    {
                        this.timer.Change(Timeout.Infinite, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                //this.logger.LogError(ex, ex.Message);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.timer.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        //public void Dispose() => this.timer.Dispose();

        public void Dispose()
        {
            try
            {
                using (AsyncScopedLifestyle.BeginScope(this.container))
                {
                    var service = this.container.GetInstance<TService>();
                    this.settings.Dispose(service);
                }
            }
            catch (Exception ex)
            {
                //this.logger.LogError(ex, ex.Message);
            }

            this.timer.Dispose();
        }

        public class Settings
        {
            public readonly TimeSpan Interval;
            public readonly bool RunOnce;
            public readonly Action<TService> Action;
            public readonly Action<TService> Dispose;

            public Settings(TimeSpan interval, bool runOnce, Action<TService> action, Action<TService> dispose)
            {
                this.Interval = interval;
                this.RunOnce = runOnce;
                this.Action = action;
                this.Dispose = dispose;
            }
        }
    }
}
