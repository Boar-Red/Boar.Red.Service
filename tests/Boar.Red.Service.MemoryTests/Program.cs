using Boar.Red.Service.MemoryTests;

await ControllerLifetimeTests.RunAsync();
StatisticsRetentionTests.Run();
await WebSocketLifetimeTests.RunAsync();
Console.WriteLine("PASS all memory regression checks");
