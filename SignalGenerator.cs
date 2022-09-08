using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace VeeTrader.Others
{
    /// <summary>
    /// Define period of timer event
    /// </summary>
    public enum SignalGeneratorAlign
    {
        /// <summary>
        /// Milliseconds period 
        /// </summary>
        Milliseconds,
        /// <summary>
        /// Seconds period
        /// </summary>
        Seconds,
        /// <summary>
        /// Minutes period
        /// </summary>
        Minutes,
        /// <summary>
        /// Hours period
        /// </summary>
        Hours
    }
    /// <summary>
    /// Generate new signal with specified intervals aligned by time borders
    /// </summary>
    public class SignalGenerator
    {
        /// <summary>
        /// List of clients
        /// </summary>
        Dictionary<string, Action> m_subscrible_list = new Dictionary<string, Action>();
        /// <summary>
        /// Subscrible count
        /// </summary>
        private int m_subscrible_count = 0;
        /// <summary>
        /// Synch obj
        /// </summary>
        private object m_state = new object();
        /// <summary>
        /// Timer
        /// </summary>
        private Timer m_timer;
        /// <summary>
        /// TimeSpan as align by time border
        /// </summary>
        private TimeSpan m_align;
        /// <summary>
        /// Time delay
        /// </summary>
        private TimeSpan m_time_delay;
        /// <summary>
        /// Количество шагов
        /// </summary>
        private long m_prev_steps = 0;
        /// <summary>
        /// Milliseconds elapsed between two call timer
        /// </summary>
        private long m_max_elapsed = 0;
        /// <summary>
        /// Stopwatch for exact time calculate
        /// </summary>
        private Stopwatch m_stopwatch = new Stopwatch();

        /// <summary>
        /// Prohibit premature triggering
        /// </summary>
        public bool UseStrongMoreTime { get; set; }
        /// <summary>
        /// Create signal generator with indicating the frequency of operation
        /// </summary>
        /// <param name="align">Align of time</param>
        /// <param name="period">Period of event</param>
        public SignalGenerator(SignalGeneratorAlign align, int period) : this(align, period, 20, false)
        {
        }
        /// <summary>
        /// Create signal generator with indicating the frequency of operation
        /// </summary>
        /// <param name="align">Align of time</param>
        /// <param name="period">Period of event</param>
        /// <param name="maxResolution">Maximum resolution of the timer in milliseconds. By default it equal 20 milliseconds. Don't change this value if undestund what do you do</param>
        public SignalGenerator(SignalGeneratorAlign align, int period, int maxResolution = 20, bool useStrongMoreTime = false)
        {
            //-- Set TimeSpan as time period
            UseStrongMoreTime = useStrongMoreTime;
            var align_to_time_span = new Dictionary<SignalGeneratorAlign, Func<int, TimeSpan>>
            {
                { SignalGeneratorAlign.Milliseconds, (count) => new TimeSpan(0, 0, 0, 0, count) },
                { SignalGeneratorAlign.Seconds, (count) => new TimeSpan(0, 0, count) },
                { SignalGeneratorAlign.Minutes, (count) => new TimeSpan(0, count, 0) },
                { SignalGeneratorAlign.Hours, (count) => new TimeSpan(count, 0, 0) },
            };
            m_align = align_to_time_span[align](period);
            m_time_delay = new TimeSpan(0, 0, 0, 0, maxResolution);
            m_timer = new Timer(OnTimer, m_state, m_time_delay.Milliseconds, m_time_delay.Milliseconds);
        }
        /// <summary>
        /// High-frequncy timer
        /// </summary>
        /// <param name="state"></param>
        private void OnTimer(object state)
        {
            try
            {
                Monitor.Enter(state);
                if (m_stopwatch.ElapsedMilliseconds > m_max_elapsed)
                    m_max_elapsed = m_stopwatch.ElapsedMilliseconds;
                DateTime time = DateTime.Now;
                long steps_next;
                if (!UseStrongMoreTime)
                    steps_next = (time + m_time_delay + m_time_delay).Ticks / m_align.Ticks;
                else
                    steps_next = time.Ticks / m_align.Ticks;
                if (steps_next > m_prev_steps)
                {
                    EventNewSignal?.Invoke();
                    m_prev_steps = steps_next;
                    m_max_elapsed = 0;
                }
                m_stopwatch.Restart();
            }
            catch(Exception ex)
            {
                ;
            }
            finally
            {
                Monitor.Exit(state);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void SubscribleOnSignal(string uniqKey, Action onSignal)
        {
            Action oldSubClient;
            //lock (m_subscrible_list)
            {
                if (!m_subscrible_list.TryGetValue(uniqKey, out oldSubClient))
                {
                    EventNewSignal += onSignal;
                    m_subscrible_list.Add(uniqKey, onSignal);
                    m_subscrible_count++;
                }
                else
                {
                    EventNewSignal -= oldSubClient;
                    EventNewSignal += onSignal;
                    m_subscrible_list[uniqKey] = onSignal;
                }
            }
        }
        /// <summary>
        /// Return current subscrible method
        /// </summary>
        public Action GetCurrentSubsribleMethod(string uniqKey)
        {
            Action currentMethod;
            m_subscrible_list.TryGetValue(uniqKey, out currentMethod);
            return currentMethod;
        }
        /// <summary>
        /// Unsubscrible client with uniq name from current signal
        /// </summary>
        public bool UnSubscribleOnSignal(string uniqKey)
        {
            Action oldSubClient;
            if (m_subscrible_list.TryGetValue(uniqKey, out oldSubClient))
            {
                EventNewSignal -= oldSubClient;
                return true;
            }
            return false;
        }
        /// <summary>
        /// New signal action
        /// </summary>
        public event Action EventNewSignal;
    }
}
