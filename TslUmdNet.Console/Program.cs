using TslUmdNet;

Console.WriteLine("Example of TSL UMD v5");

TSL5 tsl = new TSL5();

tsl.ListenUDP(8900);
tsl.ListenTCP(9000);

tsl.TallyDataRecieved += (data) =>
{
    Console.WriteLine($"Tally data recieved: {data.Sender} {data.Screen} {data.Index} {data.Display.RhTally}");
};  

TallyData tallyData = new TallyData(1, 1, 1);


tsl.SendTallyUDP("192.168.X.X", 8900, tallyData);
tsl.SendTallyTCP("192.168.X.X", 9000, tallyData);



Console.ReadKey();

