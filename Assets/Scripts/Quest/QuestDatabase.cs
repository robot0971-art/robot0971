using System;
using System.Collections.Generic;
using UnityEngine;

namespace SunnysideIsland.Quest
{
    /// <summary>
    /// 퀘스트 목표 타입
    /// </summary>
    public enum ObjectiveType
    {
        Collect,      // 아이템 수집
        Deliver,      // 아이템 전달
        Talk,         // NPC 대화
        Defeat,       // 적 처치
        Build,        // 건물 건설
        Visit,        // 장소 방문
        Survive,      // 생존 (일수)
        Farm,         // 농사 (심기/수확)
        Fish,         // 낚시
        Gather,       // 채집
        Cook,         // 요리
        Craft,        // 제작
        Sell,         // 판매
        Buy,          // 구매
        Hire,         // 고용
        Attract       // 관광객 유치
    }

    /// <summary>
    /// 퀘스트 목표
    /// </summary>
    [Serializable]
    public class QuestObjective
    {
        public ObjectiveType Type;
        public string TargetId;      // 아이템ID, NPC ID, 적ID, 건물ID 등
        public int RequiredAmount;
        public string Description;
    }

    /// <summary>
    /// 퀘스트 보상
    /// </summary>
    [Serializable]
    public class QuestReward
    {
        public int Gold;
        public List<string> ItemIds = new List<string>();
        public List<int> ItemAmounts = new List<int>();
        public int Experience;
        public string UnlocksBuilding;
        public string UnlocksArea;
    }

    /// <summary>
    /// 상세 퀘스트 데이터
    /// </summary>
    [Serializable]
    public class DetailedQuestData
    {
        public string QuestId;
        public string Title;
        public string Description;
        public ChapterType Chapter;
        public bool IsMainQuest;
        public QuestObjective[] Objectives;
        public QuestReward Reward;
        public string[] Prerequisites;
        public string NextQuestId;
        public int MinDay;
        public int MaxDay;
    }

    /// <summary>
    /// 모든 퀘스트 데이터를 관리하는 데이터베이스
    /// </summary>
    [CreateAssetMenu(fileName = "QuestDatabase", menuName = "SunnysideIsland/Quest/Database")]
    public class QuestDatabase : ScriptableObject
    {
        [Header("=== All Quests ===")]
        [SerializeField] private List<DetailedQuestData> _quests = new List<DetailedQuestData>();

        public List<DetailedQuestData> GetAllQuests() => _quests;

        public DetailedQuestData GetQuest(string questId)
        {
            foreach (var quest in _quests)
            {
                if (quest.QuestId == questId)
                    return quest;
            }
            return null;
        }

        public List<DetailedQuestData> GetChapterQuests(ChapterType chapter)
        {
            var result = new List<DetailedQuestData>();
            foreach (var quest in _quests)
            {
                if (quest.Chapter == chapter)
                    result.Add(quest);
            }
            return result;
        }

        public List<DetailedQuestData> GetMainQuests(ChapterType chapter)
        {
            var result = new List<DetailedQuestData>();
            foreach (var quest in _quests)
            {
                if (quest.Chapter == chapter && quest.IsMainQuest)
                    result.Add(quest);
            }
            return result;
        }

        public List<DetailedQuestData> GetSubQuests(ChapterType chapter)
        {
            var result = new List<DetailedQuestData>();
            foreach (var quest in _quests)
            {
                if (quest.Chapter == chapter && !quest.IsMainQuest)
                    result.Add(quest);
            }
            return result;
        }

        #region Static Quest Data Generation

        public static List<DetailedQuestData> GenerateAllQuests()
        {
            var quests = new List<DetailedQuestData>();

            // ========== Chapter 1: 생존의 시작 (Day 1-3) ==========
            quests.AddRange(GenerateChapter1Quests());

            // ========== Chapter 2: 섬 개척 (Day 4-7) ==========
            quests.AddRange(GenerateChapter2Quests());

            // ========== Chapter 3: 마을 건설 (Day 8-14) ==========
            quests.AddRange(GenerateChapter3Quests());

            // ========== Chapter 4: 관광 도시 완성 (Day 15-28) ==========
            quests.AddRange(GenerateChapter4Quests());

            return quests;
        }

