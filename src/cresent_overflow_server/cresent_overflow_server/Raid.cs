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
        private EnemyInfo[] enemy_infos;

        private Queue<EnemyAttackInfo> enemy_attack_info_queue;
        private Queue<EnemyUseSkillInfo> enemy_use_skill_info_queue;
        private Queue<EnemyDamageInfo> enemy_damage_info_queue;
        private Queue<EnemyHealInfo> enemy_heal_info_queue;

        private Dictionary<string,int> player_idx;
        private Dictionary<string,int> enemy_idx;

        private int enemy_creation_idx;
        private int enemy_id_idx;

        public Raid(int port, TcpListener listener, TcpClient[] clients, NetworkStream[] streams, ClientInfo[] clients_info, DateTime server_start_time)
        {
            this.port = port;
            this.listener = listener;
            this.clients = clients;
            this.streams = streams;
            this.clients_info = clients_info;
            this.server_start_time = server_start_time;
            this.raid_start_time = DateTime.Now;
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
            enemy_infos = new EnemyInfo[Constant.MAXIMUM];
            enemy_attack_info_queue = new Queue<EnemyAttackInfo>();
            enemy_use_skill_info_queue = new Queue<EnemyUseSkillInfo>();
            enemy_damage_info_queue = new Queue<EnemyDamageInfo>();
            enemy_heal_info_queue = new Queue<EnemyHealInfo>();

            
            // test
            AddEnemy("testenemy",300);
        }


        public async void Start()
        {
            while (true)
            {
                players_info[0].hp -= 1;
                Thread.Sleep(1000);
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
                    is_change_damage_to_enemy = ApplyDamageToEnemy();
                    is_change_heal_to_player = ApplyHealToPlayer();
                    is_change_status_ailment = ApplyStatusAilment();
                }

                // test
                
                foreach(PlayerInfo info in players_info)
                { 
                    if(info!=null)
                    { 
                        Console.Write($"id: {info.client_id}  hp: {info.hp} ");
                        foreach(string s in info.status_ailment_id)
                        { 
                            Console.Write($" {s}");
                        }
                        Console.WriteLine();
                    }
                }

                // 몬스터 처리 (생성, 공격 등)

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
                    enemy_infos[enemy_idx[enemyid]].hp -= damage;
                    // 적 사망
                    if(enemy_infos[enemy_idx[enemyid]].hp <= 0)
                    {
                        enemy_infos[enemy_idx[enemyid]] = null;
                        enemy_idx.Remove(enemyid);
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
                        players_info[player_idx[target_id]].status_ailment_id.Add(status_ailment_id);
                        players_info[player_idx[target_id]].status_ailment_time.Add(status_ailment_time);
                    }
                }
            }
            return return_value;
        }

        private void AddEnemy(string enemy_fix_id, int max_hp)
        {
            enemy_infos[enemy_creation_idx] = new EnemyInfo{enemy_id = enemy_id_idx.ToString(), enemy_sid = enemy_fix_id, hp = max_hp , status_ailment_id = new string[10], status_ailment_time = new string[10]};
            enemy_idx.Add(enemy_id_idx.ToString(), enemy_creation_idx);

            enemy_id_idx++;
            for(int i=0; i<Constant.MAXENEMY; i++)
            {
                if (enemy_infos[i]==null)
                { 
                    enemy_creation_idx = i;
                    break;
                }
            }
        }

        private void CheckAilmentTime()
        { 
            // 플레이어
            for(int i=0; i<Constant.MAXIMUM; i++)
            {
                if (players_info[i] != null)
                { 
                    for(int j=0; j < players_info[i].status_ailment_time.Count; j++)
                    { 
                        ; // 여기부터 개발해야함, 
                    }
                }
            }
            // 적
        }
    }
}
