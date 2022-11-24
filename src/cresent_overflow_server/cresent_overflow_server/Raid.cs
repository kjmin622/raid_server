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
        private string send_data_str;

        private DateTime one_second_counter;
        bool one_second;

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

        private Dictionary<string,int> player_idx;
        private Dictionary<string,int> enemy_idx;

        private int enemy_creation_idx;
        private int enemy_id_idx;
        private int enemy_cnt;

        // 상수 모음집
        private Constant constant = new Constant();
        
        // 일반몬스터 행동 관련 변수들
        //// enemy_id : recent_time
        private Dictionary<string,DateTime> enemy_recent_attack;
        private Dictionary<string, DateTime> enemy_recent_skill_attack;

        // 보스 관련 변수들
        private DateTime less_minute5_boss_appear_time;
        private bool less_minute5_boss_appear;
        private bool boss_appear;
        private int boss_hp;
        private DateTime boss_recent_attack;
        private DateTime boss_recent_skill_1;
        private DateTime boss_recent_skill_2;
        private DateTime boss_recent_skill_3;
        public TimeSpan BOSS_ATTACK_DELAY = new TimeSpan(0,0,0,1,500);
        public TimeSpan BOSS_SKILL1_DELAY = new TimeSpan(0,0,12);
        public TimeSpan BOSS_SKILL2_DELAY = new TimeSpan(0,0,15);
        public TimeSpan BOSS_SKILL3_DELAY = new TimeSpan(0,0,15);
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

            rand = new Random();

            // 일반 몬스터 행동 관련 변수들
            enemy_recent_attack = new Dictionary<string, DateTime>();
            enemy_recent_skill_attack = new Dictionary<string, DateTime>();

            // 보스 관련 변수들
            less_minute5_boss_appear_time = server_start_time;
            less_minute5_boss_appear = true;
            boss_hp = Constant.BOSS_MAXHP;
            boss_appear = false;
            boss_recent_attack = Utility.Today();
        }


        public async void Start()
        {

            // test player
            /*
            players_info[0] = new PlayerInfo { 
                character_id = "testchar",
                client_id = "testclient",
                hp = 2000,
                status_ailment_id = new List<string>(),
                status_ailment_time = new List<string>(),
                total_deal = 0,
                total_heal = 0
            };
            player_idx["testclient"] = 0;
            */

            while (true)
            {
                Thread.Sleep(50);
                // 1초 지났는지 확인
                if (TimeSpan.Compare(Utility.Today() - one_second_counter, TimeSpan.FromSeconds(1)) == 1)
                {
                    one_second_counter = Utility.Today();
                    one_second = true;
                }
                else
                {
                    one_second = false;
                }

                // 클라이언트로부터 정보 받아오기
                {
                recv_data_str = "";
                send_data_str = "";

                for (int i = 0; i < Constant.MAXIMUM; i++) 
                {
                    if (clients[i] != null)
                    {
                        Reader(streams[i]);
                    }
                }
                }

                // 받아온 정보 처리하기
                {
                if(recv_data_str != "")
                { 
                    Dictionary<string,List<string>> classname_json = Funcs.TranslateAString(recv_data_str);
                    ApplyDict(classname_json);
                    // 몬스터에게 데미지 적용시키기
                    ApplyDamageToEnemy();
                    // 플레이어에게 힐 적용시키기
                    ApplyHealToPlayer();
                    // 상태이상 적용시키기
                    ApplyStatusAilment();
                }
                }
                
                // 플레이어 처리
                //// 딜링, 힐적용, 상태이상 적용은 위에서 했음
                //// 상태이상 시간 확인, 끝난 상태이상 제거
                CheckAilmentTime();

                // 보스몬스터 행동 처리
                {
                // 스턴걸리면 행동 x
                if (enemys_info[0] != null && enemys_info[0].enemy_sid == "OVER1" && !enemys_info[0].status_ailment_id.Contains("DF003"))
                {
                    //일반공격
                    BossAttack();
                    //스킬 1
                    BossSkill1();
                    //스킬 2
                    BossSkill2();
                    //스킬 3
                    BossSkill3();

                }else
                { 
                    boss_recent_skill_1 = Utility.Today();
                    boss_recent_skill_2 = Utility.Today();
                    boss_recent_skill_3 = Utility.Today();
                }
                }
                // 일반몬스터 처리 (생성, 공격 등)
                //// 5분간 일반몬스터 자동 리스폰, 5분 후부터 보스 등장
                { 
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
                    AddEnemys(18);
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
                }

                //// 일반몬스터 행동 처리
                {
                for(int i=0; i<Constant.MAXENEMY; i++)
                {
                    // 스턴걸리면 행동 x
                    if (enemys_info[i]!=null && enemys_info[i].enemy_sid != "OVER1" && !enemys_info[i].status_ailment_id.Contains("DF003"))
                    {
                        int enemy_idx = constant.ENEMY_NAME_TO_IDX[enemys_info[i].enemy_sid];
                        // 일반공격
                        // 마비 상태이상 걸리면 공속 감소
                        TimeSpan plustime = enemys_info[i].status_ailment_id.Contains("DF001") ? TimeSpan.FromSeconds(0) : TimeSpan.FromSeconds(constant.ENEMY_ATTACK_DELAY[enemy_idx].Seconds * 0.2);
                        if (TimeSpan.Compare(Utility.Today() - enemy_recent_attack[enemys_info[i].enemy_id], constant.ENEMY_ATTACK_DELAY[enemy_idx] + plustime) == 1)
                        {
                            enemy_recent_attack[enemys_info[i].enemy_id] = Utility.Today();
                            EnemyAttack(enemys_info[i].enemy_id, RandomPlayerPickFromShort(), constant.ENEMY_ATTACK_DAMAGE[enemy_idx], constant.ENEMY_MAGIC_DAMAGE[enemy_idx]);
                        }
                        // 힐러일 경우 힐하기
                        if (enemys_info[i].enemy_sid == "M1010" && TimeSpan.Compare(Utility.Today() - enemy_recent_skill_attack[enemys_info[i].enemy_id], constant.ENEMY_SKILL_DELAY[enemy_idx]) == 1)
                        {
                                enemy_recent_skill_attack[enemys_info[i].enemy_id] = Utility.Today();
                                EnemyUseSkill(enemys_info[i].enemy_id, enemys_info[i].enemy_id, "HEAL", false);
                                for (int j = 0; j < Constant.MAXENEMY; j++)
                                {
                                    if (enemys_info[i] != null)
                                    {
                                        EnemyHeal(enemys_info[i].enemy_id, Constant.HEALER_HEAL_VALUE);
                                    }
                                }
                        }

                    }
                }
                }

                //// 상태이상 처리
                if (one_second)
                {
                    for (int i = 0; i < Constant.MAXIMUM; i++)
                    {
                        if (players_info[i] != null)
                        {
                            ApplyStatusAilmentDamage(players_info[i].client_id, true);
                        }
                    }
                    for (int i = 0; i < Constant.MAXENEMY; i++)
                    {
                        if (enemys_info[i] != null)
                        {
                            ApplyStatusAilmentDamage(enemys_info[i].enemy_id, false);
                        }
                    }

                }


                // 게임 종료 연산
                {
                //// 보스 처치
                if(BossDie())
                {
                    // 클리어 성공 처리
                    string send_clear_str = GameClearSendStr();
                    for(int i=0; i<Constant.MAXIMUM; i++)
                    {
                        if (streams[i] != null)
                        {
                            Console.WriteLine(send_clear_str);
                            Funcs.SendByteArray(streams[i], Funcs.StringToByteArray(send_clear_str));
                        }
                    }
                    Thread.Sleep(10000);
                    return;
                }
                
                //// 모든 플레이어 사망
                if(AllPlayerDie())
                {
                    // 클리어 실패 처리
                    for(int i=0; i<Constant.MAXIMUM; i++)
                    {
                        if (streams[i] != null)
                        {
                            Funcs.SendByteArray(streams[i], Funcs.StringToByteArray("fail"));
                        }
                    }
                    Thread.Sleep(10000);
                    return;
                }

                }

                // 플레이어에게 정보 보내주기
                {
                //// 플레이어 정보 담기
                AddSendDatas(players_info);
                //// 적 정보 담기
                AddSendDatas(enemys_info);
                //// 플레이어 공격 및 스킬 정보 담기
                AddSendPlayerSkillAndAttackInfo();
                //// 적 공격 및 스킬 정보 담기
                AddSendEnemySkillAndAttackInfo();
                //// 모든 플레이어에게 보내기
                for(int i=0; i<Constant.MAXIMUM; i++)
                {
                    if (players_info[i] != null && streams[i] != null)
                    {
                        Funcs.SendByteArray(streams[i], Funcs.StringToByteArray(send_data_str));
                    }
                }
                Console.WriteLine(send_data_str+"\n");
                }

                //test
                //for (int i = 0; i < 30; i++)
                //{
                //    if (enemys_info[i] != null)
                //    {
                //        ApplyDamage(enemys_info[i].enemy_id, 100, false);
                //    }
                //}
            }
        }

        // stream으로부터 온 데이터 받아서 recv_data_str에 저장하기
        private void Reader(NetworkStream stream)
        {
            try
            {
                recv_data_str += Funcs.PacketToString(stream, 4096);
            }
            catch 
            {
                ;
            }
        }

        // 배열 직렬화해서 send_data_str에 넣기
        private void AddSendDatas<T>(T[] data)
        {
            send_data_str += data.GetType().Name;
            for(int i=0; i<data.Length; i++)
            {
                if (data[i] != null) 
                { 
                    send_data_str += "&"+Funcs.DataToString(data[i]);
                }   
            }
            send_data_str += "$";

        }

        // 플레이어 공격 및 스킬 사용 정보 큐에 있는 데이터 직렬화해서 send_data_str에 넣기
        private void AddSendPlayerSkillAndAttackInfo()
        { 
            send_data_str += "PlayerUseSkillInfo[]";
            while(player_use_skill_info_queue.Count != 0)
            { 
                send_data_str += "&"+player_use_skill_info_queue.Dequeue();
            }                                                           
            send_data_str += "$";
            send_data_str += "PlayerAttackInfo[]";
            while(player_attack_info_queue.Count != 0)
            { 
                send_data_str += "&"+player_attack_info_queue.Dequeue();
            }
            send_data_str += "$";
        }

        // 몬스터 공격 및 스킬 사용 정보 큐에 있는 데이터 직렬화해서 send_data_str에 넣기
        private void AddSendEnemySkillAndAttackInfo()
        { 
            send_data_str += "EnemyUseSkillInfo[]";
            while(enemy_use_skill_info_queue.Count != 0)
            { 
                send_data_str += "&"+Funcs.DataToString(enemy_use_skill_info_queue.Dequeue());
            }
            send_data_str += "$";
            send_data_str += "EnemyAttackInfo[]";
            while(enemy_attack_info_queue.Count != 0)
            { 
                send_data_str += "&"+Funcs.DataToString(enemy_attack_info_queue.Dequeue());
            }
            send_data_str += "$";
        }

        // clients_info[]에 있던 정보 player_info[]로 변환
        private PlayerInfo[] MakePlayerInfo()
        { 
            PlayerInfo[] infos = new PlayerInfo[Constant.MAXIMUM];
            for(int i=0; i<Constant.MAXIMUM; i++)
            {
                if (clients[i]!=null)
                {
                    infos[i] = new PlayerInfo { client_id = clients_info[i].client_id, character_id = clients_info[i].character_id,
                                                        hp = clients_info[i].hp, phsysical_defense = clients_info[i].phsysical_defense,
                                                        magic_defense = clients_info[i].magic_defense,
                                                        status_ailment_id = new List<string>(), status_ailment_time = new List<string>(),
                                                        total_deal = 0, total_heal = 0
                    };
                    player_idx.Add(clients_info[i].client_id,i);
                }
            }
            return infos;
        }
        
        // 딕셔너리에 저장한 받은 직렬화 데이터 역직렬화해서 각각의 큐에 넣기
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


        private void RemoveEnemy(string enemy_id)
        {
            enemys_info[enemy_idx[enemy_id]] = null;
            enemy_idx.Remove(enemy_id);
            enemy_recent_attack.Remove(enemy_id);
            enemy_recent_skill_attack.Remove(enemy_id);
            enemy_cnt--;

        }
        private void ApplyDamage(string target_id, int value, bool to_player)
        {
            if (to_player)
            {
                players_info[player_idx[target_id]].hp = Convert.ToInt32(Math.Max(0, players_info[player_idx[target_id]].hp - value));
            }
            else
            {
                if (enemy_idx.ContainsKey(target_id))
                {
                    enemys_info[enemy_idx[target_id]].hp = Convert.ToInt32(Math.Max(0, enemys_info[enemy_idx[target_id]].hp - value));
                    // 보스 hp는 따로 저장
                    if (enemys_info[enemy_idx[target_id]].enemy_sid == "OVER1")
                    {
                        boss_hp = enemys_info[enemy_idx[target_id]].hp;
                    }
                    if (enemys_info[enemy_idx[target_id]].hp == 0)
                    {
                        RemoveEnemy(target_id);
                    }
                }
            }
        }
        
        // 클라이언트가 준 데이터가 들어간 player_damage_info_queue의 데미지 정보 적용
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
                    ApplyDamage(enemyid, damage, false);
                }
            }
            return return_value;
        }
        
        // 클라이언트가 준 데이터가 들어간 player_heal_info_queue의 상태이상 정보 적용
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
       
        // 클라이언트가 준 데이터가 들어간 inflict_status_ailment_info_queue의 상태이상 정보 적용
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
        
        // 몬스터 생성
        private void AddEnemy(string enemy_fix_id, int max_hp)
        {
            if (enemy_cnt < Constant.MAXENEMY) 
            { 
                enemys_info[enemy_creation_idx] = new EnemyInfo{enemy_id = enemy_id_idx.ToString(), enemy_sid = enemy_fix_id, max_hp = max_hp, hp = max_hp , status_ailment_id = new List<string>(), status_ailment_time = new List<string>()};
                enemy_idx.Add(enemy_id_idx.ToString(), enemy_creation_idx);
                enemy_recent_attack.Add(enemy_id_idx.ToString(), Utility.Today());
                enemy_recent_skill_attack.Add(enemy_id_idx.ToString(), Utility.Today());
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
       
        // 일반몬스터 cnt마리 생성
        private void AddEnemys(int cnt) 
        { 
            int add_enemy_cnt = cnt/2;
            for(int i=0; i<add_enemy_cnt; i++) 
            {
                int short_idx = constant.ENEMY_SHORT_IDX[rand.Next(0, 5)];
                int long_idx = constant.ENEMY_LONG_IDX[rand.Next(0, 5)];
                AddEnemy(constant.ENEMY_IDX_TO_NAME[short_idx], constant.ENEMY_MAXHP[short_idx]);
                AddEnemy(constant.ENEMY_IDX_TO_NAME[long_idx], constant.ENEMY_MAXHP[long_idx]);
            }
            if(add_enemy_cnt%2==1)
            {
                int short_idx = constant.ENEMY_SHORT_IDX[rand.Next(0, 5)];
                AddEnemy(constant.ENEMY_IDX_TO_NAME[short_idx], constant.ENEMY_MAXHP[short_idx]);
            }
        }
        
        // 상태이상이 끝났으면 제거
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
       
        // 모든 플레이어가 죽었으면 true
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
       
        // 보스가 죽었으면 true
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

        // 게임 클리어 시 클라이언트에 보내줄 문자열 반환
        private String GameClearSendStr()
        {
            string[] dealking = DealKing();
            string healking = HealKing();
            string sendstr = "clear";
            
            // dealking
            sendstr += "$dealking";
            if (dealking[0]!="") sendstr += "&" + dealking[0];
            if (dealking[1]!="") sendstr += "&" + dealking[1];
            if (dealking[2]!="") sendstr += "&" + dealking[2];

            // healking
            sendstr += "$healking";
            if (healking != "") sendstr += "&" + healking;

            return sendstr;
        }

        // 살아있는 플레이어 중 아무나 한명
        private String RandomPlayerPick()
        {
            int idx = 0;
            string[] tmparr = new string[Constant.MAXIMUM];
            for(int i=0; i<Constant.MAXIMUM; i++)
            {
                if (players_info[i] != null && players_info[i].hp > 0)
                {
                    tmparr[idx++] = players_info[i].client_id;
                }
            }
            if (idx==0) return "";
            return tmparr[rand.Next(0,idx)];
        }

        // 근거리부터 한명 고르기 
        private String RandomPlayerPickFromShort()
        {
            int shortidx = 0, longidx = 0;
            string[] shortarr = new string[Constant.MAXIMUM];
            string[] longarr = new string[Constant.MAXIMUM];
            for (int i = 0; i < Constant.MAXIMUM; i++)
            {
                if (players_info[i] != null)
                {
                    if (Array.IndexOf(constant.SHORT_CHARACTER, players_info[i].character_id) != -1)
                    {
                        shortarr[shortidx++] = players_info[i].client_id;
                    }
                    else
                    {
                        longarr[longidx++] = players_info[i].client_id;
                    }
                }
            }
            if (shortidx > 0)
            {
                return shortarr[rand.Next(0, shortidx)];
            }
            else
            {
                return longarr[rand.Next(0, longidx)];
            }
        }

        // 플레이어에게 물리피해 입히기 (최소 40%)
        private void PlayerApplyDamage(string client_id, int damage)
        {
            double defense_reduce = players_info[player_idx[client_id]].status_ailment_id.Contains("DF004") ? 0.5 : 1;
            int applydamage = Convert.ToInt32(Math.Max(damage * 0.4, damage - players_info[player_idx[client_id]].phsysical_defense * defense_reduce));
            players_info[player_idx[client_id]].hp = Convert.ToInt32(Math.Max(0, players_info[player_idx[client_id]].hp-applydamage));
        }

        // 플레이어에게 마법피해 입히기 (최소 60%)
        private void PlayerApplyMagicDamage(string client_id, int damage)
        {
            double defense_reduce = players_info[player_idx[client_id]].status_ailment_id.Contains("DF004") ? 0.5 : 1;
            int applydamage = Convert.ToInt32(Math.Max(damage * 0.6, damage - players_info[player_idx[client_id]].magic_defense * defense_reduce));
            players_info[player_idx[client_id]].hp = Convert.ToInt32(Math.Max(0, players_info[player_idx[client_id]].hp - applydamage));
        }

        // 몬스터 공통 행동
        //// 몬스터 일반 공격 결과 적용 후 send queue에 넣기
        private void EnemyAttack(string enemy_id, string target_client_id, int phsydamage, int magicdamage)
        { 
            if(target_client_id == "") return;

            double damage_reduce = enemys_info[enemy_idx[enemy_id]].status_ailment_id.Contains("DF005") ? 0.7 : 1;

            PlayerApplyDamage(target_client_id, Convert.ToInt32(phsydamage*damage_reduce));
            PlayerApplyMagicDamage(target_client_id, Convert.ToInt32(magicdamage * damage_reduce));
            enemy_attack_info_queue.Enqueue(new EnemyAttackInfo { 
                client_id = target_client_id,
                enemy_id = enemy_id
            });
        }

        // 힐
        private void EnemyHeal(string target_id, int value)
        {
            if (target_id == "") return;
            enemys_info[enemy_idx[target_id]].hp = Convert.ToInt32(Math.Min(enemys_info[enemy_idx[target_id]].max_hp, enemys_info[enemy_idx[target_id]].hp + value));
        }

        private void EnemyUseSkill(string enemy_id, string target_id, string skill_id, bool to_player)
        {
            enemy_use_skill_info_queue.Enqueue(new EnemyUseSkillInfo
            {
                enemy_id = enemy_id,
                skill_id = skill_id,
                to_player = to_player,
                target_id = target_id
            });
        }

        // 몬스터가 거는 상태이상 또는 버프
        private void EnemyUseSkillStatusAilments(string target_id, string ailment_id, DateTime time, bool to_player)
        {
            // 플레이어일 일 때 
            if (to_player)
            {
                if (target_id != "" && player_idx.ContainsKey(target_id))
                {
                    // 이미 상태이상이 걸려있으면 더 긴 시간으로 설정
                    if (players_info[player_idx[target_id]].status_ailment_id.Contains(ailment_id))
                    {
                        int idx = players_info[player_idx[target_id]].status_ailment_id.FindIndex(a => a.Contains(ailment_id));
                        DateTime now_ailment_time = DateTime.Parse(players_info[player_idx[target_id]].status_ailment_time[idx]);
                        if (DateTime.Compare(time, now_ailment_time) > 0)
                        {
                            players_info[player_idx[target_id]].status_ailment_time[idx] = time.ToString();
                        }
                    }
                    // 상태이상이 없었다면 걸어주기
                    else
                    {
                        players_info[player_idx[target_id]].status_ailment_id.Add(ailment_id);
                        players_info[player_idx[target_id]].status_ailment_time.Add(time.ToString());
                    }
                }
            }
            else
            {
                if (target_id != "" && enemy_idx.ContainsKey(target_id))
                {
                    // 이미 상태이상이 걸려있으면 더 긴 시간으로 설정
                    if (enemys_info[enemy_idx[target_id]].status_ailment_id.Contains(ailment_id))
                    {
                        int idx = enemys_info[enemy_idx[target_id]].status_ailment_id.FindIndex(a => a.Contains(ailment_id));
                        DateTime now_ailment_time = DateTime.Parse(enemys_info[enemy_idx[target_id]].status_ailment_time[idx]);
                        if (DateTime.Compare(time, now_ailment_time) > 0)
                        {
                            enemys_info[enemy_idx[target_id]].status_ailment_time[idx] = time.ToString();
                        }
                    }
                    // 상태이상이 없었다면 걸어주기
                    else
                    {
                        enemys_info[enemy_idx[target_id]].status_ailment_id.Add(ailment_id);
                        enemys_info[enemy_idx[target_id]].status_ailment_time.Add(time.ToString());
                    }
                }
            }
        }

        // 보스 행동
        //// 일반공격 후 결과 적용, send queue에 넣기
        private void BossAttack()
        {
            // 마비 상태이상 걸리면 공속 감소
            TimeSpan plustime = enemys_info[0].status_ailment_id.Contains("DF001") ? TimeSpan.FromSeconds(0) : TimeSpan.FromSeconds(BOSS_ATTACK_DELAY.Seconds*0.2);
            if(TimeSpan.Compare(Utility.Today() - boss_recent_attack, BOSS_ATTACK_DELAY + plustime) == 1)
            { 
                boss_recent_attack = Utility.Today();
                EnemyAttack(enemys_info[0].enemy_id, RandomPlayerPickFromShort(), Constant.BOSS_ATTACK_DAMAGE, Constant.BOSS_MAGIC_DAMAGE);
            }
        }

        private void BossSkill1()
        {
            if (TimeSpan.Compare(Utility.Today() - boss_recent_skill_1, BOSS_SKILL1_DELAY) == 1)
            {
                boss_recent_skill_1 = Utility.Today();
                string target_player = RandomPlayerPickFromShort();
                EnemyUseSkill(enemys_info[0].enemy_id, target_player, "OVER1_SKILL1", true);
                PlayerApplyMagicDamage(target_player, Constant.BOSS_SKILL1_DAMAGE);
            }
        }

        private void BossSkill2()
        {
            if (TimeSpan.Compare(Utility.Today() - boss_recent_skill_2, BOSS_SKILL2_DELAY) == 1)
            {
                boss_recent_skill_2 = Utility.Today();
                EnemyUseSkill(enemys_info[0].enemy_id, enemys_info[0].enemy_id, "OVER1_SKILL2", true);
                int cmd = rand.Next(0, 3);
                string ailment = cmd == 0 ? "DF002" : (cmd == 1 ? "DF004" : "DF005");
                
                for (int i = 0; i < Constant.MAXIMUM; i++)
                {
                    if (players_info[i] != null)
                    {
                        EnemyUseSkillStatusAilments(players_info[i].client_id, ailment, Utility.Today().AddSeconds(2), true);
                    }
                }
                
            }
        }

        private void BossSkill3()
        {
            if (enemy_cnt > 1)
            {
                boss_recent_skill_3 = Utility.Today();
            }
            if (TimeSpan.Compare(Utility.Today() - boss_recent_skill_3, BOSS_SKILL3_DELAY) == 1)
            {
                boss_recent_skill_3 = Utility.Today();
                EnemyUseSkill(enemys_info[0].enemy_id, enemys_info[0].enemy_id, "OVER1_SKILL3", false);
                AddEnemys(15);
            }
        }

        // 마비, 화상, 중독
        private void ApplyStatusAilmentDamage(string target_id, bool to_player)
        {
            if (to_player)
            {
                if (players_info[player_idx[target_id]].status_ailment_id.Contains("DF001")) ApplyDamage(target_id, 3, true);
                if (players_info[player_idx[target_id]].status_ailment_id.Contains("DF002")) ApplyDamage(target_id, 6, true);
                if (players_info[player_idx[target_id]].status_ailment_id.Contains("DF006")) ApplyDamage(target_id, 3, true);
            }
            else
            {
                if (enemys_info[enemy_idx[target_id]].status_ailment_id.Contains("DF001")) ApplyDamage(target_id, 3, false);
                if (enemys_info[enemy_idx[target_id]].status_ailment_id.Contains("DF002")) ApplyDamage(target_id, 6, false);
                if (enemys_info[enemy_idx[target_id]].status_ailment_id.Contains("DF006")) ApplyDamage(target_id, 3, false);
            }
        }
    }
}
