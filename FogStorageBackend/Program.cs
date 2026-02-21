// See https://aka.ms/new-console-template for more information

using FogStorageBackend.Model;
using FogStorageBackend.Repository;

class Program
{
    public static void Main(string[] args)
    {
        ShardOperator so = new ShardOperator();
        FileOperator fileOp = new FileOperator();
        StoredFileInfo fi = fileOp.ReadFile("/home/cursed/test/testfile");
        Shard[] shards = so.SplitFile(fi);
        
        foreach (byte b in shards[0].ShardBytes)
        {
            Console.Write(char.ConvertFromUtf32(b));
        }
        
        foreach (byte b in shards[1].ShardBytes)
        {
            Console.Write(char.ConvertFromUtf32(b));
        }
        
        // TODO: run this as a server normally... var host = ....
    }
}
