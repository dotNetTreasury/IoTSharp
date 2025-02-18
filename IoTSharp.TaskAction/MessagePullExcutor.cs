﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter;
using IoTSharp.Data;
using IoTSharp.TaskAction;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace IoTSharp.TaskAction
{
    [DisplayName("用于消息推送的执行器")]
    public class MessagePullExecutor : ITaskAction
    {

        public MessagePullExecutor()
        {


        }

        public override TaskActionOutput Execute(TaskActionInput input)
        {
            try
            {
            //    var cache = this.ServiceProvider.GetService(typeof(IMemoryCache)) as IMemoryCache;

                return SendData(input);

            }
            catch (Exception ex)
            {
                return new TaskActionOutput() { ExecutionInfo = ex.Message, ExecutionStatus = false, };
            }
        }


        private void Login(ModelExecutorConfig config)
        {




        }


        private TaskActionOutput SendData(TaskActionInput input)
        {
            try
            {

                if (input.Input.Contains("ValueKind"))
                {


                    JObject o = JsonConvert.DeserializeObject(input.Input) as JObject;
                    var config = JsonConvert.DeserializeObject<ModelExecutorConfig>(input.ExecutorConfig);
                    var dd = o.Properties().Select(c => new ParamObject { keyName = c.Name, value = JPropertyToObject(c.Value.First as JProperty) }).ToList();
                    string contentType = "application/json";
                    var restclient = new RestClient(config.BaseUrl);
                    var request = new RestRequest(config.Url + (input.DeviceId == Guid.Empty ? "" : "/" + input.DeviceId), Method.POST);
                    request.AddHeader("X-Access-Token",
                        config.Token);
                    request.RequestFormat = DataFormat.Json;
                    request.AddHeader("cache-control", "no-cache");
                    request.AddJsonBody(JsonConvert.SerializeObject(dd));
                    var response = restclient.Execute(request);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var result = JsonConvert.DeserializeObject<MessagePullResult>(response.Content);
                        if (result != null && result.success)
                        {
                            return new TaskActionOutput() { ExecutionInfo = result.message, ExecutionStatus = result.success, DynamicOutput = input.DynamicInput }; ;
                        }
                        else
                        {
                            return new TaskActionOutput() { ExecutionInfo = result.message, ExecutionStatus = result.success }; ;
                        }
                    }
                    else
                    {
                        return new TaskActionOutput() { ExecutionInfo = response.ErrorMessage, ExecutionStatus = false }; ;
                    }



                }
                else
                {
                    JObject o = JsonConvert.DeserializeObject(input.Input) as JObject;
                    var config = JsonConvert.DeserializeObject<ModelExecutorConfig>(input.ExecutorConfig);
                    var dd = o.Properties().Select(c => new ParamObject { keyName = c.Name, value = JPropertyToObject(c) }).ToList();
                    string contentType = "application/json";
                    var restclient = new RestClient(config.BaseUrl);
                    var request = new RestRequest(config.Url + (input.DeviceId == Guid.Empty ? "" : "/" + input.DeviceId), Method.POST);
                    request.AddHeader("X-Access-Token",
                        config.Token);
                    request.RequestFormat = DataFormat.Json;
                    request.AddHeader("cache-control", "no-cache");
                    request.AddJsonBody(JsonConvert.SerializeObject(dd));
                    var response = restclient.Execute(request);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var result = JsonConvert.DeserializeObject<MessagePullResult>(response.Content);
                        if (result != null && result.success)
                        {
                            return new TaskActionOutput() { ExecutionInfo = result.message, ExecutionStatus = result.success, DynamicOutput = input.DynamicInput }; ;
                        }
                        else
                        {
                            return new TaskActionOutput() { ExecutionInfo = result.message, ExecutionStatus = result.success }; ;
                        }
                    }
                    else
                    {
                        return new TaskActionOutput() { ExecutionInfo = response.ErrorMessage, ExecutionStatus = false }; ;
                    }

                }




            }
            catch (Exception ex)
            {
                return new TaskActionOutput() { ExecutionInfo = ex.Message, ExecutionStatus = false }; ;
            }
        }


        public static object JPropertyToObject(JProperty property)
        {
            object obj = null;
            switch (property.Value.Type)
            {
                case JTokenType.Integer:
                    obj = property.Value.ToObject<int>();
                    break;
                case JTokenType.Float:
                    obj = property.Value.ToObject<float>();
                    break;
                case JTokenType.String:
                    obj = property.Value.ToObject<string>();
                    break;
                case JTokenType.Boolean:
                    obj = property.Value.ToObject<bool>();
                    break;
                case JTokenType.Date:
                    obj = property.Value.ToObject<DateTime>();
                    break;
                case JTokenType.Bytes:
                    obj = property.Value.ToObject<byte[]>();
                    break;
                case JTokenType.Guid:
                    obj = property.Value.ToObject<Guid>();
                    break;
                case JTokenType.Uri:
                    obj = property.Value.ToObject<Uri>();
                    break;
                case JTokenType.TimeSpan:
                    obj = property.Value.ToObject<TimeSpan>();
                    break;
                default:
                    obj = property.Value;
                    break;
            }
            return obj;
        }

        class MessagePullParam
        {


            public double param1 { get; set; }
            public double param2 { get; set; }
            public double param3 { get; set; }
            public int param4 { get; set; }

            public long param5 { get; set; }


            public ParamObject param6 { get; set; }

            public string param9 { get; set; }
        }


        class MessagePullResult
        {


            public bool success { get; set; }
            public string message { get; set; }
            public string code { get; set; }

            public long timestamp { get; set; }

            public dynamic result { get; set; }


        }


        public class LoginParam
        {


        }

        public class ParamObject
        {

            public string keyName { get; set; }
            public dynamic value { get; set; }
        }

        class ModelExecutorConfig
        {
            public string Url { get; set; }
            public string BaseUrl { get; set; }
            public string Method { get; set; }
            public string Passwoid { get; set; }
            public string UserName { get; set; }
            public string Token { get; set; }

        }
    }
}
