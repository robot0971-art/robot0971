using System.Collections.Generic;
using UnityEngine;
using SunnysideIsland.Events;

namespace SunnysideIsland.NPC
{
    /// <summary>
    /// NPC 데이터
    /// </summary>
    [System.Serializable]
    public class NPCData
    {
        public string NPCId;
        public string Name;
        public string Dialogue; // 기본 대사
        public List<string> QuestIds; // 관련 퀘스트
        public Vector3 SpawnPosition;
        public bool IsInteractable;
    }

    /// <summary>
    /// NPC 시스템
    /// </summary>
    public class NPCSystem : MonoBehaviour
    {
        [Header("=== Settings ===")]
        [SerializeField] private List<NPCData> _npcs = new List<NPCData>();
        
        private Dictionary<string, NPCData> _npcDatabase = new Dictionary<string, NPCData>();
        
        private void Awake()
        {
            // NPC 데이터베이스 구성
            foreach (var npc in _npcs)
            {
                if (!_npcDatabase.ContainsKey(npc.NPCId))
                {
                    _npcDatabase[npc.NPCId] = npc;
                }
            }
        }
        
        /// <summary>
        /// NPC 대화
        /// </summary>
        public string TalkToNPC(string npcId)
        {
            var npc = GetNPC(npcId);
            if (npc == null) return null;
            
            EventBus.Publish(new NPCTalkEvent
            {
                NPCId = npcId,
                Dialogue = npc.Dialogue
            });
            
            return npc.Dialogue;
        }
        
        /// <summary>
        /// NPC 조회
        /// </summary>
        public NPCData GetNPC(string npcId)
        {
            if (_npcDatabase.ContainsKey(npcId))
            {
                return _npcDatabase[npcId];
            }
            return null;
        }
        
        /// <summary>
        /// 모든 NPC 목록
        /// </summary>
        public List<NPCData> GetAllNPCs()
        {
            return new List<NPCData>(_npcs);
        }
        
        /// <summary>
        /// 퀘스트 관련 NPC 찾기
        /// </summary>
        public List<NPCData> GetNPCsWithQuest(string questId)
        {
            var result = new List<NPCData>();
            foreach (var npc in _npcs)
            {
                if (npc.QuestIds.Contains(questId))
                {
                    result.Add(npc);
                }
            }
            return result;
        }
        
        /// <summary>
        /// NPC 추가
        /// </summary>
        public void AddNPC(NPCData npc)
        {
            if (npc != null && !string.IsNullOrEmpty(npc.NPCId))
            {
                _npcs.Add(npc);
                _npcDatabase[npc.NPCId] = npc;
            }
        }
    }
    
    /// <summary>
    /// NPC 대화 이벤트
    /// </summary>
    public class NPCTalkEvent
    {
        public string NPCId { get; set; }
        public string Dialogue { get; set; }
    }
}
