namespace DandTSoftware.Timers
{
    using System;
    using System.Timers;

    /// <summary>
    /// Midnight Timer Delegate for the event
    /// </summary>
    /// <param name="time"></param>
    public delegate void TimeReachedEventHandler(DateTime time);

    /// <summary>
    /// Provides the means to detect when midnight is reached.
    /// </summary>
    public class MidnightTimer : IDisposable
    {
        #region Static Variables

        private bool _disposed;

        /// <summary>
        /// Internal Timer
        /// </summary>
        private static Timer s_timer; // renamed from m_ to s_ to represent static

        /// <summary>
        /// How many Minutes after midnight are added to the timer
        /// </summary>
        private static int s_MinutesAfterMidnight;

        /// <summary>
        /// Occurs whens midnight occurs, subscribe to this
        /// </summary>
        public event TimeReachedEventHandler TimeReached;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance of the Midnight Timer
        /// </summary>
        public MidnightTimer()
        {
        }

        /// <summary>
        /// Creates an instance of the Midnight Timer, which will fire after a set number of minutes after midnight
        /// </summary>
        /// <param name="MinutesAfterMidnight">How many Minutes after midnight do we start the timer? between 0 and 59</param>
        public MidnightTimer(int MinutesAfterMidnight) : this()
        {
            // Check if the supplied m is between 0 and 59 mins after midnight
            if ((MinutesAfterMidnight < 0) || (MinutesAfterMidnight > 59))
            {
                // if it is outside of this range, throw a exception
                throw new ArgumentException("Minutes after midnight is less than 0 or more than 60!");
            }

            // Set the internal value
            s_MinutesAfterMidnight = MinutesAfterMidnight;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the Timer to fire a certain amount of Minutes AFTER midnight, every night (based on server time).
        /// </summary>
        public void Start()
        {
            // Subtract the current time, from midnigh (tomorrow).
            // This will return a value, which will be used to set the Timer interval
            var ts = this.GetMidnight(s_MinutesAfterMidnight).Subtract(DateTime.Now);

            // We only want the Hours, Minuters and Seconds until midnight
            var tsMidnight = new TimeSpan(ts.Hours, ts.Minutes, ts.Seconds);

            // Create the Timer
            s_timer = new Timer(tsMidnight.TotalMilliseconds);

            // Set the event handler
            s_timer.Elapsed += Timer_Elapsed;

            // Hook into when Windows Time changes - Thanks to Nicole1982 for the suggestion & BruceN for the help
#if Windows
            Microsoft.Win32.SystemEvents.TimeChanged += WindowsTimeChangeHandler;
#endif
            // TODO: Add other platforms support

            // Start the timer
            s_timer.Start();
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        public void Stop()
        {
            // sanity checking
            if (s_timer != null)
            {
                // Stop the orginal timer
                s_timer.Stop();

                // As this is a static event, clean it up
#if Windows
                Microsoft.Win32.SystemEvents.TimeChanged -= WindowsTimeChangeHandler;
#endif
                // TODO: Add other platforms support
            }
        }

        /// <summary>
        /// Restarts the timer
        /// </summary>
        public void Restart()
        {
            // Stop the timer
            Stop();

            // (Re)Start
            Start();
        }

        #endregion

        #region Hanlders

        /// <summary>
        /// Standard Event/Delegate handler, if its not null, fire the event
        /// </summary>
        private void OnTimeReached()
        {
            // Fire the event
            TimeReached?.Invoke(GetMidnight(s_MinutesAfterMidnight));
        }

        /// <summary>
        /// Handles Windows Time Changes which cause the timer to stop/start aka Reset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowsTimeChangeHandler(object sender, EventArgs e)
        {
            // Please see https://connect.microsoft.com/VisualStudio/feedback/details/776003/systemevent-timechanged-is-fired-twice
            // The event is fired twice.. I assume 'once' for the change from the old system time and 'once' when the time has been changed.
            // i.e Event is fired when Systerm time has Changed and is Changing

            // Restart the timer -> note as above, this is called twice
            Restart();
        }

        /// <summary>
        /// Executes when the timer has elasped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Stop the orginal timer
            s_timer.Stop(); // swapped order thanks to Jeremy

            // now raise a event that the timer has elapsed
            OnTimeReached(); // swapped order thanks to Jeremy

            // reset the timer
            Start();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Obtains a DateTime of Midngiht
        /// </summary>
        /// <param name="MinutesAfterMidnight">How many minuets after midnight to add?</param>
        /// <returns></returns>
        private DateTime GetMidnight(int MinutesAfterMidnight)
        {
            // Lets work out the next occuring midnight
            // Add 1 day and use hours 0, min 0 and second 0 (remember this is 24 hour time)

            // Thanks to Yashar for this code/fix
            var tomorrow = DateTime.Now.AddDays(1);

            // Return a datetime for Tomorrow, but with how many minutes after midnight
            return new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 0, MinutesAfterMidnight, 0);
        }

        #endregion

        #region Disposing

        /// <summary>
        /// Dispose of the timer (also stops the timer)
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // free managed resources
                // Pass to Stop to unsubscribe the event handler of Windows System Time Changes
                Stop();
                s_timer.Dispose();
            }

            // free native resources if there are any.
            //if (nativeResource != IntPtr.Zero)
            //{
            //}

            _disposed = true;
        }

        #endregion
    }
}