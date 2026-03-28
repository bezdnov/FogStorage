using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using FogStorageBackend.Configuration;
using FogStorageBackend.Constants;
using FogStorageBackend.Model;
using FogStorageBackend.Repository;
using FogStorageBackend.Utils;
using Microsoft.Extensions.Logging;
using SocketIOClient;
using SocketIOClient.Common;

namespace FogStorageBackend.WebHandling;

public class WebSocketsCommunicator
{
    private SocketIO _socket;
    private IShardOperator _shardOperator;
    private IFileOperator _fileOperator;
    private ILogger _logger;

    public WebSocketsCommunicator(ILogger<WebSocketsCommunicator> logger, IShardOperator shardOperator, IFileOperator fileOperator)
    {
        _shardOperator = shardOperator;
        _fileOperator = fileOperator;
        _logger = logger;
    }

    public async Task Init () {
        _logger.LogInformation("creating socket");
        // var serverUri = new Uri($"ws://{WebConstants.ServerAddress}");
        var serverUri = new Uri($"ws://127.0.0.1:5000");

        _socket = new SocketIO(serverUri, new SocketIOOptions
        {
            EIO = EngineIO.V4,
            Transport = TransportProtocol.WebSocket,
            Auth = new Dictionary<string, int>
            {
                ["client_kept"] = _shardOperator.CalculateShardWeight(),
                ["client_saved"] = -1,
            }
        });
        
        _socket.OnConnected += (sender, e) =>
        {
            _logger.LogInformation("socket connected");
        };
        
        _socket.On("message", response =>
        {
            _logger.LogInformation(response.RawText);
            return Task.CompletedTask;
        });
        
        _socket.On("has_shard", response =>
        {
            _logger.LogDebug(response.RawText);
            
            var dict = response.GetValue<Dictionary<string, object>>(0);

            if (dict != null && dict.TryGetValue("FilePublicKey", out var pubKey))
            {
                var publicKey = pubKey.ToString();
                _logger.LogInformation($"{publicKey} <- this is the key that was received");
                
                _logger.LogDebug("hash_shard: Sending Ack");
                Console.WriteLine(publicKey != null && _shardOperator.HasShardWithPubkey(publicKey));
                response.SendAckDataAsync([publicKey != null && _shardOperator.HasShardWithPubkey(publicKey)]);
            }
            
            return Task.CompletedTask;
        });
        
        _socket.On("save_shard", response =>
        {
            _logger.LogInformation(response.RawText);
            Console.WriteLine(response.RawText);
            
            // var shardDict = response.GetValue<Dictionary<string, string>>(0);
            var shard = response.GetValue<Shard>(0);
            
            if (shard != null) {
                _logger.LogInformation($"Saved shard with pubkey {shard.FilePublicKey}");
                _shardOperator.SaveShard(shard);
            }
            else {
                _logger.LogInformation("Couldn't save shard returned null");
            }
            
            return Task.CompletedTask;
        });
        
        _socket.On("save_shard_ack", response =>
        {
            _logger.LogInformation("save_shard_ack");
            _logger.LogInformation(response.RawText);
            return Task.CompletedTask;
        });
        
        _socket.On("check_proof_bytes", response =>
        {
            _logger.LogInformation("Proofing file ownership");
            var proofBytes = response.GetValue<Dictionary<string, string>>(0);

            if (proofBytes != null)
            {
                
                // RsaEncryptor.RsaBytesDecrypt(proofBytes, );
                
                // TODO send response with unencrypted bytes
                
            } else {
                _logger.LogWarning("No proof bytes received");
            }
            return Task.CompletedTask;
        });
        
        _socket.On("get_proof_bytes", response =>
        {
            return Task.CompletedTask;
        });

        _socket.On("compare_proof_bytes", response =>
        {
            var proofBytes = response.GetValue<string>(0);
            
            return Task.CompletedTask;
        });
        
        await _socket.ConnectAsync();
    }
    
    // Sends 1 shard to the network; makes sure, that it's kept on separate clients (?)
    public async Task SendShard(Shard shard)
    {
        await _socket.EmitAsync("save_shard", [JsonSerializer.Serialize(shard)], ack =>
        {
            _logger.LogInformation("save_shard: Saving {ArgRawText}", ack.RawText);
            return Task.CompletedTask;
        });
    }
    
    public async Task GetShards(string privateKey)
    {
        var tcs = new TaskCompletionSource<Shard[]>();
        
        _socket.EmitAsync("get_shard", [privateKey], ack =>
        {
            try
            {
                var shards = ack.GetValue<Shard[]>(0);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }

            return Task.CompletedTask;
        });
    }

    public async Task DeleteFile(string publicKey)
    {
        await _socket.EmitAsync("delete_file", [JsonSerializer.Serialize(publicKey)], ack =>
        {
            return Task.CompletedTask;
        });
    }

    public async Task CheckFileStatus(string privateKey)
    {
        
    }
    
}
