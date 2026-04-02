using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
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
    private IDbRepository _dbRepository;

    public WebSocketsCommunicator(ILogger<WebSocketsCommunicator> logger, IShardOperator shardOperator, IFileOperator fileOperator, IDbRepository dbRepository)
    {
        _shardOperator = shardOperator;
        _fileOperator = fileOperator;
        _logger = logger;
        _dbRepository = dbRepository;
    }

    public async Task Init () {
        _logger.LogInformation("creating socket");
        var serverUri = new Uri($"http://{WebConstants.ServerAddress}:5000");
        // var serverUri = new Uri($"ws://127.0.0.1:5000");

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
        
        _socket.OnConnected += (sender, e) => _logger.LogInformation("Connected to server");
        _socket.OnDisconnected += (sender, e) => _logger.LogInformation("Disconnected from server");
        _socket.OnPing += (sender, e) => _logger.LogDebug("Received ping from server");
        _socket.OnError += (sender, e) => _logger.LogError($"Error {e}");
        _socket.OnReconnectAttempt += (sender, e) => _logger.LogInformation($"Reconnect attempt: {e}");
        _socket.OnReconnectError += (sender, e) => _logger.LogWarning($"Reconnected error: {e.Message}");
        
        
        _socket.On("message", response =>
        {
            _logger.LogInformation(response.RawText);
            return Task.CompletedTask;
        });
        
        _socket.On("users_count", response =>
        {
            _logger.LogInformation(response.RawText);
            return Task.CompletedTask;
        });
        
        // input: {"FilePublicKey": hex_string, "ShardIndex": int}
        _socket.On("has_shard", response =>
        {
            _logger.LogDebug(response.RawText);
            
            var dict = response.GetValue<Dictionary<string, object>>(0);

            if (dict != null && dict.TryGetValue("FilePublicKey", out var pubKey))
            {
                var publicKey = pubKey.ToString();
                _logger.LogInformation($"{publicKey} <- this is the key that was received");
                
                _logger.LogDebug("has_shard: Sending Ack");
                Console.WriteLine(publicKey != null && _shardOperator.HasShardWithPubkey(publicKey));
                if (dict.TryGetValue("ShardIndex", out var shardIndex))
                {
                    response.SendAckDataAsync([
                        publicKey != null && _shardOperator.HasShardWithPubkey(publicKey, (int)shardIndex)
                    ]);
                }
                else
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
            Console.WriteLine($"Received response from server on shard saving: {response.RawText}");
            return Task.CompletedTask;
        });
        
        _socket.On("get_proof_bytes", response =>
        {
            _logger.LogDebug("Get proof bytes request from server (file checkup stage 1/3)");
            var inputData = response.GetValue<Dictionary<string, string>>(0);

            if (inputData == null) return Task.CompletedTask;
            
            inputData.TryGetValue("FilePublicKey", out var publicKey);
            if (publicKey == null) return Task.CompletedTask;

            var shard = _shardOperator.LoadShardByPublicKey(publicKey);
            
            if (shard != null) response.SendAckDataAsync([shard.ProofBytesUnencrypted]);

            return Task.CompletedTask;
        });
        
        _socket.On("check_proof_bytes", response =>
        {
            _logger.LogDebug("Checking (decrypting) proof bytes request from server (file checkup stage 2/3)");
            var inputData = response.GetValue<Dictionary<string, string>>(0);

            if (inputData == null) return Task.CompletedTask;
            
            inputData.TryGetValue("FilePublicKey", out var publicKey);
            if (publicKey == null) return Task.CompletedTask;
            inputData.TryGetValue("ProofBytes", out var proofBytesUnencrypted);
            if (proofBytesUnencrypted == null) return Task.CompletedTask;
            
            _logger.LogDebug("This is how proof bytes look like on this stage {proofBytes}", proofBytesUnencrypted);

            try
            {
                var proofBytesDecrypted = RsaEncryptor.RsaBytesDecrypt(Convert.FromBase64String(proofBytesUnencrypted),
                    _dbRepository.GetPrivateKeyByPublicKey(publicKey));
                
                response.SendAckDataAsync([proofBytesDecrypted]);
                return Task.CompletedTask;
            }
            catch (CryptographicException e)
            {
                _logger.LogWarning("Decryption of proof bytes failed: {error_message}", e.Message);
            }

            return Task.CompletedTask;
        });

        _socket.On("compare_proof_bytes", response =>
        {
            _logger.LogDebug("comparing received proof bytes with original (file checkup stage 3/3)");
            var inputData = response.GetValue<Dictionary<string, string>>(0);

            if (inputData == null) return Task.CompletedTask;
            
            inputData.TryGetValue("FilePublicKey", out var publicKey);
            if (publicKey == null) return Task.CompletedTask;
            inputData.TryGetValue("ProofBytesDecrypted", out var proofBytesDecrypted);
            if (proofBytesDecrypted == null) return Task.CompletedTask;

            var shard = _shardOperator.LoadShardByPublicKey(publicKey);
            if (shard == null) return Task.CompletedTask;
            
            _logger.LogDebug("Comparing these proof bytes: {proofBytes} with these {shardProofBytes}", proofBytesDecrypted, shard.ProofBytesEncrypted);
            
            response.SendAckDataAsync([Convert.FromBase64String(proofBytesDecrypted) == shard.ProofBytesEncrypted]);

            return Task.CompletedTask;
        });
        
        _socket.On("delete_file", response =>
        {
            _logger.LogDebug($"Received delete_file request (first 40 chars): {response.RawText.AsSpan(0, 40)}");
            
            var inputData = response.GetValue<Dictionary<string, string>>(0);

            if (inputData != null && inputData.TryGetValue("FilePublicKey", out var pubKey))
            {
                _shardOperator.DeleteShard(pubKey);
            }
            return Task.CompletedTask;
        });

        await _socket.ConnectAsync();
    }
    
    // Sends 1 shard to the network
    public async Task SendShard(Shard shard)
    {
        await _socket.EmitAsync("save_shard", [JsonSerializer.Serialize(shard)], ack =>
        {
            _logger.LogInformation("save_shard: Saving {ArgRawText}", ack.RawText);
            return Task.CompletedTask;
        });
    }
    
    // TODO
    public async Task GetShards(string publicKey)
    {
        for (var i = 0; i < StorageConstants.ShardingFactor; ++i)
        {
            await _socket.EmitAsync("get_shard", [publicKey, i], ack =>
            {
                try
                {
                    var shard = ack.GetValue<Shard>(0);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                    
                }

                return Task.CompletedTask;
            });
        }
    }
    
    // shallow deletion
    public async Task DeleteFile(string publicKey)
    {
        // just emitting message, no more than that
        await _socket.EmitAsync("delete_file", [JsonSerializer.Serialize(publicKey)], ack =>
        {
            return Task.CompletedTask;
        });
    }

    public async Task CheckFileStatus(string publicKey)
    {
        await _socket.EmitAsync("check_file_status", [JsonSerializer.Serialize(publicKey)], ack =>
        {
            return Task.CompletedTask;
        });
    }
    
}
