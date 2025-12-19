using System.Threading.Tasks;

namespace Final_VS1.Services
{
    public interface IMailchimpService
    {
        /// <summary>
        /// Đăng ký email vào Mailchimp audience
        /// </summary>
        Task<bool> SubscribeAsync(string email, string firstName = "", string lastName = "");

        /// <summary>
        /// Hủy đăng ký email khỏi Mailchimp audience
        /// </summary>
        Task<bool> UnsubscribeAsync(string email);

        /// <summary>
        /// Kiểm tra trạng thái đăng ký của email
        /// </summary>
        Task<string> GetSubscriptionStatusAsync(string email);

        /// <summary>
        /// Cập nhật thông tin subscriber
        /// </summary>
        Task<bool> UpdateSubscriberAsync(string email, string firstName = "", string lastName = "");
    }
}
