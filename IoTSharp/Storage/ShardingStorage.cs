﻿using EFCore.Sharding;
using IoTSharp.Data;
using IoTSharp.Dtos;
using IoTSharp.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IoTSharp.Storage
{
    /// <summary>
    /// 由于此模式目前无法通过EFCore.Sharding 进行Group By 获取最新遥测数据， 和Select  新对象， 所以， 最新遥测依然从DataStorage表里获取，历史从分表里获取
    /// 更多内容可以参考<seealso cref="https://github.com/Coldairarrow/EFCore.Sharding/issues/52"/>
    /// </summary>
    public class ShardingStorage : IStorage
    {
        private readonly AppSettings _appSettings;
        private readonly ILogger _logger;
        private readonly IServiceScope scope;
        private readonly IServiceScopeFactory _scopeFactor;
      

        public ShardingStorage(ILogger<ShardingStorage> logger, IServiceScopeFactory scopeFactor
           , IOptions<AppSettings> options
            )
        {
            _appSettings = options.Value;
            _logger = logger;
            scope = scopeFactor.CreateScope();
            _scopeFactor = scopeFactor;
        }

        public Task<List<TelemetryDataDto>> GetTelemetryLatest(Guid deviceId)
        {
            try
            {
                using (var _scope = _scopeFactor.CreateScope())
                {
                    using (var _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
                    {
                        var devid = from t in _context.TelemetryLatest
                                    where t.DeviceId == deviceId
                                    select new TelemetryDataDto() { DateTime = t.DateTime, KeyName = t.KeyName, Value = t.ToObject() };

                        return Task.FromResult(devid.AsNoTracking().ToList());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ deviceId}数据处理失败{ex.Message} {ex.InnerException?.Message} ");
                throw;
            }
        }

        public Task<List<TelemetryDataDto>> GetTelemetryLatest(Guid deviceId, string keys)
        {
            try
            {
                using (var _scope = _scopeFactor.CreateScope())
                {
                    using (var _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
                    {
                        var devid = from t in _context.TelemetryLatest
                                    where t.DeviceId == deviceId && keys.Split(',', ' ', ';').Contains(t.KeyName)

                                    select new TelemetryDataDto() { DateTime = t.DateTime, KeyName = t.KeyName, Value = t.ToObject() };

                        return Task.FromResult(devid.AsNoTracking().ToList());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ deviceId}数据处理失败{ex.Message} {ex.InnerException?.Message} ");
                throw;
            }
        }

        public Task<List<TelemetryDataDto>> LoadTelemetryAsync(Guid deviceId, string keys, DateTime begin)
        {
            return LoadTelemetryAsync(deviceId, keys, begin, DateTime.Now);
        }

        public Task<List<TelemetryDataDto>> LoadTelemetryAsync(Guid deviceId, string keys, DateTime begin, DateTime end)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var _scope = _scopeFactor.CreateScope())
                    {
                        using (var _context = _scope.ServiceProvider.GetService<IShardingDbAccessor>())
                        {
                            var lst = new List<TelemetryDataDto>();
                            var kv = _context.GetIShardingQueryable<TelemetryData>()
                                .Where(t => t.DeviceId == deviceId && keys.Split(',', ' ', ';').Contains(t.KeyName) && t.DateTime >= begin && t.DateTime < end)
                                .ToList().Select(t => new TelemetryDataDto() { DateTime = t.DateTime, KeyName = t.KeyName, Value = t.ToObject() });
                            return kv.ToList();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{ deviceId}数据处理失败{ex.Message} {ex.InnerException?.Message} ");
                    throw;
                }
            });
        }

        public Task<List<TelemetryDataDto>> LoadTelemetryAsync(Guid deviceId, DateTime begin)
        {
            return LoadTelemetryAsync(deviceId, begin, DateTime.Now);
        }

        public Task<List<TelemetryDataDto>> LoadTelemetryAsync(Guid deviceId, DateTime begin, DateTime end)
        {
            return Task.Run(() =>
            {
                using (var _context = scope.ServiceProvider.GetService<IShardingDbAccessor>())
                {
                    var lst = new List<TelemetryDataDto>();
                    var kv = _context.GetIShardingQueryable<TelemetryData>()
                        .Where(t => t.DeviceId == deviceId && t.DateTime >= begin && t.DateTime < end)
                        .ToList().Select(t => new TelemetryDataDto() { DateTime = t.DateTime, KeyName = t.KeyName, Value = t.ToObject() });
                    return kv.ToList();
                }
            });
        }
        
        public async Task<(bool result, List<TelemetryData> telemetries)> StoreTelemetryAsync(RawMsg msg)
        {
            bool result = false;
            List<TelemetryData> telemetries = new List<TelemetryData>();

            try
            {
                using (var db = scope.ServiceProvider.GetService<IShardingDbAccessor>())
                {
                    var lst = new List<TelemetryData>();
                    msg.MsgBody.ToList().ForEach(kp =>
                                     {
                                         if (kp.Value != null)
                                         {
                                             var tdata = new TelemetryData() { DateTime = DateTime.Now, DeviceId = msg.DeviceId, KeyName = kp.Key};
                                             tdata.FillKVToMe(kp);
                                             lst.Add(tdata);
                                             telemetries.Add(tdata);
                                         }
                                     });
                    int ret = await db.InsertAsync(lst);
                    _logger.LogInformation($"新增({msg.DeviceId})遥测数据{ret}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{msg.DeviceId}数据处理失败{ex.Message} {ex.InnerException?.Message} ");
            }


            try
            {
                using (var _scope = _scopeFactor.CreateScope())
                {
                    using (var _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
                    {
                        var result1 = await _dbContext.SaveAsync<TelemetryLatest>(msg.MsgBody, msg.DeviceId, msg.DataSide);
                        result1.exceptions?.ToList().ForEach(ex =>
                        {
                            _logger.LogError(ex.Value, $"{ex.Key} {ex.Value.Message} {ex.Value.InnerException?.Message}");
                        });
                        _logger.LogInformation($"新增({msg.DeviceId})遥测数据更新最新信息{result1.ret}");
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{msg.DeviceId}数据处理失败{ex.Message} {ex.InnerException?.Message} ");
            }
            return (result,telemetries);
        }
    }
}