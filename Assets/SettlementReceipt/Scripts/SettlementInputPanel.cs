using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LogisticsTycoon.Settlement
{
    /// <summary>
    /// 테스트/시연용 정산 데이터 입력 패널.
    /// 입력칸의 값을 <see cref="SettlementData"/>로 만들어 <see cref="SettlementReceiptView.Print"/>에 그대로 넘긴다.
    /// 게임에서는 이 패널 대신 실제 게임 데이터로 SettlementData를 채워 Print를 호출하면 된다.
    /// </summary>
    public class SettlementInputPanel : MonoBehaviour
    {
        public SettlementReceiptView view;

        [Header("Fields")]
        public TMP_InputField dayField;
        public TMP_InputField dateField;
        public TMP_InputField dateTagField;
        public TMP_InputField incomeField;
        public TMP_InputField expenseField;
        public TMP_InputField deliveredField;
        public TMP_InputField returnedField;
        [Tooltip("비워두면 수입 - 지출로 자동 계산")] public TMP_InputField netField;
        public Button weatherButton;
        public TMP_Text weatherButtonLabel;
        public TMP_InputField weatherEffectField;
        public TMP_InputField eventField;

        [Header("Buttons")]
        public Button printButton;
        public Button tearButton;
        public Button normalPresetButton;
        public Button holidayPresetButton;
        public Button muteButton;
        public TMP_Text muteLabel;

        [Header("Printer Hood Buttons (optional)")]
        public Button feedButton;
        public Button cutButton;

        [Header("Behaviour")]
        public bool printOnStart = true;

        private SettlementWeather weather = SettlementWeather.Rain;
        private bool muted;

        private void Start()
        {
            Bind(printButton, PrintFromFields);
            Bind(feedButton, PrintFromFields);
            Bind(tearButton, Tear);
            Bind(cutButton, Tear);
            Bind(normalPresetButton, () => Fill(SettlementData.NormalDayPreset()));
            Bind(holidayPresetButton, () => Fill(SettlementData.HolidayPreset()));
            Bind(weatherButton, CycleWeather);
            Bind(muteButton, ToggleMute);

            Fill(SettlementData.NormalDayPreset());
            if (printOnStart && view != null) Invoke(nameof(PrintFromFields), 0.4f);
        }

        private static void Bind(Button b, UnityEngine.Events.UnityAction a)
        {
            if (b != null) b.onClick.AddListener(a);
        }

        public void PrintFromFields()
        {
            if (view != null) view.Print(ReadFields());
        }

        public void Tear()
        {
            if (view != null) view.TearOff();
        }

        public void Fill(SettlementData d)
        {
            Set(dayField, d.day.ToString(CultureInfo.InvariantCulture));
            Set(dateField, d.date);
            Set(dateTagField, d.dateTag);
            Set(incomeField, d.income.ToString(CultureInfo.InvariantCulture));
            Set(expenseField, d.expense.ToString(CultureInfo.InvariantCulture));
            Set(deliveredField, d.delivered.ToString(CultureInfo.InvariantCulture));
            Set(returnedField, d.returned.ToString(CultureInfo.InvariantCulture));
            Set(netField, d.autoNetProfit ? "" : d.netProfit.ToString(CultureInfo.InvariantCulture));
            weather = d.weather;
            RefreshWeatherLabel();
            Set(weatherEffectField, d.weatherEffect);
            Set(eventField, d.eventNote);
        }

        public SettlementData ReadFields()
        {
            var d = new SettlementData
            {
                day = (int)ParseLong(dayField, 1),
                date = Get(dateField),
                dateTag = Get(dateTagField),
                income = ParseLong(incomeField, 0),
                expense = ParseLong(expenseField, 0),
                delivered = (int)ParseLong(deliveredField, 0),
                returned = (int)ParseLong(returnedField, 0),
                weather = weather,
                weatherEffect = Get(weatherEffectField),
                eventNote = Get(eventField)
            };

            string net = Get(netField);
            d.autoNetProfit = string.IsNullOrWhiteSpace(net);
            if (!d.autoNetProfit) d.netProfit = ParseLong(netField, 0);
            return d;
        }

        private void CycleWeather()
        {
            string oldDefault = SettlementFormat.DefaultWeatherEffect(weather);
            int count = Enum.GetValues(typeof(SettlementWeather)).Length;
            weather = (SettlementWeather)(((int)weather + 1) % count);
            RefreshWeatherLabel();

            // 효과 문구를 직접 고치지 않았다면 새 날씨의 기본 문구로 교체
            if (weatherEffectField != null && (string.IsNullOrWhiteSpace(weatherEffectField.text) || weatherEffectField.text == oldDefault))
                weatherEffectField.text = SettlementFormat.DefaultWeatherEffect(weather);
        }

        private void RefreshWeatherLabel()
        {
            if (weatherButtonLabel != null) weatherButtonLabel.text = $"◀  {SettlementFormat.WeatherName(weather)}  ▶";
        }

        private void ToggleMute()
        {
            muted = !muted;
            if (view != null) view.SetMuted(muted);
            if (muteLabel != null) muteLabel.text = muted ? "사운드 OFF" : "사운드 ON";
        }

        private static void Set(TMP_InputField f, string v)
        {
            if (f != null) f.text = v ?? "";
        }

        private static string Get(TMP_InputField f) => f != null ? f.text : "";

        private static long ParseLong(TMP_InputField f, long fallback)
        {
            string s = Get(f);
            if (string.IsNullOrWhiteSpace(s)) return fallback;
            s = s.Replace(",", "").Replace(" ", "").Replace("G", "").Replace("건", "");
            return long.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long v) ? v : fallback;
        }
    }
}
