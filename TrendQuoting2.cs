using System;
using System.ComponentModel;
using System.Collections.Generic;
using TSLab.Script.Handlers;
using TSLab.Script.Realtime;
using TSLab.DataSource;
using TSLab.ScriptExecution.Realtime;
using VeeTrader.Others;
using System.Text;
using TSLab.Script;

/// <summary>
/// Disable obsolete warning
/// </summary>
#pragma warning disable CS0612
// Disable integrated variable
#pragma warning disable IDE0018
namespace TSLab.TestAddons
{
    /// <summary>
    /// Получатель сигнала через WebSocket
    /// </summary>
    [HandlerName("GettingLastBarTest")]
    [HandlerCategory("Testing")]
    public class TrendQuoting2 : BasePeriodIndicatorHandler, IContextUses, IBar2ValueDoubleHandler, IDisposable
    {
        Random m_rnd = new Random();
        /// <summary>
        /// Signal generator
        /// </summary>
        private static readonly SignalGenerator m_sig_gen = new SignalGenerator(SignalGeneratorAlign.Minutes, 1, 20, true);
        /// <summary>
        /// Запомненный последний рабочий символ для таймера
        /// </summary>
        ISecurityRt m_secRt;
        /// <summary>
        /// Current context
        /// </summary>
        public IContext Context { get; set; }
        /// <summary>
        /// Qty of each deal
        /// </summary>
        [HandlerParameter(Default = "true", NotOptimized = true)]
        [Description("Истина, если отклонение задается в процентах от 0 до 100 от текущей цены. Ложь - отклонение задается в пунктах.")]
        public bool UseDeviationPercent { get; set; }
        /// <summary>
        /// Connect string
        /// </summary>
        string m_connect_string = "tcp://gtam";
        /// <summary>
        /// Рудементарный параметр, используемый для инициализации таймера, больше никак
        /// </summary>
        [HandlerParameter(Default = "tcp://gtam", NotOptimized = true)]
        public string ConnectString
        {
            get
            {
                return m_connect_string;
            }
            set
            {
                m_connect_string = value;
                m_sig_gen.EventNewSignal += OnTimer;
            }
        }
        /// <summary>
        /// Unsubscrible from get timer
        /// </summary>
        public void Dispose()
        {
            m_sig_gen.EventNewSignal -= OnTimer;
        }
        /// <summary>
        /// Main execute module
        /// </summary>
        /// <param name="source"></param>
        /// <param name="barNum"></param>
        /// <returns></returns>
        public double Execute(ISecurity source, int barNum)
        {
            try
            {
                if (barNum >= source.Bars.Count)
                    return 0.0;
                if (IsRealTime(barNum))
                    ExecuteRealTime(source);
                int sign = m_rnd.Next(0, 1) == 0 ? -1 : 1;
                return sign;
            }
            catch (Exception ex)
            {
                StringBuilder info = new StringBuilder();
                info.Append("Получено необработанное исключение в обработчике SignalTakerInstance.Execute: ");
                info.Append(ex.Message).Append("\nStackTrace:\n");
                info.Append(ex.StackTrace);
                Context.Log(info.ToString(), new Color(), true);
                return 0;
            }
        }
        /// <summary>
        /// Return true if trading is realtime, otherwise return false
        /// </summary>
        /// <param name="barNum">Number current bar</param>
        /// <returns></returns>
        private bool IsRealTime(int barNum)
        {
            int barsCount = Context.BarsCount;
            if (!Context.IsLastBarUsed)
                barsCount--;
            if (barNum < barsCount - 1)
                return false;
            return true;
        }
        /// <summary>
        /// Execute on RealTime
        /// </summary>
        /// <param name="source"></param>
        private void ExecuteRealTime(ISecurity source)
        {
            m_secRt = source as ISecurityRt;
            //OnBuyState(m_secRt);
            //OnSellState(m_secRt);
        }
        /// <summary>
        /// Срабатывает раз в минуту, на границе минутного таймфрейма (что отличается его от обычного минутного таймера)
        /// </summary>
        protected void OnTimer()
        {
            try
            {
                DateTime timeUpdateBegin = DateTime.Now;
                if (m_secRt == null)
                    Context.Log("ISecurityRt is null. Perhaps method 'Excecute' wasn't be called", new Color(), true);
                IReadOnlyList<IDataBar> bars = m_secRt.Bars;
                IDataBar bar = bars[bars.Count - 1];
                TimeSpan delta = DateTime.UtcNow - bar.Date;
                Context.Log($"На момент {DateTime.UtcNow.ToString("HH:mm:ss")} получен бар с временем {bar.Date.ToString("HH:mm:ss")}. Разница {delta}");
                //-- заставляем пересчитаться агента на границе бара
                Context?.Runtime.Recalc(true);
            }
            catch (Exception ex)
            {
                
            }
        }

        /// <summary>
        /// Return trade allowed
        /// </summary>
        /// <param name="secRt"></param>
        /// <returns></returns>
        private bool IsTradeAllowed(ISecurityRt secRt)
        {
            try
            {
                RtSecurityImpl rt = (RtSecurityImpl)secRt;
                bool a = rt.Security.TradePlace.AllowTrade;
                return a;
            }
            catch
            {
                return true;
            }
        }
        
        
        /// <summary>
        /// Buy logic
        /// </summary>
        /// <param name="secRt"></param>
        private void OnBuyState(ISecurityRt secRt)
        {
            
        }
        /// <summary>
        /// Sell logic
        /// </summary>
        /// <param name="secRt"></param>
        private void OnSellState(ISecurityRt secRt)
        {
            
        }
        
        
    }
}
