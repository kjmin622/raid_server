using System;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
using client;

public async Task OverflowMain()
{
    Debug.Log("main join");
    main_server = new Overflow(Constant.SERVER_IP, Constant.EASYROOM1_PORT, "easy", "testclientid", "testchracter_id", 300);
    Task serverjoin = new Task(delegate { main_server.Join(); });
    serverjoin.Start();
    await serverjoin;

    WaitRoom wait_server = new WaitRoom(main_server);
    Task wait_server_join = new Task(delegate { wait_server.Start(); });
    wait_server_join.Start();
    await wait_server_join;

}

public void TestAttack()
{
    main_server.DamageToEnemy("0", 10);
}

public void TestHeal()
{
    main_server.HealToPlayer("testclientid", 10);
}

public void TestAilment()
{
    main_server.AilmentToAnyone("testclientid", true, "testailment", 5);
}

namespace client
{
    class Program
    {

        static async Task Main(string[] args)
        {
            OverflowMain();
        }
    }
}