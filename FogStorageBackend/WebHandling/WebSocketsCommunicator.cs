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
    private readonly IShardOperator _shardOperator;
    private readonly ILogger _logger;
    private readonly IDbRepository _dbRepository;
    
    public event EventHandler ConnectionChanged;
    public event EventHandler FileRestoredOrDeleted;

    public WebSocketsCommunicator(ILogger<WebSocketsCommunicator> logger, IShardOperator shardOperator, IDbRepository dbRepository)
    {
        _shardOperator = shardOperator;
        _logger = logger;
        _dbRepository = dbRepository;
    }

    public async Task Init () {
        _logger.LogInformation($"Initializing WebSockets, connecting to {WebConstants.ServerAddress}");
        var serverUri = new Uri($"http://{WebConstants.ServerAddress}:5000");
        // var serverUri = new Uri($"ws://127.0.0.1:5000");

        _socket = new SocketIO(serverUri, new SocketIOOptions
        {
            EIO = EngineIO.V4,
            Transport = TransportProtocol.WebSocket,
            Auth = new Dictionary<string, int>
            {
                ["client_kept"] = _shardOperator.CalculateShardWeight(),
                ["client_saved"] = _dbRepository.GetFileSizeSummary(),
            },
            ReconnectionAttempts = 2000,  // yes, it will be retrying for forever
        });
        
        _socket.OnConnected += (sender, e) =>
        {
            _logger.LogInformation("Connected to server");
            IsConnected = true;
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
        };
        _socket.OnDisconnected += (sender, e) =>
        {
            _logger.LogWarning("Disconnected from server");
            IsConnected = false;
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
        };
        _socket.OnPing += (sender, e) => _logger.LogDebug("Received ping from server");
        _socket.OnError += (sender, e) => _logger.LogError($"Error {e}");
        _socket.OnReconnectAttempt += (sender, e) => _logger.LogInformation($"Reconnect attempt: {e}");
        _socket.OnReconnectError += (sender, e) => _logger.LogWarning($"Reconnected error: {e.Message}");
        
        
        _socket.On("message", response =>
        {
            _logger.LogDebug(response.RawText);
            return Task.CompletedTask;
        });
        
        _socket.On("users_count", response =>
        {
            _logger.LogDebug(response.RawText);
            return Task.CompletedTask;
        });
        
        // input: {"FilePublicKey": hex_string, "ShardIndex": int}, second is optional
        _socket.On("has_shard", response =>
        {
            _logger.LogInformation("Checking shard existence request");
            
            var dict = response.GetValue<Dictionary<string, object>>(0);

            if (dict == null || !dict.TryGetValue("FilePublicKey", out var pubKey))
                return Task.CompletedTask;
            
            // WebSockets return strings with " at start and end
            var publicKey = pubKey.ToString().Trim('"');
            
            _logger.LogDebug($"{publicKey.AsSpan(0, 40)} <- this is the key that was received");
            
            _shardOperator.UpdateShard(publicKey);
            
            if (dict.TryGetValue("ShardIndex", out var shardIndex))
            {
                _logger.LogDebug($"has_shard: working with index {Convert.ToInt32(shardIndex.ToString())}");
                bool result = _shardOperator.HasShardWithPubkey(publicKey, Convert.ToInt32(shardIndex.ToString()));
                _logger.LogDebug($"has_shard: {result}");
                response.SendAckDataAsync([result]);
            }
            else
                response.SendAckDataAsync([_shardOperator.HasShardWithPubkey(publicKey)]);
        
            return Task.CompletedTask;
        });
        
        _socket.On("save_shard", response =>
        {
            _logger.LogInformation("Saving shard");
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
        
        _socket.On("give_shard", response =>
        {
            _logger.LogInformation("Server requested shard");
            var dict = response.GetValue<Dictionary<string, object>>(0);

            if (dict == null || !dict.TryGetValue("FilePublicKey", out var pubKey))
                return Task.CompletedTask;
            
            // WebSockets return strings with " at start and end
            var publicKey = pubKey.ToString().Trim('"');

            if (!dict.TryGetValue("ShardIndex", out var shardI))
                return Task.CompletedTask;
            var shardIndex = (int)shardI;

            var shard = _shardOperator.LoadShardByPublicKey(publicKey);
            if (shard.ShardIndex != shardIndex)
                _logger.LogWarning("Returning a shard with incorrect shard index");
            
            _logger.LogDebug($"give_shard: returning a shard");

            response.SendAckDataAsync([shard]);

            return Task.CompletedTask;
        });
        
        _socket.On("save_shard_ack", response =>
        {
            _logger.LogInformation("Shard was saved (received acknowledgement)");
            return Task.CompletedTask;
        });
        
        _socket.On("get_shard_ack", response =>
        {
            _logger.LogInformation($"get_shard_ack: {response.RawText}");
            return Task.CompletedTask;
        });
        
        _socket.On("get_proof_bytes", response =>
        {
            _logger.LogDebug("Get proof bytes request from server (file checkup stage 1/2)");
            var inputData = response.GetValue<Dictionary<string, string>>(0);

            if (inputData == null) return Task.CompletedTask;
            
            inputData.TryGetValue("FilePublicKey", out var publicKey);
            if (publicKey == null) return Task.CompletedTask;

            var shard = _shardOperator.LoadShardByPublicKey(publicKey);
            
            if (shard != null) response.SendAckDataAsync([Convert.ToBase64String(shard.ProofBytesEncrypted), Convert.ToBase64String(shard.ProofBytesUnencrypted)]);

            return Task.CompletedTask;
        });
        
        _socket.On("check_proof_bytes", response =>
        {
            _logger.LogInformation("Checking (decrypting) proof bytes request from server (file checkup stage 2/2)");
            var inputData = response.GetValue<Dictionary<string, string>>(0);
            if (inputData == null) return Task.CompletedTask;
            
            inputData.TryGetValue("FilePublicKey", out var publicKey);
            inputData.TryGetValue("ProofBytes", out var proofBytesUnencrypted);
            if (publicKey == null || proofBytesUnencrypted == null) return Task.CompletedTask;
            
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
        
        /* Deprecated way to proof ownership
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
        */
        
        _socket.On("delete_file", response =>
        {
            _logger.LogDebug($"Received delete_file request (first 40 chars): {response.RawText.AsSpan(0, 40)}");
            
            var inputData = response.GetValue<Dictionary<string, string>>(0);

            if (inputData != null && inputData.TryGetValue("FilePublicKey", out var shardPublicKey))
            {
                _shardOperator.DeleteShard(shardPublicKey);
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
    
    public async Task<Shard[]?> GetShards(string publicKey)
    {
        _logger.LogDebug("Getting shards");
        var shards = new Shard[StorageConstants.ShardingFactor];

        for (var i = 0; i < StorageConstants.ShardingFactor; ++i)
        {
            // Convert callback to Task
            var shard = await Task.Run<Shard?>(async () =>
            {
                Shard? result;
                var tcs = new TaskCompletionSource<Shard?>();

                await _socket.EmitAsync("get_shard", new object[] { publicKey, i }, ack =>
                {
                    _logger.LogInformation($"get_shard: retrieving shard {i}");

                    if (ack.RawText == "[]")
                    {
                        _logger.LogWarning($"Server couldn't get shard {i}");
                        tcs.SetResult(null);
                        return Task.CompletedTask;
                    }

                    try
                    {
                        var shardValue = ack.GetValue<string>(0);
                        var shardObj = JsonSerializer.Deserialize<Shard>(shardValue);
                        tcs.SetResult(shardObj);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Error getting shard {i}");
                        tcs.SetResult(null); // treat as missing
                    }

                    return Task.CompletedTask;
                });

                result = await tcs.Task;
                return result;
            });

            if (shard == null)
            {
                _logger.LogWarning("One of the shards was missing. Returning null.");
                return null;
            }

            shards[i] = shard;
        }

        _logger.LogDebug("Got all shards");
        return shards;
    }
    
    public async Task DeleteFile(string publicKey)
    {
        _dbRepository.DeleteByPublicKey(publicKey);
        FileRestoredOrDeleted?.Invoke(this, EventArgs.Empty);
        // deleting file from db here
        await _socket.EmitAsync("delete_file", [JsonSerializer.Serialize(publicKey)], ack => Task.CompletedTask);
    }
    
    public async Task CheckFileStatus(string publicKey)
    {
        // var replicaAmount = new int[StorageConstants.ShardingFactor];
        for (var i = 0; i < StorageConstants.ShardingFactor; ++i)
        {
            await _socket.EmitAsync("check_file", [JsonSerializer.Serialize(publicKey)], ack =>
            {
                // replicaAmount[i] = ack.GetValue<int>(0);
                return Task.CompletedTask;
            });
            
        }
        // return replicaAmount;
    }

    public bool IsConnected { get; private set; }
}
