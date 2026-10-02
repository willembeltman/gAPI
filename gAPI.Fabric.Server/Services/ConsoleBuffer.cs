//using gAPI.Fabric.Server.Models;
//using System.Collections.Concurrent;

//namespace gAPI.Fabric.Server.Services;

//public class ConsoleBuffer
//{
//    public ConcurrentQueue<ColorLine> Lines { get; } = [];

//    // Als de code naar de console schrijft wordt dit hier gedaan (ivm snelheid)
//    public void WriteError(string message, Exception ex)
//    {
//        Lines.Enqueue(new ColorLine() { Color = ConsoleColor.Red, Text = $"{message}\r\n{ex.Message}" });
//    }
//    public void WriteInfo(string message)
//    {
//        Lines.Enqueue(new ColorLine() { Color = ConsoleColor.White, Text = message });
//    }
//}