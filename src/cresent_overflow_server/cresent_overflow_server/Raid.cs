using cresent_overflow_server.packet;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace cresent_overflow_server
{
    public class Raid
    {
        private int port;
        private TcpListener listener;
        private TcpClient[] clients;
        private NetworkStream[] streams;
        private DateTime server_start_time;
        private DateTime raid_start_time;
        private Task<string>[] read_datas;
        private string recv_data_str;

        private ClientInfo[] clients_info;

        private PlayerInfo[] players_info;
        private Queue<string> player_use_skill_info_queue; // 서버에서 따로 처리 없이 클라이언트들에게 그대로 뿌려줄거라 string
        private Queue<string> player_attack_info_queue; // // 서버에서 따로 처리 없이 클라이언트들에게 그대로 뿌려줄거라 string
        private Queue<PlayerDamageInfo> player_damage_info_queue;
        private Queue<PlayerHealInfo> player_heal_info_queue;
        private Queue<InflictStatusAilmentInfo> inflict_status_ailment_info_queue;
        private EnemyInfo[] enemys_info;

        private Queue<EnemyAttackInfo> enemy_attack_info_queue;
        private Queue<EnemyUseSkillInfo> enemy_use_skill_info_queue;
        private Queue<EnemyDamageInfo> enemy_damage_info_queue;
        private Queue<EnemyHealInfo> enemy_heal_info_queue;

        private Dictionary<string,int> player_idx;
        private Dictionary<string,int> enemy_idx;

        private int enemy_creation_idx;
        private int enemy_id_idx;
        private int enemy_cnt;

        private DateTime less_minute5_boss_appear_time;
        private bool less_minute5_boss_appear;
        private bool boss_appear;
        private int boss_hp;
        Random rand;
       
        public Raid(int port, TcpListener listener, TcpClient[] clients, NetworkStream[] streams, ClientInfo[] clients_info, DateTime server_start_time)
        {
            this.port = port;
            this.listener = listener;
            this.clients = clients;
            this.streams = streams;
            this.clients_info = clients_info;
            this.server_start_time = server_start_time;
            this.raid_start_time = Utility.Today();
            this.read_datas = new Task<string>[Constant.MAXIMUM];
            this.enemy_id_idx = 0;
            this.enemy_creation_idx = 0;
            for (int i = 0; i < Constant.MAXIMUM; i++)
            {
                if (clients[i] != null)
                {
                    streams[i].ReadTimeout = 50;
                    streams[i].WriteTimeout = 50;
                }
            }

            enemy_idx = new Dictionary<string, int>();
            player_idx = new Dictionary<string, int>();

            players_info = MakePlayerInfo();
            player_attack_info_queue = new Queue<string>();
            player_use_skill_info_queue = new Queue<string>();
            player_damage_info_queue = new Queue<PlayerDamageInfo>();
            player_heal_info_queue = new Queue<PlayerHealInfo>();
            inflict_status_ailment_info_queue = new Queue<InflictStatusAilmentInfo>();
            enemys_info = new EnemyInfo[Constant.MAXENEMY];
            enemy_attack_info_queue = new Queue<EnemyAttackInfo>();
            enemy_use_skill_info_queue = new Queue<EnemyUseSkillInfo>();
            enemy_damage_info_queue = new Queue<EnemyDamageInfo>();
            enemy_heal_info_queue = new Queue<EnemyHealInfo>();
            rand = new Random();
            less_minute5_boss_appear_time = server_start_time;
            less_minute5_boss_appear = true;
            boss_hp = Constant.BOSS_MAXHP;
            boss_appear = false;
        }


        public async void Start()
        {
            int debug_cnt = 0;
            while (true)
            {
                Thread.Sleep(100);
                // 클라이언트로부터 정보 받아오기
                recv_data_str = "";
                int idx = 0;
                for (int i = 0; i < Constant.MAXIMUM; i++) 
                {
                    if (clients[i] != null)
                    {
                        Reader(streams[i]);
                    }
                }

                bool is_change_damage_to_enemy = false;
                bool is_change_heal_to_player = false;
                bool is_change_status_ailment = false;
                // 받아온 정보 토대로 처리하기
                if(recv_data_str != "")
                { 
                    Console.WriteLine(recv_data_str);
                    Dictionary<string,List<string>> classname_json = Funcs.TranslateAString(recv_data_str);
                    ApplyDict(classname_json);
                    // 몬스터에게 데미지 적용시키기
                    is_change_damage_to_enemy = ApplyDamageToEnemy();
                    // 플레이어에게 힐 적용시키기
                    is_change_heal_to_player = ApplyHealToPlayer();
                    // 상태이상 적용시키기
                    is_change_status_ailment = ApplyStatusAilment();
                }

                // 플레이어 처리
                //// 상태이상 시간 확인, 끝난 상태이상 제거
                CheckAilmentTime();
                
                // 보스몬스터 처리
                

                // 일반몬스터 처리 (생성, 공격 등)
                // 5분간 일반몬스터 자동 리스폰
                if(ElapsedTime().TotalMinutes<5 && enemy_cnt == 0 && (Utility.Today()-less_minute5_boss_appear_time).TotalSeconds > 10 && less_minute5_boss_appear) 
                { 
                    if(less_minute5_boss_appear)
                    { 
                        less_minute5_boss_appear = false;
                        if (enemys_info[0] != null) 
                        { 
                            boss_hp = enemys_info[0].hp;
                        }
                        enemy_creation_idx = 0;
                    }
                    AddEnemys(rand.Next(7,13));
                }
                else if(ElapsedTime().TotalMinutes<5 && enemy_cnt == 0 && !less_minute5_boss_appear)
                { 
                    less_minute5_boss_appear = true;
                    less_minute5_boss_appear_time = Utility.Today();
                    enemy_creation_idx = 0;
                    AddEnemy("OVER1",Constant.BOSS_MAXHP);
                    enemys_info[0].hp = boss_hp;
                    enemy_cnt = 0;
                }
                else if(ElapsedTime().TotalMinutes>=5 && enemy_cnt == 0 && !boss_appear)
                { 
                    boss_appear = true;
                    enemy_creation_idx = 0;
                    AddEnemy("OVER1",Constant.BOSS_MAXHP);
                    enemys_info[0].hp = boss_hp;
                }
                
                // 게임 종료 연산
                //// 보스 처치
                if(BossDie())
                {
                    // 클리어 성공 처리
                    String[] deal_king = DealKing();
                    String heal_king = HealKing();
                    return;
                }
                
                //// 모든 플레이어 사망
                if(AllPlayerDie())
                {
                    // 클리어 실패 처리
                    return;
                }

                // 플레이어에게 정보 보내주기


              
            }
        }

        private void Reader(NetworkStream stream)
        {
            try
            {
                this.recv_data_str += Funcs.PacketToString(stream, 4096);
            }
            catch 
            {
                ;
            }
        }
    
        private PlayerInfo[] MakePlayerInfo()
        { 
            PlayerInfo[] infos = new PlayerInfo[Constant.MAXIMUM];
            for(int i=0; i<Constant.MAXIMUM; i++)
            {
                if (clients[i]!=null)
                {
                    infos[i] = new PlayerInfo { client_id = clients_info[i].client_id, character_id = clients_info[i].character_id,
                                                        hp = clients_info[i].hp, status_ailment_id = new List<string>(), status_ailment_time = new List<string>(),
                                                        total_deal = 0, total_heal = 0
                    };
                    player_idx.Add(clients_info[i].client_id,i);
                }
            }
            return infos;
        }
        
        private void ApplyDict(Dictionary<string,List<string>> classname_json)
        { 
            if(classname_json.ContainsKey("PlayerAttackInfo"))
            { 
                foreach(string json in classname_json["PlayerAttackInfo"])
                { 
                    player_attack_info_queue.Enqueue(json);
                }
            }
            if(classname_json.ContainsKey("PlayerUseSkillInfo"))
            { 
                foreach(string json in classname_json["PlayerUseSkillInfo"])
                {
                    player_use_skill_info_queue.Enqueue(json);
                }
            }
            if(classname_json.ContainsKey("PlayerDamageInfo"))
            { 
                foreach(string json in classname_json["PlayerDamageInfo"])
                { 
                    player_damage_info_queue.Enqueue(JsonSerializer.Deserialize<PlayerDamageInfo>(json));
                }
            }
            if(classname_json.ContainsKey("PlayerHealInfo"))
            { 
                foreach(string json in classname_json["PlayerHealInfo"])
                { 
                    player_heal_info_queue.Enqueue(JsonSerializer.Deserialize<PlayerHealInfo>(json));
                }
            }
            if(classname_json.ContainsKey("InflictStatusAilmentInfo"))
            { 
                foreach(string json in classname_json["InflictStatusAilmentInfo"])
                { 
                    inflict_status_ailment_info_queue.Enqueue(JsonSerializer.Deserialize<InflictStatusAilmentInfo>(json));
                }
            }
        }
        
        // 레이드 진행 경과 시간
        private TimeSpan ElapsedTime() 
        { 
            return Utility.Today() - raid_start_time;
        }
        // 변화 있었으면 true
        private bool ApplyDamageToEnemy()
        {
            bool return_value = false;
            while(player_damage_info_queue.Count != 0)
            { 
                return_value = true;
                PlayerDamageInfo info = player_damage_info_queue.Dequeue();
                string clientid = info.client_id;
                string enemyid = info.enemy_id;
                int damage = info.damage;
                
                if (enemy_idx.ContainsKey(enemyid) && player_idx.ContainsKey(clientid))
                {
                    players_info[player_idx[clientid]].total_deal += damage;
                    enemys_info[enemy_idx[enemyid]].hp -= damage;
                    // 보스 hp는 따로 저장
                    if(enemys_info[enemy_idx[enemyid]].enemy_sid=="OVER1")
                    { 
                        boss_hp = enemys_info[enemy_idx[enemyid]].hp;
                    }
                    // 적 사망
                    if(enemys_info[enemy_idx[enemyid]].hp <= 0)
                    {
                        enemys_info[enemy_idx[enemyid]] = null;
                        enemy_idx.Remove(enemyid);
                        enemy_cnt--;
                    }
                }
            }
            return return_value;
        }
        private bool ApplyHealToPlayer()
        { 
            bool return_value = false;
            while(player_heal_info_queue.Count != 0)
            { 
                return_value = true;
                PlayerHealInfo info = player_heal_info_queue.Dequeue();
                string client_id = info.client_id;
                string target_id = info.target_id;
                int heal = info.heal;

                if(player_idx.ContainsKey(client_id) && player_idx.ContainsKey(target_id) && players_info[player_idx[target_id]].hp!=0)
                {
                    players_info[player_idx[client_id]].total_heal += heal;
                    players_info[player_idx[target_id]].hp = Math.Min(players_info[player_idx[target_id]].hp+heal, clients_info[player_idx[target_id]].hp);
                }
            }
            return return_value;
        }
        private bool ApplyStatusAilment()
        { 
            bool return_value = false;
            while(inflict_status_ailment_info_queue.Count!=0)
            { 
                return_value = true;
                InflictStatusAilmentInfo info = inflict_status_ailment_info_queue.Dequeue();
                string target_id = info.target_id;
                bool is_player = info.is_player;
                string status_ailment_id = info.status_ailment_id;
                string status_ailment_time = info.status_ailment_time;
                if(is_player)
                {
                    if (player_idx.ContainsKey(target_id))
                    {
                        List<string> aid = players_info[player_idx[target_id]].status_ailment_id; //
                        List<string> atime = players_info[player_idx[target_id]].status_ailment_time; //
                        int findidx = aid.FindIndex((string str)=>(str.Equals(status_ailment_id)));
                        if(findidx == -1) 
                        { 
                            aid.Add(status_ailment_id);
                            atime.Add(status_ailment_time);
                        }
                        else
                        {
                            if(DateTime.Compare(DateTime.Parse(atime[findidx]),DateTime.Parse(status_ailment_time))<0)
                            {
                                atime[findidx] = status_ailment_time;
                            }
                        }
                    }
                }
            }
            return return_value;
        }

        private void AddEnemy(string enemy_fix_id, int max_hp)
        {
            if (enemy_cnt < Constant.MAXENEMY) 
            { 
                enemys_info[enemy_creation_idx] = new EnemyInfo{enemy_id = enemy_id_idx.ToString(), enemy_sid = enemy_fix_id, hp = max_hp , status_ailment_id = new List<string>(), status_ailment_time = new List<string>()};
                enemy_idx.Add(enemy_id_idx.ToString(), enemy_creation_idx);

                enemy_id_idx++;
                enemy_cnt++;
                for(int i=0; i<Constant.MAXENEMY; i++)
                {
                    if (enemys_info[i]==null)
                    { 
                        enemy_creation_idx = i;
                        break;
                    }
                }
            }
        }
        private void AddEnemys(int cnt) 
        { 
            int add_enemy_cnt = cnt/2;
            for(int i=0; i<add_enemy_cnt; i++) 
            { 
                AddEnemy("M1001",140);
                AddEnemy("M1004",135);
            }
            if(add_enemy_cnt%2==1)
            { 
                AddEnemy("M1001",140);
            }
            
        }

        private void CheckAilmentTime()
        { 
            // 플레이어
            DateTime now_time = Utility.Today();
            for(int i=0; i<Constant.MAXIMUM; i++)
            {
                if (players_info[i] != null)
                { 
                    for(int j=0; j < players_info[i].status_ailment_time.Count; j++)
                    { 
                        DateTime ailment_time = DateTime.Parse(players_info[i].status_ailment_time[j]);
                        if(ailment_time <= now_time)
                        {
                            players_info[i].status_ailment_id.RemoveAt(j);
                            players_info[i].status_ailment_time.RemoveAt(j);
                            j--;
                        }
                    }
                }
            }
            // 적
            for(int i=0; i<Constant.MAXIMUM; i++)
            {
                if (enemys_info[i] != null)
                { 
                    for(int j=0; j < enemys_info[i].status_ailment_time.Count(); j++)
                    { 
                        DateTime ailment_time = DateTime.Parse(enemys_info[i].status_ailment_time[j]);
                        if(ailment_time <= now_time)
                        {
                            enemys_info[i].status_ailment_id.RemoveAt(j);
                            enemys_info[i].status_ailment_time.RemoveAt(j);
                            j--;
                        }
                    }
                }
            }
        }
    
        private bool AllPlayerDie()
        { 
            for(int i=0; i<Constant.MAXIMUM; i++) 
            {
                if (players_info[i]!=null && players_info[i].hp > 0)
                {
                    return false;
                }
            }
            return true;
        }
        private bool BossDie()
        { 
            return boss_hp<=0;
        }

        // 게임 종료할 때 딜킹 3위까지 클라이언트 아이디 반환, 빈자리는 ""
        private String[] DealKing()
        { 
            String[] king = new String[3];
            king[0] = king[1] = king[2]= "";
            int k1,k2,k3;
            k1=k2=k3=0;
            for(int i=0; i<Constant.MAXIMUM; i++) 
            {
                if (players_info[i]!=null)
                {
                    if (players_info[i].total_deal > k1)
                    {
                        king[1] = king[0];
                        king[2] = king[1];
                        king[0] = players_info[i].client_id;
                        k2=k1;
                        k3=k2;
                        k1=players_info[i].total_deal;
                    }
                    else if (players_info[i].total_deal > k2)
                    {
                        king[2] = king[1];
                        king[1] = players_info[i].client_id;
                        k3=k2;
                        k2=players_info[i].total_deal;
                    }
                    else if (players_info[i].total_deal > k3)
                    {
                        king[2] = players_info[i].client_id;
                        k3=players_info[i].total_deal;
                    }
                }
            }
            return king;
        }
        // 게임 종료할 때 힐킹 1위 클라이언트 아이디 반환, 힐킹 없으면 "" 반환
        private String HealKing()
        { 
            String king=""; int heal=1;
            for(int i=0; i<Constant.MAXIMUM; i++)
            {
                if (players_info[i]!=null && players_info[i].total_heal > heal)
                { 
                    king = players_info[i].client_id;
                }
            }
            return king;
        }

    }
}