        private static List<DetailedQuestData> GenerateChapter1Quests()
        {
            var quests = new List<DetailedQuestData>();

            // C1_Q1: 첫걸음
            quests.Add(new DetailedQuestData
            {
                QuestId = "C1_Q1",
                Title = "첫걸음",
                Description = "낯선 섬에서 눈을 떴다. 일단 주변을 둘러보자.",
                Chapter = ChapterType.Chapter1,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Visit, TargetId = "Beach", RequiredAmount = 1, Description = "해변 탐색" },
                    new QuestObjective { Type = ObjectiveType.Visit, TargetId = "Forest", RequiredAmount = 1, Description = "숲 탐색" }
                },
                Reward = new QuestReward { Gold = 50, Experience = 10 },
                NextQuestId = "C1_Q2",
                MinDay = 1,
                MaxDay = 1
            });

            // C1_Q2: 생존의 기본
            quests.Add(new DetailedQuestData
            {
                QuestId = "C1_Q2",
                Title = "생존의 기본",
                Description = "배가 고프다. 무언가 먹을 것을 찾아야 한다.",
                Chapter = ChapterType.Chapter1,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Gather, TargetId = "Herb", RequiredAmount = 3, Description = "야생 약초 채집 (3개)" },
                    new QuestObjective { Type = ObjectiveType.Fish, TargetId = "Any", RequiredAmount = 1, Description = "물고기 낚시 (1마리)" }
                },
                Reward = new QuestReward { Gold = 100, Experience = 20 },
                Prerequisites = new[] { "C1_Q1" },
                NextQuestId = "C1_Q3",
                MinDay = 1,
                MaxDay = 2
            });

            // C1_Q3: 임시 거처
            quests.Add(new DetailedQuestData
            {
                QuestId = "C1_Q3",
                Title = "임시 거처",
                Description = "밤이 위험하다. 임시로라도 거처가 필요하다.",
                Chapter = ChapterType.Chapter1,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Gather, TargetId = "Wood", RequiredAmount = 10, Description = "나무 채집 (10개)" },
                    new QuestObjective { Type = ObjectiveType.Gather, TargetId = "Stone", RequiredAmount = 5, Description = "돌 채집 (5개)" },
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "Tent", RequiredAmount = 1, Description = "텐트 건설" }
                },
                Reward = new QuestReward { Gold = 150, Experience = 30, UnlocksBuilding = "Tent" },
                Prerequisites = new[] { "C1_Q2" },
                NextQuestId = "C1_Q4",
                MinDay = 1,
                MaxDay = 2
            });

            // C1_Q4: 도구 만들기
            quests.Add(new DetailedQuestData
            {
                QuestId = "C1_Q4",
                Title = "도구 만들기",
                Description = "맨손으로는 힘들다. 기본 도구를 만들자.",
                Chapter = ChapterType.Chapter1,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Craft, TargetId = "Axe", RequiredAmount = 1, Description = "도끼 제작" },
                    new QuestObjective { Type = ObjectiveType.Craft, TargetId = "Pickaxe", RequiredAmount = 1, Description = "곡괭이 제작" }
                },
                Reward = new QuestReward { Gold = 100, Experience = 25 },
                Prerequisites = new[] { "C1_Q3" },
                NextQuestId = "C1_Q5",
                MinDay = 2,
                MaxDay = 3
            });

            // C1_Q5: 첫 번째 밤
            quests.Add(new DetailedQuestData
            {
                QuestId = "C1_Q5",
                Title = "첫 번째 밤",
                Description = "섬에서 첫 밤을 무사히 보내자.",
                Chapter = ChapterType.Chapter1,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Survive, TargetId = "Days", RequiredAmount = 1, Description = "하루 생존" },
                    new QuestObjective { Type = ObjectiveType.Cook, TargetId = "Any", RequiredAmount = 1, Description = "요리하기 (1회)" }
                },
                Reward = new QuestReward { Gold = 200, Experience = 50 },
                Prerequisites = new[] { "C1_Q4" },
                NextQuestId = "C2_Q1",
                MinDay = 2,
                MaxDay = 3
            });

            // Sub Quests
            quests.Add(new DetailedQuestData
            {
                QuestId = "C1_SQ1",
                Title = "해변의 보물",
                Description = "해변에서 유용한 것들을 찾아보자.",
                Chapter = ChapterType.Chapter1,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Gather, TargetId = "Seashell", RequiredAmount = 5, Description = "조개 채집 (5개)" },
                    new QuestObjective { Type = ObjectiveType.Gather, TargetId = "Driftwood", RequiredAmount = 3, Description = "표류목 채집 (3개)" }
                },
                Reward = new QuestReward { Gold = 75, ItemIds = new List<string> { "Rope" }, ItemAmounts = new List<int> { 2 } },
                MinDay = 1,
                MaxDay = 3
            });

            quests.Add(new DetailedQuestData
            {
                QuestId = "C1_SQ2",
                Title = "벌레 잡기",
                Description = "벌레를 잡아서 미끼로 쓰자.",
                Chapter = ChapterType.Chapter1,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Gather, TargetId = "Worm", RequiredAmount = 10, Description = "지렁이 채집 (10마리)" }
                },
                Reward = new QuestReward { Gold = 30 },
                MinDay = 1,
                MaxDay = 3
            });

            quests.Add(new DetailedQuestData
            {
                QuestId = "C1_SQ3",
                Title = "약초 수집가",
                Description = "다양한 약초를 수집하자.",
                Chapter = ChapterType.Chapter1,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Gather, TargetId = "Herb_Red", RequiredAmount = 3, Description = "빨간 약초 (3개)" },
                    new QuestObjective { Type = ObjectiveType.Gather, TargetId = "Herb_Blue", RequiredAmount = 3, Description = "파란 약초 (3개)" }
                },
                Reward = new QuestReward { Gold = 100, ItemIds = new List<string> { "Potion_Small" }, ItemAmounts = new List<int> { 2 } },
                MinDay = 1,
                MaxDay = 3
            });

            return quests;
        }

        private static List<DetailedQuestData> GenerateChapter2Quests()
        {
            var quests = new List<DetailedQuestData>();

            // C2_Q1: 농사 시작
            quests.Add(new DetailedQuestData
            {
                QuestId = "C2_Q1",
                Title = "농사 시작",
                Description = "안정적인 식량 공급을 위해 농사를 시작하자.",
                Chapter = ChapterType.Chapter2,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "FarmPlot", RequiredAmount = 4, Description = "밭 건설 (4칸)" },
                    new QuestObjective { Type = ObjectiveType.Farm, TargetId = "Potato", RequiredAmount = 4, Description = "감자 심기 (4개)" },
                    new QuestObjective { Type = ObjectiveType.Farm, TargetId = "Any", RequiredAmount = 4, Description = "작물 수확 (4개)" }
                },
                Reward = new QuestReward { Gold = 300, Experience = 50, ItemIds = new List<string> { "Seed_Carrot" }, ItemAmounts = new List<int> { 5 } },
                Prerequisites = new[] { "C1_Q5" },
                NextQuestId = "C2_Q2",
                MinDay = 4,
                MaxDay = 5
            });

            // C2_Q2: 더 나은 집
            quests.Add(new DetailedQuestData
            {
                QuestId = "C2_Q2",
                Title = "더 나은 집",
                Description = "텐트는 춥다. 제대로 된 집을 짓자.",
                Chapter = ChapterType.Chapter2,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Gather, TargetId = "Wood", RequiredAmount = 30, Description = "나무 채집 (30개)" },
                    new QuestObjective { Type = ObjectiveType.Gather, TargetId = "Stone", RequiredAmount = 20, Description = "돌 채집 (20개)" },
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "Hut", RequiredAmount = 1, Description = "오두막 건설" }
                },
                Reward = new QuestReward { Gold = 500, Experience = 75, UnlocksBuilding = "Hut" },
                Prerequisites = new[] { "C2_Q1" },
                NextQuestId = "C2_Q3",
                MinDay = 4,
                MaxDay = 6
            });

            // C2_Q3: 고블린의 습격
            quests.Add(new DetailedQuestData
            {
                QuestId = "C2_Q3",
                Title = "고블린의 습격",
                Description = "밤마다 고블린이 나타난다. 무기를 만들어 대비하자.",
                Chapter = ChapterType.Chapter2,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Craft, TargetId = "Sword_Wood", RequiredAmount = 1, Description = "나무 검 제작" },
                    new QuestObjective { Type = ObjectiveType.Defeat, TargetId = "Goblin", RequiredAmount = 5, Description = "고블린 처치 (5마리)" }
                },
                Reward = new QuestReward { Gold = 400, Experience = 100, ItemIds = new List<string> { "Sword_Iron" }, ItemAmounts = new List<int> { 1 } },
                Prerequisites = new[] { "C2_Q2" },
                NextQuestId = "C2_Q4",
                MinDay = 5,
                MaxDay = 7
            });

            // C2_Q4: 저장고 건설
            quests.Add(new DetailedQuestData
            {
                QuestId = "C2_Q4",
                Title = "저장고 건설",
                Description = "아이템이 늘어났다. 저장고가 필요하다.",
                Chapter = ChapterType.Chapter2,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "Storage", RequiredAmount = 1, Description = "저장고 건설" },
                    new QuestObjective { Type = ObjectiveType.Gather, TargetId = "Any", RequiredAmount = 50, Description = "자원 비축 (50개)" }
                },
                Reward = new QuestReward { Gold = 300, Experience = 50 },
                Prerequisites = new[] { "C2_Q3" },
                NextQuestId = "C2_Q5",
                MinDay = 6,
                MaxDay = 7
            });

            // C2_Q5: 첫 번째 상점
            quests.Add(new DetailedQuestData
            {
                QuestId = "C2_Q5",
                Title = "첫 번째 상점",
                Description = "여유 자원을 판매할 상점을 열자.",
                Chapter = ChapterType.Chapter2,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "Stall", RequiredAmount = 1, Description = "노점 건설" },
                    new QuestObjective { Type = ObjectiveType.Sell, TargetId = "Any", RequiredAmount = 10, Description = "아이템 판매 (10개)" }
                },
                Reward = new QuestReward { Gold = 500, Experience = 75, UnlocksBuilding = "Stall" },
                Prerequisites = new[] { "C2_Q4" },
                NextQuestId = "C3_Q1",
                MinDay = 7,
                MaxDay = 7
            });

            // Sub Quests
            quests.Add(new DetailedQuestData
            {
                QuestId = "C2_SQ1",
                Title = "낚시꾼",
                Description = "낚시로 식량을 확보하자.",
                Chapter = ChapterType.Chapter2,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Fish, TargetId = "Any", RequiredAmount = 10, Description = "물고기 낚시 (10마리)" }
                },
                Reward = new QuestReward { Gold = 150 },
                MinDay = 4,
                MaxDay = 7
            });

            quests.Add(new DetailedQuestData
            {
                QuestId = "C2_SQ2",
                Title = "광산 탐사",
                Description = "동굴에서 광물을 찾아보자.",
                Chapter = ChapterType.Chapter2,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Visit, TargetId = "Mine", RequiredAmount = 1, Description = "광산 발견" },
                    new QuestObjective { Type = ObjectiveType.Gather, TargetId = "Iron", RequiredAmount = 5, Description = "철광석 채굴 (5개)" }
                },
                Reward = new QuestReward { Gold = 200 },
                MinDay = 4,
                MaxDay = 7
            });

            quests.Add(new DetailedQuestData
            {
                QuestId = "C2_SQ3",
                Title = "요리사",
                Description = "다양한 요리를 만들어보자.",
                Chapter = ChapterType.Chapter2,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Cook, TargetId = "Fish_Stew", RequiredAmount = 1, Description = "생선 스튜 요리" },
                    new QuestObjective { Type = ObjectiveType.Cook, TargetId = "Roasted_Potato", RequiredAmount = 1, Description = "구운 감자 요리" }
                },
                Reward = new QuestReward { Gold = 100 },
                MinDay = 4,
                MaxDay = 7
            });

            quests.Add(new DetailedQuestData
            {
                QuestId = "C2_SQ4",
                Title = "숲의 수호자",
                Description = "숲을 위협하는 몬스터를 처치하자.",
                Chapter = ChapterType.Chapter2,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Defeat, TargetId = "Goblin", RequiredAmount = 10, Description = "고블린 처치 (10마리)" },
                    new QuestObjective { Type = ObjectiveType.Defeat, TargetId = "Wolf", RequiredAmount = 3, Description = "늑대 처치 (3마리)" }
                },
                Reward = new QuestReward { Gold = 300, ItemIds = new List<string> { "Armor_Leather" }, ItemAmounts = new List<int> { 1 } },
                MinDay = 4,
                MaxDay = 7
            });

            return quests;
        }

        private static List<DetailedQuestData> GenerateChapter3Quests()
        {
            var quests = new List<DetailedQuestData>();

            // C3_Q1: 첫 주민
            quests.Add(new DetailedQuestData
            {
                QuestId = "C3_Q1",
                Title = "첫 주민",
                Description = "여행자가 섬에 정착하고 싶어한다.",
                Chapter = ChapterType.Chapter3,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Talk, TargetId = "Traveler", RequiredAmount = 1, Description = "여행자와 대화" },
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "House", RequiredAmount = 1, Description = "집 건설" },
                    new QuestObjective { Type = ObjectiveType.Hire, TargetId = "Resident", RequiredAmount = 1, Description = "주민 고용" }
                },
                Reward = new QuestReward { Gold = 500, Experience = 100 },
                Prerequisites = new[] { "C2_Q5" },
                NextQuestId = "C3_Q2",
                MinDay = 8,
                MaxDay = 10
            });

            // C3_Q2: 마을 광장
            quests.Add(new DetailedQuestData
            {
                QuestId = "C3_Q2",
                Title = "마을 광장",
                Description = "주민들이 모일 광장을 만들자.",
                Chapter = ChapterType.Chapter3,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "Plaza", RequiredAmount = 1, Description = "광장 건설" },
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "Bench", RequiredAmount = 3, Description = "벤치 설치 (3개)" }
                },
                Reward = new QuestReward { Gold = 600, Experience = 75 },
                Prerequisites = new[] { "C3_Q1" },
                NextQuestId = "C3_Q3",
                MinDay = 9,
                MaxDay = 11
            });

            // C3_Q3: 상업 지구
            quests.Add(new DetailedQuestData
            {
                QuestId = "C3_Q3",
                Title = "상업 지구",
                Description = "마을에 상점들을 더 지어 활성화하자.",
                Chapter = ChapterType.Chapter3,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "GroceryStore", RequiredAmount = 1, Description = "식료품점 건설" },
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "Blacksmith", RequiredAmount = 1, Description = "대장간 건설" },
                    new QuestObjective { Type = ObjectiveType.Sell, TargetId = "Any", RequiredAmount = 50, Description = "총 판매량 50개 달성" }
                },
                Reward = new QuestReward { Gold = 1000, Experience = 150 },
                Prerequisites = new[] { "C3_Q2" },
                NextQuestId = "C3_Q4",
                MinDay = 10,
                MaxDay = 12
            });

            // C3_Q4: 식당 건설
            quests.Add(new DetailedQuestData
            {
                QuestId = "C3_Q4",
                Title = "식당 건설",
                Description = "주민과 여행자를 위한 식당을 짓자.",
                Chapter = ChapterType.Chapter3,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "Restaurant", RequiredAmount = 1, Description = "식당 건설" },
                    new QuestObjective { Type = ObjectiveType.Cook, TargetId = "Any", RequiredAmount = 20, Description = "요리 제공 (20회)" }
                },
                Reward = new QuestReward { Gold = 800, Experience = 100 },
                Prerequisites = new[] { "C3_Q3" },
                NextQuestId = "C3_Q5",
                MinDay = 11,
                MaxDay = 13
            });

            // C3_Q5: 여관 개업
            quests.Add(new DetailedQuestData
            {
                QuestId = "C3_Q5",
                Title = "여관 개업",
                Description = "여행자들이 머물 수 있는 여관을 짓자.",
                Chapter = ChapterType.Chapter3,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "Inn", RequiredAmount = 1, Description = "여관 건설" },
                    new QuestObjective { Type = ObjectiveType.Hire, TargetId = "Resident", RequiredAmount = 3, Description = "추가 주민 고용 (3명)" }
                },
                Reward = new QuestReward { Gold = 1000, Experience = 150 },
                Prerequisites = new[] { "C3_Q4" },
                NextQuestId = "C3_Q6",
                MinDay = 12,
                MaxDay = 14
            });

            // C3_Q6: 마을 방어
            quests.Add(new DetailedQuestData
            {
                QuestId = "C3_Q6",
                Title = "마을 방어",
                Description = "고블린의 공격이 거세지고 있다. 마을을 방어하자.",
                Chapter = ChapterType.Chapter3,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "Watchtower", RequiredAmount = 2, Description = "감시탑 건설 (2개)" },
                    new QuestObjective { Type = ObjectiveType.Defeat, TargetId = "Goblin", RequiredAmount = 20, Description = "고블린 처치 (20마리)" },
                    new QuestObjective { Type = ObjectiveType.Survive, TargetId = "Days", RequiredAmount = 3, Description = "3일간 마을 방어" }
                },
                Reward = new QuestReward { Gold = 1500, Experience = 200, ItemIds = new List<string> { "Armor_Iron" }, ItemAmounts = new List<int> { 1 } },
                Prerequisites = new[] { "C3_Q5" },
                NextQuestId = "C4_Q1",
                MinDay = 13,
                MaxDay = 14
            });

            // Sub Quests
            quests.Add(new DetailedQuestData
            {
                QuestId = "C3_SQ1",
                Title = "농장 확장",
                Description = "농장 규모를 늘리자.",
                Chapter = ChapterType.Chapter3,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "FarmPlot", RequiredAmount = 8, Description = "밭 추가 건설 (8칸)" },
                    new QuestObjective { Type = ObjectiveType.Farm, TargetId = "Any", RequiredAmount = 20, Description = "작물 수확 (20개)" }
                },
                Reward = new QuestReward { Gold = 300 },
                MinDay = 8,
                MaxDay = 14
            });

            quests.Add(new DetailedQuestData
            {
                QuestId = "C3_SQ2",
                Title = "낚시 대회",
                Description = "큰 물고기를 낚아보자.",
                Chapter = ChapterType.Chapter3,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Fish, TargetId = "Big_Fish", RequiredAmount = 3, Description = "대형 물고기 낚시 (3마리)" }
                },
                Reward = new QuestReward { Gold = 400, ItemIds = new List<string> { "FishingRod_Iron" }, ItemAmounts = new List<int> { 1 } },
                MinDay = 8,
                MaxDay = 14
            });

            quests.Add(new DetailedQuestData
            {
                QuestId = "C3_SQ3",
                Title = "희귀 약초",
                Description = "동굴 깊은 곳에서 희귀 약초를 찾자.",
                Chapter = ChapterType.Chapter3,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Gather, TargetId = "Herb_Purple", RequiredAmount = 5, Description = "보라 약초 (5개)" },
                    new QuestObjective { Type = ObjectiveType.Gather, TargetId = "Herb_Gold", RequiredAmount = 2, Description = "황금 약초 (2개)" }
                },
                Reward = new QuestReward { Gold = 500, ItemIds = new List<string> { "Potion_Large" }, ItemAmounts = new List<int> { 3 } },
                MinDay = 8,
                MaxDay = 14
            });

            quests.Add(new DetailedQuestData
            {
                QuestId = "C3_SQ4",
                Title = "장인의 도구",
                Description = "더 좋은 도구를 만들자.",
                Chapter = ChapterType.Chapter3,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Craft, TargetId = "Axe_Iron", RequiredAmount = 1, Description = "철제 도끼 제작" },
                    new QuestObjective { Type = ObjectiveType.Craft, TargetId = "Pickaxe_Iron", RequiredAmount = 1, Description = "철제 곡괭이 제작" }
                },
                Reward = new QuestReward { Gold = 200 },
                MinDay = 8,
                MaxDay = 14
            });

            quests.Add(new DetailedQuestData
            {
                QuestId = "C3_SQ5",
                Title = "시장 개설",
                Description = "정기 시장을 열자.",
                Chapter = ChapterType.Chapter3,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "Market", RequiredAmount = 1, Description = "시장 건설" },
                    new QuestObjective { Type = ObjectiveType.Sell, TargetId = "Any", RequiredAmount = 100, Description = "누적 판매 100개" }
                },
                Reward = new QuestReward { Gold = 800 },
                MinDay = 8,
                MaxDay = 14
            });

            return quests;
        }

        private static List<DetailedQuestData> GenerateChapter4Quests()
        {
            var quests = new List<DetailedQuestData>();

            // C4_Q1: 관광의 시작
            quests.Add(new DetailedQuestData
            {
                QuestId = "C4_Q1",
                Title = "관광의 시작",
                Description = "섬의 아름다움을 널리 알리자.",
                Chapter = ChapterType.Chapter4,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "Dock", RequiredAmount = 1, Description = "부두 건설" },
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "Lighthouse", RequiredAmount = 1, Description = "등대 건설" },
                    new QuestObjective { Type = ObjectiveType.Attract, TargetId = "Tourist", RequiredAmount = 5, Description = "관광객 5명 유치" }
                },
                Reward = new QuestReward { Gold = 2000, Experience = 200 },
                Prerequisites = new[] { "C3_Q6" },
                NextQuestId = "C4_Q2",
                MinDay = 15,
                MaxDay = 18
            });

            // C4_Q2: 관광 명소
            quests.Add(new DetailedQuestData
            {
                QuestId = "C4_Q2",
                Title = "관광 명소",
                Description = "관광객을 위한 시설을 확충하자.",
                Chapter = ChapterType.Chapter4,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "Park", RequiredAmount = 1, Description = "공원 건설" },
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "FestivalGround", RequiredAmount = 1, Description = "축제장 건설" },
                    new QuestObjective { Type = ObjectiveType.Attract, TargetId = "Tourist", RequiredAmount = 20, Description = "관광객 20명 유치" }
                },
                Reward = new QuestReward { Gold = 2500, Experience = 250 },
                Prerequisites = new[] { "C4_Q1" },
                NextQuestId = "C4_Q3",
                MinDay = 16,
                MaxDay = 20
            });

            // C4_Q3: 온천 개발
            quests.Add(new DetailedQuestData
            {
                QuestId = "C4_Q3",
                Title = "온천 개발",
                Description = "섬의 온천을 개발하여 힐링 명소로 만들자.",
                Chapter = ChapterType.Chapter4,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Visit, TargetId = "HotSpring", RequiredAmount = 1, Description = "온천 발견" },
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "HotSpring_Inn", RequiredAmount = 1, Description = "온천 여관 건설" },
                    new QuestObjective { Type = ObjectiveType.Attract, TargetId = "Tourist", RequiredAmount = 40, Description = "관광객 40명 유치" }
                },
                Reward = new QuestReward { Gold = 3000, Experience = 300 },
                Prerequisites = new[] { "C4_Q2" },
                NextQuestId = "C4_Q4",
                MinDay = 18,
                MaxDay = 22
            });

            // C4_Q4: 고블린 위기
            quests.Add(new DetailedQuestData
            {
                QuestId = "C4_Q4",
                Title = "고블린 위기",
                Description = "고블린 족장이 군대를 이끌고 쳐들어온다!",
                Chapter = ChapterType.Chapter4,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Defeat, TargetId = "Goblin", RequiredAmount = 30, Description = "고블린 처치 (30마리)" },
                    new QuestObjective { Type = ObjectiveType.Defeat, TargetId = "Goblin_Chief", RequiredAmount = 1, Description = "고블린 족장 처치" }
                },
                Reward = new QuestReward { Gold = 5000, Experience = 500, ItemIds = new List<string> { "Sword_Legendary" }, ItemAmounts = new List<int> { 1 } },
                Prerequisites = new[] { "C4_Q3" },
                NextQuestId = "C4_Q5",
                MinDay = 20,
                MaxDay = 24
            });

            // C4_Q5: 리조트 호텔
            quests.Add(new DetailedQuestData
            {
                QuestId = "C4_Q5",
                Title = "리조트 호텔",
                Description = "세계적인 휴양지가 되기 위해 리조트 호텔을 건설하자.",
                Chapter = ChapterType.Chapter4,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "Resort_Hotel", RequiredAmount = 1, Description = "리조트 호텔 건설" },
                    new QuestObjective { Type = ObjectiveType.Hire, TargetId = "Resident", RequiredAmount = 10, Description = "총 주민 10명 고용" },
                    new QuestObjective { Type = ObjectiveType.Attract, TargetId = "Tourist", RequiredAmount = 100, Description = "관광객 100명 유치" }
                },
                Reward = new QuestReward { Gold = 10000, Experience = 500 },
                Prerequisites = new[] { "C4_Q4" },
                NextQuestId = "C4_Q6",
                MinDay = 22,
                MaxDay = 26
            });

            // C4_Q6: 섬의 엔딩
            quests.Add(new DetailedQuestData
            {
                QuestId = "C4_Q6",
                Title = "섬의 엔딩",
                Description = "28일 동안의 여정이 마무리된다. 새로운 시작을 위해...",
                Chapter = ChapterType.Chapter4,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Survive, TargetId = "Days", RequiredAmount = 28, Description = "28일 생존 완료" },
                    new QuestObjective { Type = ObjectiveType.Attract, TargetId = "Tourist", RequiredAmount = 150, Description = "총 관광객 150명 유치" },
                    new QuestObjective { Type = ObjectiveType.Sell, TargetId = "Any", RequiredAmount = 500, Description = "총 판매량 500개 달성" }
                },
                Reward = new QuestReward { Gold = 20000, Experience = 1000 },
                Prerequisites = new[] { "C4_Q5" },
                MinDay = 28,
                MaxDay = 28
            });

            // C4_BOSS: 보스 레이드
            quests.Add(new DetailedQuestData
            {
                QuestId = "C4_BOSS",
                Title = "고블린 족장 토벌",
                Description = "마을을 위협하는 고블린 족장을 무찌르자!",
                Chapter = ChapterType.Chapter4,
                IsMainQuest = true,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Visit, TargetId = "Goblin_Cave", RequiredAmount = 1, Description = "고블린 동굴 진입" },
                    new QuestObjective { Type = ObjectiveType.Defeat, TargetId = "Goblin_Chief", RequiredAmount = 1, Description = "고블린 족장 처치" }
                },
                Reward = new QuestReward { Gold = 8000, Experience = 800, ItemIds = new List<string> { "Crown_Goblin" }, ItemAmounts = new List<int> { 1 } },
                Prerequisites = new[] { "C4_Q3" },
                MinDay = 20,
                MaxDay = 27
            });

            // Sub Quests
            quests.Add(new DetailedQuestData
            {
                QuestId = "C4_SQ1",
                Title = "특산품 개발",
                Description = "섬만의 특산품을 만들자.",
                Chapter = ChapterType.Chapter4,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Craft, TargetId = "Special_Souvenir", RequiredAmount = 5, Description = "기념품 제작 (5개)" },
                    new QuestObjective { Type = ObjectiveType.Sell, TargetId = "Special_Souvenir", RequiredAmount = 5, Description = "기념품 판매 (5개)" }
                },
                Reward = new QuestReward { Gold = 500 },
                MinDay = 15,
                MaxDay = 28
            });

            quests.Add(new DetailedQuestData
            {
                QuestId = "C4_SQ2",
                Title = "축제 준비",
                Description = "첫 번째 축제를 준비하자.",
                Chapter = ChapterType.Chapter4,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Cook, TargetId = "Festival_Food", RequiredAmount = 10, Description = "축제 음식 준비 (10개)" },
                    new QuestObjective { Type = ObjectiveType.Craft, TargetId = "Decoration", RequiredAmount = 5, Description = "장식품 제작 (5개)" }
                },
                Reward = new QuestReward { Gold = 600 },
                MinDay = 15,
                MaxDay = 28
            });

            quests.Add(new DetailedQuestData
            {
                QuestId = "C4_SQ3",
                Title = "낚시 토너먼트",
                Description = "관광객을 위한 낚시 대회를 열자.",
                Chapter = ChapterType.Chapter4,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Fish, TargetId = "Trophy_Fish", RequiredAmount = 1, Description = "트로피 물고기 낚시" },
                    new QuestObjective { Type = ObjectiveType.Attract, TargetId = "Tourist_Fisher", RequiredAmount = 5, Description = "낚시꾼 관광객 5명" }
                },
                Reward = new QuestReward { Gold = 800 },
                MinDay = 15,
                MaxDay = 28
            });

            quests.Add(new DetailedQuestData
            {
                QuestId = "C4_SQ4",
                Title = "농장 투어",
                Description = "체험 농장을 운영하자.",
                Chapter = ChapterType.Chapter4,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Build, TargetId = "Farm_Tour", RequiredAmount = 1, Description = "체험 농장 건설" },
                    new QuestObjective { Type = ObjectiveType.Attract, TargetId = "Tourist_Farm", RequiredAmount = 10, Description = "농장 체험객 10명" }
                },
                Reward = new QuestReward { Gold = 500 },
                MinDay = 15,
                MaxDay = 28
            });

            quests.Add(new DetailedQuestData
            {
                QuestId = "C4_SQ5",
                Title = "평판 향상",
                Description = "섬의 평판을 높이자.",
                Chapter = ChapterType.Chapter4,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Sell, TargetId = "Any", RequiredAmount = 200, Description = "누적 판매 200개" },
                    new QuestObjective { Type = ObjectiveType.Attract, TargetId = "Tourist", RequiredAmount = 50, Description = "관광객 50명 유치" }
                },
                Reward = new QuestReward { Gold = 1000 },
                MinDay = 15,
                MaxDay = 28
            });

            quests.Add(new DetailedQuestData
            {
                QuestId = "C4_SQ6",
                Title = "모든 업적",
                Description = "섬의 모든 것을 완성하자.",
                Chapter = ChapterType.Chapter4,
                IsMainQuest = false,
                Objectives = new[]
                {
                    new QuestObjective { Type = ObjectiveType.Gather, TargetId = "Any", RequiredAmount = 1000, Description = "총 채집 1000개" },
                    new QuestObjective { Type = ObjectiveType.Fish, TargetId = "Any", RequiredAmount = 100, Description = "총 낚시 100마리" },
                    new QuestObjective { Type = ObjectiveType.Farm, TargetId = "Any", RequiredAmount = 200, Description = "총 수확 200개" }
                },
                Reward = new QuestReward { Gold = 5000, Experience = 500, ItemIds = new List<string> { "Trophy_Master" }, ItemAmounts = new List<int> { 1 } },
                MinDay = 20,
                MaxDay = 28
            });

            return quests;
        }

        #endregion
    }
}