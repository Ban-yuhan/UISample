using System;
using System.Globalization;
using UnityEngine;

namespace LogisticsTycoon.Settlement
{
    public enum SettlementWeather
    {
        Sunny,
        Cloudy,
        Rain,
        Snow
    }

    /// <summary>
    /// 일일 정산 영수증에 출력될 원본 데이터.
    /// 게임에서는 하루 결산 결과를 이 객체에 채워 <see cref="SettlementReceiptView.Print"/>에 넘기면 그대로 출력된다.
    /// 금액/건수는 "원시 숫자"로만 받고, 부호·천단위 콤마·단위(G/건) 포맷은 출력기가 담당한다.
    /// </summary>
    [Serializable]
    public class SettlementData
    {
        [Header("날짜")]
        [Tooltip("DAY n")] public int day = 14;
        [Tooltip("yyyy.MM.dd 형식이면 요일(THU 등)이 자동으로 붙는다. 다른 형식이면 그대로 출력.")]
        public string date = "2026.11.12";
        [Tooltip("요일 옆에 붙는 태그 (예: 명절 대목). 비우면 생략.")]
        public string dateTag = "";

        [Header("정산 수치 (원시 값)")]
        [Tooltip("수입 (G). 항상 + 로 출력")] public long income = 485000;
        [Tooltip("지출 (G). 양수로 입력해도 - 로 출력")] public long expense = 112000;
        [Tooltip("배송 택배 수 (건)")] public int delivered = 142;
        [Tooltip("반품 택배 수 (건)")] public int returned = 4;

        [Tooltip("true면 순수익 = 수입 - 지출 자동 계산, false면 netProfit 값을 그대로 출력")]
        public bool autoNetProfit = true;
        public long netProfit;

        [Header("익일 예보")]
        public SettlementWeather weather = SettlementWeather.Rain;
        [Tooltip("날씨 효과 (예: 이속 -10%)")] public string weatherEffect = "이속 -10%";
        [Tooltip("특이사항 본문 ('특이사항: ' 접두어는 자동)")]
        public string eventNote = "명절 대량 물류 예고! (익일 물량 +180%)";

        public long ResolvedNetProfit => autoNetProfit ? income - Math.Abs(expense) : netProfit;

        public SettlementData Clone() => (SettlementData)MemberwiseClone();

        public static SettlementData NormalDayPreset() => new SettlementData();

        public static SettlementData HolidayPreset() => new SettlementData
        {
            day = 15,
            date = "2026.11.13",
            dateTag = "명절 대목",
            income = 1290000,
            expense = 245000,
            delivered = 386,
            returned = 11,
            autoNetProfit = true,
            weather = SettlementWeather.Sunny,
            weatherEffect = "보너스 +5%",
            eventNote = "명절 직후 반품 집중 주간 (검수 주의)"
        };
    }

    /// <summary>영수증 표기 포맷 유틸</summary>
    public static class SettlementFormat
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Number(long v) => Math.Abs(v).ToString("N0", Inv);

        public static string Income(long v) => $"+{Number(v)} G";

        public static string Expense(long v) => v == 0 ? "0 G" : $"-{Number(v)} G";

        public static string Signed(long v) => v > 0 ? $"+{Number(v)} G" : v < 0 ? $"-{Number(v)} G" : "0 G";

        public static string Count(int v) => $"{v.ToString("N0", Inv)} 건";

        public static string DateLine(SettlementData d)
        {
            string datePart = string.IsNullOrWhiteSpace(d.date) ? "" : d.date.Trim();
            string inside = null;

            if (DateTime.TryParseExact(datePart, new[] { "yyyy.MM.dd", "yyyy-MM-dd", "yyyy/MM/dd" }, Inv,
                    DateTimeStyles.None, out DateTime parsed))
            {
                datePart = parsed.ToString("yyyy.MM.dd", Inv);
                inside = parsed.ToString("ddd", Inv).ToUpperInvariant();
            }

            if (!string.IsNullOrWhiteSpace(d.dateTag))
                inside = string.IsNullOrEmpty(inside) ? d.dateTag.Trim() : $"{inside} · {d.dateTag.Trim()}";

            string line = $"DAY {d.day}";
            if (!string.IsNullOrEmpty(datePart)) line += $" · {datePart}";
            if (!string.IsNullOrEmpty(inside)) line += $" ({inside})";
            return line;
        }

        public static string WeatherName(SettlementWeather w)
        {
            switch (w)
            {
                case SettlementWeather.Sunny: return "맑음";
                case SettlementWeather.Cloudy: return "흐림";
                case SettlementWeather.Rain: return "비";
                case SettlementWeather.Snow: return "눈";
                default: return w.ToString();
            }
        }

        public static string DefaultWeatherEffect(SettlementWeather w)
        {
            switch (w)
            {
                case SettlementWeather.Sunny: return "보너스 +5%";
                case SettlementWeather.Cloudy: return "변동 없음";
                case SettlementWeather.Rain: return "이속 -10%";
                case SettlementWeather.Snow: return "이속 -20%";
                default: return "";
            }
        }

        public static string WeatherLine(SettlementData d)
        {
            string name = WeatherName(d.weather);
            return string.IsNullOrWhiteSpace(d.weatherEffect) ? name : $"{name} ({d.weatherEffect.Trim()})";
        }

        public static string EventLine(SettlementData d)
        {
            return string.IsNullOrWhiteSpace(d.eventNote) ? "특이사항: 없음" : $"특이사항: {d.eventNote.Trim()}";
        }
    }
}
