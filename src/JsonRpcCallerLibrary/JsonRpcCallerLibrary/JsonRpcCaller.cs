using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using Newtonsoft.Json;

namespace JsonRpcCallerLibrary
{
    public sealed class JsonRpcCaller : IDynamicMetaObjectProvider
    {
        private int _id;
        private readonly string _baseUrl;
        private const string ProtocolVersion = "2.0";
        public JsonRpcCaller(string baseUrl)
        {
            _baseUrl = baseUrl;
        }
        public DynamicMetaObject GetMetaObject(Expression expression)
        {
            return new MetaJsonRpcCaller(expression, this);
        }

        private dynamic PrivateCall(string method, params string[] args)
        {
            var c = new HttpClient();
            var p = new JsonRpcRequest
            {
                ID = ++_id,
                Method = method,
                Version = ProtocolVersion,
                Params = args
            };
            var result = c.PostAsJsonAsync(_baseUrl, p).Result.Content.ReadAsAsync<JsonRpcResponse>().Result;
            if (result.Error != null)
                throw new JsonRpcCallerException(result.Error.Message)
                {
                    Code = result.Error.Code,
                    ExtraData = result.Error.Data
                };
            return result.Result;
        }

        private class JsonRpcRequest
        {
            [JsonProperty("jsonrpc")]
            public string Version { get; set; }
            [JsonProperty("id")]
            public int ID { get; set; }
            [JsonProperty("method")]
            public string Method { get; set; }
            [JsonProperty("params")]
            public object Params { get; set; }
        }

        private class JsonRpcResponse
        {
            [JsonProperty("jsonrpc")]
            public string Version { get; set; }
            [JsonProperty("id")]
            public int ID { get; set; }
            [JsonProperty("error")]
            public JsonRpcError Error { get; set; }
            [JsonProperty("result")]
            public object Result { get; set; }
        }

        private class JsonRpcError
        {
            [JsonProperty("code")]
            public int Code { get; set; }
            [JsonProperty("message")]
            public string Message { get; set; }
            [JsonProperty("data")]
            public object Data { get; set; }
        }

        private class MetaJsonRpcCaller : DynamicMetaObject
        {
            private static readonly MethodInfo CallService = typeof(JsonRpcCaller).GetMethod("PrivateCall", BindingFlags.Instance | BindingFlags.NonPublic);
            internal MetaJsonRpcCaller
            (Expression expression, JsonRpcCaller creator)
                : base(expression, BindingRestrictions.Empty, creator)
            { }
            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                var targetObject = (JsonRpcCaller)Value;
                Expression self = Expression.Convert(Expression, typeof(JsonRpcCaller));
                Expression targetBehavior = Expression.Call(self, CallService, Expression.Constant(binder.Name), Expression.Constant(args.Select(p => p.Value.ToString()).ToArray()));

                var restrictions = BindingRestrictions.GetInstanceRestriction(self, targetObject);
                return new DynamicMetaObject(targetBehavior, restrictions);
            }
        }
    }
}
