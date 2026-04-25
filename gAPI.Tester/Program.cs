// See https://aka.ms/new-console-template for more information
using System.Dynamic;

Console.WriteLine("Hello, World!");

var stream = new MemoryStream();
var writer = new BinaryWriter(stream);
byte[] data = new byte[50];
writer.Write(data);

stream.Position = 0;
var reader = new BinaryReader(stream);
//reader.ReadBytes(data);