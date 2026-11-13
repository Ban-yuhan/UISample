using System;
using System.Collections.Generic;
using UnityEngine;

namespace LogisticsTycoon.ClipBoard
{
    [Serializable]
    public struct WorkerStats
    {
        public int carry, speed, work;
        public WorkerStats(int c, int s, int w) { carry = c; speed = s; work = w; }
    }

    [Serializable]
    public class CandidateData
    {
        public string id, name, trait;
        public string avatarBg, avatarFg;
        public WorkerStats stats;
        public int wage;
        public bool hired;
        public Vector2 stampPos;   // 이력서 카드 안에 찍힌 도장 위치
        public float stampRot;
        public CandidateData Clone() => (CandidateData)MemberwiseClone();
    }

    [Serializable]
    public class StaffData
    {
        public string id, name, trait;
        public bool isNew;
        public string zone = "waiting"; // waiting / conveyor / loading / packing
        public WorkerStats stats;
        public int stress;
        public int wage;
        public StaffData Clone() => (StaffData)MemberwiseClone();
    }

    [Serializable]
    public class ContractData
    {
        public string id, client, category, cycle;
        public int dailyBoxes, revenuePerBox, trust;
        public bool signed;
        public int DailyTotal => dailyBoxes * revenuePerBox;
        public ContractData Clone() => (ContractData)MemberwiseClone();
    }

    [Serializable]
    public class UpgradeData
    {
        public string id, title, desc, glyph;
        public bool staffGroup;
        public int level, maxLevel, cost;
        public string statValue, nextValue, bonusText;
        public UpgradeData Clone() => (UpgradeData)MemberwiseClone();
    }

    /// <summary>HTML 프로토타입의 초기 스냅샷 (시연 초기화 시 깊은 복사로 원복)</summary>
    public static class ClipboardSeed
    {
        public const long InitialGold = 124500;
        public const int InitialReputation = 3;
        public const long PiggyFullGold = 200000;

        public static List<CandidateData> Candidates() => new List<CandidateData>
        {
            new CandidateData { id = "c1", name = "김철수", trait = "성실한 모범생", avatarBg = "#fef3c7", avatarFg = "#92400e", stats = new WorkerStats(6, 8, 8), wage = 10500 },
            new CandidateData { id = "c2", name = "박영희", trait = "무쇠체력 헬창", avatarBg = "#ffe4e6", avatarFg = "#9f1239", stats = new WorkerStats(10, 6, 7), wage = 12000 },
            new CandidateData { id = "c3", name = "최민수", trait = "축지법 라이더", avatarBg = "#e0f2fe", avatarFg = "#075985", stats = new WorkerStats(5, 10, 9), wage = 13500 },
        };

        public static List<StaffData> Staff() => new List<StaffData>
        {
            new StaffData { id = "s_init_1", name = "이동현", trait = "베테랑 조장", zone = "waiting", stats = new WorkerStats(7, 8, 9), stress = 24, wage = 11500 },
            new StaffData { id = "s_init_2", name = "한지민", trait = "꼼꼼한 검수원", zone = "conveyor", stats = new WorkerStats(6, 7, 9), stress = 68, wage = 11000 },
        };

        public static List<ContractData> Contracts() => new List<ContractData>
        {
            new ContractData { id = "con1", client = "냥냥 펫푸드", category = "반려동물 사료/간식", dailyBoxes = 120, revenuePerBox = 450, trust = 88, cycle = "매일 오전 09:00 입고" },
            new ContractData { id = "con2", client = "쿠마 택배유통", category = "소형 가전 및 공산품", dailyBoxes = 250, revenuePerBox = 620, trust = 76, cycle = "매일 오전 10:30 입고" },
            new ContractData { id = "con3", client = "프레시 마켓", category = "신선 과일 및 냉장 화물", dailyBoxes = 80, revenuePerBox = 890, trust = 94, cycle = "매일 새벽 06:00 냉장입고" },
        };

        public static List<UpgradeData> Upgrades() => new List<UpgradeData>
        {
            new UpgradeData { id = "p_carry", title = "양손 적재량", desc = "한 번에 들어올릴 수 있는 최대 택배 박스 수", glyph = "箱", level = 1, maxLevel = 10, cost = 8000, statValue = "2박스", nextValue = "3박스", bonusText = "+1 상자" },
            new UpgradeData { id = "p_speed", title = "이동속도", desc = "물류센터 내부를 뛰어다니는 기본 이동 속도", glyph = "走", level = 1, maxLevel = 10, cost = 10000, statValue = "100%", nextValue = "115%", bonusText = "+15% 이속" },
            new UpgradeData { id = "p_work", title = "작업속도", desc = "컨베이어 분류 및 포장 테이핑 처리 속도", glyph = "速", level = 1, maxLevel = 10, cost = 12000, statValue = "100%", nextValue = "120%", bonusText = "+20% 작업" },
            new UpgradeData { id = "s_carry", staffGroup = true, title = "알바 적재량", desc = "모든 알바 직원의 기본 운반 적재량 증가", glyph = "箱", level = 1, maxLevel = 5, cost = 20000, statValue = "+0박스", nextValue = "+1박스", bonusText = "전원 +1 상자" },
            new UpgradeData { id = "s_speed", staffGroup = true, title = "알바 이동속도", desc = "직원들의 보행 속도 향상 및 피로 누적 감소", glyph = "走", level = 1, maxLevel = 5, cost = 15000, statValue = "100%", nextValue = "110%", bonusText = "전원 +10% 이속" },
            new UpgradeData { id = "s_work", staffGroup = true, title = "알바 작업속도", desc = "분류기·상차장·포장대 작업 처리 숙련도 상승", glyph = "速", level = 1, maxLevel = 5, cost = 18000, statValue = "100%", nextValue = "115%", bonusText = "전원 +15% 작업" },
        };
    }
}
