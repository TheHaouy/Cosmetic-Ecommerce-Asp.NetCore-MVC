using MailChimp.Net;
using MailChimp.Net.Interfaces;
using MailChimp.Net.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Final_VS1.Services
{
    public class MailchimpService : IMailchimpService
    {
        private readonly IMailChimpManager _mailChimpManager;
        private readonly string _listId = string.Empty;
        private readonly ILogger<MailchimpService> _logger;

        public MailchimpService(IConfiguration configuration, ILogger<MailchimpService> logger)
        {
            _logger = logger;
            var apiKey = configuration["Mailchimp:ApiKey"] ?? string.Empty;
            _listId = configuration["Mailchimp:ListId"] ?? string.Empty;

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Mailchimp API Key chưa được cấu hình");
            }

            if (string.IsNullOrEmpty(_listId))
            {
                _logger.LogWarning("Mailchimp List ID chưa được cấu hình");
            }

            _mailChimpManager = new MailChimpManager(apiKey);
            _logger.LogInformation($"Mailchimp initialized - ListId: {_listId}, Server: {apiKey.Split('-').LastOrDefault()}");
        }

        /// <summary>
        /// Tạo MD5 hash của email (Mailchimp yêu cầu)
        /// </summary>
        private string GetMd5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.ASCII.GetBytes(input.ToLower());
                var hashBytes = md5.ComputeHash(inputBytes);
                return Convert.ToHexString(hashBytes).ToLower();
            }
        }

        public async Task<bool> SubscribeAsync(string email, string firstName = "", string lastName = "")
        {
            try
            {
                _logger.LogInformation($"Đang đăng ký email: {email}");

                var member = new Member
                {
                    EmailAddress = email.ToLower(),
                    StatusIfNew = Status.Subscribed,
                    Status = Status.Subscribed
                };

                // Thêm thông tin tên nếu có
                if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
                {
                    member.MergeFields.Add("FNAME", firstName);
                    member.MergeFields.Add("LNAME", lastName);
                }

                var result = await _mailChimpManager.Members.AddOrUpdateAsync(_listId, member);
                
                _logger.LogInformation($"Đăng ký thành công: {email} - Status: {result.Status}");
                return result.Status == Status.Subscribed;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi đăng ký email {email}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UnsubscribeAsync(string email)
        {
            try
            {
                _logger.LogInformation($"Đang hủy đăng ký email: {email}");

                var emailHash = GetMd5Hash(email.ToLower());
                
                // Cập nhật status thành unsubscribed
                var member = new Member
                {
                    EmailAddress = email.ToLower(),
                    Status = Status.Unsubscribed
                };

                var result = await _mailChimpManager.Members.AddOrUpdateAsync(_listId, member);
                
                _logger.LogInformation($"Hủy đăng ký thành công: {email}");
                return result.Status == Status.Unsubscribed;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi hủy đăng ký email {email}: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetSubscriptionStatusAsync(string email)
        {
            try
            {
                var emailHash = GetMd5Hash(email.ToLower());
                var member = await _mailChimpManager.Members.GetAsync(_listId, emailHash);
                return member?.Status.ToString() ?? "Not Found";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi kiểm tra status email {email}: {ex.Message}");
                return "Error";
            }
        }

        public async Task<bool> UpdateSubscriberAsync(string email, string firstName = "", string lastName = "")
        {
            try
            {
                _logger.LogInformation($"Đang cập nhật thông tin subscriber: {email}");

                var emailHash = GetMd5Hash(email.ToLower());
                var member = new Member
                {
                    EmailAddress = email.ToLower()
                };

                if (!string.IsNullOrEmpty(firstName))
                    member.MergeFields.Add("FNAME", firstName);
                
                if (!string.IsNullOrEmpty(lastName))
                    member.MergeFields.Add("LNAME", lastName);

                var result = await _mailChimpManager.Members.AddOrUpdateAsync(_listId, member);
                
                _logger.LogInformation($"Cập nhật thành công: {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi cập nhật subscriber {email}: {ex.Message}");
                return false;
            }
        }
    }
}
