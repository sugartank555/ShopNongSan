using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using ShopNongSan.ViewModels;

namespace ShopNongSan.Services
{
    public interface ICartSession
    {
        CartVM Get();
        void Save(CartVM cart);
        void Clear();
    }

    public class CartSession : ICartSession
    {
        private readonly ISession _session;
        private const string Key = "CART_V1";
        public CartSession(IHttpContextAccessor accessor) => _session = accessor.HttpContext!.Session;

        public CartVM Get()
        {
            var raw = _session.GetString(Key);
            return string.IsNullOrEmpty(raw) ? new CartVM() : JsonConvert.DeserializeObject<CartVM>(raw)!;
        }
        public void Save(CartVM cart) => _session.SetString(Key, JsonConvert.SerializeObject(cart));
        public void Clear() => _session.Remove(Key);
    }
}
