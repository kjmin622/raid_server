using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cresent_overflow_server
{
    public class Constant
    {
        public const int MAXIMUM = 5;
        public const double WAITMINUTE = 0.25;
        public const int MAXENEMY = 30;

        //BOSS
        public const int BOSS_MAXHP = 11000;
        public const int BOSS_ATTACK_DAMAGE = 90;
        public const int BOSS_MAGIC_DAMAGE = 70;
        public const int BOSS_SKILL1_DAMAGE = 200;

        //M10nn
        public readonly int[] ENEMY_MAXHP = new int[10] { 140, 135, 140, 135, 130, 150, 150, 120, 120, 100 };
        public readonly TimeSpan[] ENEMY_ATTACK_DELAY = new TimeSpan[10] { new TimeSpan(0, 0, 0, 1, 0), new TimeSpan(0, 0, 0, 1, 100), new TimeSpan(0, 0, 0, 1, 0), new TimeSpan(0, 0, 0, 1, 200), new TimeSpan(0, 0, 0, 1, 300), new TimeSpan(0, 0, 0, 1, 500), new TimeSpan(0, 0, 0, 1, 400), new TimeSpan(0, 0, 0, 1, 400), new TimeSpan(0, 0, 0, 1, 300), new TimeSpan(0, 0, 0, 1, 500) };
        public readonly int[] ENEMY_ATTACK_DAMAGE = new int[10] { 7, 7, 0, 8, 0, 6, 6, 0, 0, 0 };
        public readonly int[] ENEMY_MAGIC_DAMAGE = new int[10] { 0, 0, 6, 0, 7, 0, 4, 6, 6, 2 };
        public readonly TimeSpan[] ENEMY_SKILL_DELAY = new TimeSpan[10] { new TimeSpan(0, 0, 0, 5), new TimeSpan(0, 0, 0, 6), new TimeSpan(0, 0, 0, 6), new TimeSpan(0, 0, 0, 7), new TimeSpan(0, 0, 0, 5), new TimeSpan(0, 0, 0, 5), new TimeSpan(0, 0, 0, 6), new TimeSpan(0, 0, 0, 7), new TimeSpan(0, 0, 0, 8), new TimeSpan(0, 0, 0, 6) };
        public readonly Dictionary<int, string> ENEMY_IDX_TO_NAME = new Dictionary<int, string> (){ { 0,"M1001" }, { 1, "M1002" }, { 2, "M1003" }, { 3, "M1004" }, { 4, "M1005" }, { 5, "M1006" }, { 6, "M1007" }, { 7, "M1008" }, { 8, "M1009" }, { 9, "M1010" } };
        public readonly Dictionary<string, int> ENEMY_NAME_TO_IDX = new Dictionary<string, int>() { { "M1001",0 }, { "M1002",1 }, { "M1003",2 }, { "M1004",3 }, { "M1005",4 }, { "M1006",5 }, { "M1007",6 }, { "M1008",7 }, { "M1009",8 }, { "M1010",9 } };
        public readonly int[] ENEMY_SHORT_IDX = new int[5] { 0, 1, 2, 5, 6 };
        public readonly int[] ENEMY_LONG_IDX = new int[5] { 3, 4, 7, 8, 9 };
        public const int ENEMY_EVADE = 2;
        public const int HEALER_HEAL_VALUE = 10;
    }
}
