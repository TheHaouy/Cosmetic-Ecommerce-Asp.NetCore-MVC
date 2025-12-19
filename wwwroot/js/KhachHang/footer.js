document.addEventListener("DOMContentLoaded", function () {
  // Initialize footer link notifications
  initFooterNotifications();
  // Initialize social media notifications
  initSocialMediaNotifications();
  // Initialize mobile-friendly footer categories
  initMobileFooterCategories();
});

// Initialize mobile-friendly footer categories
function initMobileFooterCategories() {
  const isTouchDevice = ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);
  const isSmallScreen = window.innerWidth <= 991;

  if (isTouchDevice && isSmallScreen) {
    // Lấy tất cả category items có subcategories
    const categoryItems = document.querySelectorAll('.footer-category-item.has-subcategories');

    categoryItems.forEach(item => {
      const categoryLink = item.querySelector('.footer-category-link');
      
      if (categoryLink) {
        // Xóa event listeners cũ nếu có
        const newCategoryLink = categoryLink.cloneNode(true);
        categoryLink.parentNode.replaceChild(newCategoryLink, categoryLink);
        
        // Ngăn link mặc định khi click vào parent category trên mobile
        newCategoryLink.addEventListener('click', function(e) {
          e.preventDefault();
          e.stopPropagation();

          // Kiểm tra trạng thái hiện tại
          const isCurrentlyActive = item.classList.contains('active');

          // Đóng tất cả các category khác trước
          categoryItems.forEach(otherItem => {
            otherItem.classList.remove('active');
          });

          // Nếu item không active thì mở, nếu đã active thì đóng (đã remove ở trên)
          if (!isCurrentlyActive) {
            item.classList.add('active');
          }
          // Nếu isCurrentlyActive = true, thì đã remove rồi, không cần làm gì thêm
        });
      }

      // Subcategory links vẫn hoạt động bình thường
      const subcategoryLinks = item.querySelectorAll('.footer-subcategories a');
      subcategoryLinks.forEach(link => {
        link.addEventListener('click', function(e) {
          // Cho phép link hoạt động bình thường
          e.stopPropagation();
        });
      });
    });

    // Thêm event listener để đóng tất cả khi click ra ngoài (vào khoảng trống)
    document.addEventListener('click', function(e) {
      // Kiểm tra xem click có nằm trong footer category không
      const isClickInsideCategory = e.target.closest('.footer-category-item.has-subcategories');
      
      // Nếu click ra ngoài (vào khoảng trống), đóng tất cả các category
      if (!isClickInsideCategory) {
        categoryItems.forEach(item => {
          item.classList.remove('active');
        });
      }
    });
  }
}

// Cập nhật lại khi resize
window.addEventListener('resize', debounce(function() {
  initMobileFooterCategories();
}, 250));

// Debounce function
function debounce(func, wait) {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
}

function initFooterNotifications() {
  // Create notification container if it doesn't exist
  createNotificationContainer();

  // Add click event listeners to footer links
  const footerLinks = document.querySelectorAll(
    ".footer-links a[data-feature]"
  );

  footerLinks.forEach((link) => {
    link.addEventListener("click", function (e) {
      e.preventDefault();

      const featureName =
        this.querySelector("i").nextSibling.textContent.trim();
      showDevelopmentNotification(featureName);
    });
  });
}

function initSocialMediaNotifications() {
  // Create notification container if it doesn't exist
  createNotificationContainer();

  // Add click event listeners to social media links
  const socialLinks = document.querySelectorAll(".social-link[data-social]");

  socialLinks.forEach((link) => {
    link.addEventListener("click", function (e) {
      e.preventDefault();

      const socialPlatform = this.dataset.social;
      const platformName = this.title || socialPlatform;
      showSocialMediaNotification(platformName, socialPlatform);
    });
  });
}

function createNotificationContainer() {
  if (document.getElementById("notification-container")) return;

  const container = document.createElement("div");
  container.id = "notification-container";
  container.className = "notification-container";
  document.body.appendChild(container);
}

function showDevelopmentNotification(featureName) {
  const container = document.getElementById("notification-container");

  // Create notification element
  const notification = document.createElement("div");
  notification.className = "development-notification";

  notification.innerHTML = `
        <div class="notification-content">
            <div class="notification-header">
                <i class="fas fa-tools notification-icon"></i>
                <span class="notification-title">Tính năng đang phát triển</span>
                <button class="notification-close" onclick="closeNotification(this)">
                    <i class="fas fa-times"></i>
                </button>
            </div>
            <div class="notification-body">
                <p><strong>"${featureName}"</strong> hiện đang trong quá trình phát triển.</p>
                <p>Chúng tôi sẽ sớm ra mắt tính năng này trong thời gian tới. Cảm ơn bạn đã quan tâm!</p>
            </div>
            <div class="notification-footer">
                <i class="fas fa-heart text-danger"></i>
                <span>LittleFish Beauty Team</span>
            </div>
        </div>
    `;

  // Add to container
  container.appendChild(notification);

  // Trigger animation
  setTimeout(() => {
    notification.classList.add("show");
  }, 100);

  // Auto remove after 5 seconds
  setTimeout(() => {
    closeNotification(notification.querySelector(".notification-close"));
  }, 2000);
}

function showSocialMediaNotification(platformName, socialPlatform) {
  const container = document.getElementById("notification-container");

  // Get platform-specific icon and color
  const platformInfo = getSocialPlatformInfo(socialPlatform);

  // Create notification element
  const notification = document.createElement("div");
  notification.className = "development-notification social-notification";

  notification.innerHTML = `
        <div class="notification-content">
            <div class="notification-header">
                <i class="${platformInfo.icon} notification-icon" style="color: ${platformInfo.color}"></i>
                <span class="notification-title">Kết nối ${platformName}</span>
                <button class="notification-close" onclick="closeNotification(this)">
                    <i class="fas fa-times"></i>
                </button>
            </div>
            <div class="notification-body">
                <p>Trang <strong>${platformName}</strong> của chúng tôi đang được xây dựng.</p>
                <p>Hãy theo dõi để không bỏ lỡ những chia sẻ thú vị về làm đẹp và các ưu đãi độc quyền!</p>
                <div class="social-preview">
                    <i class="${platformInfo.icon}" style="color: ${platformInfo.color}"></i>
                    <span>@LittleFishBeauty</span>
                </div>
            </div>
            <div class="notification-footer">
                <i class="fas fa-rocket"></i>
                <span>Sắp ra mắt!</span>
            </div>
        </div>
    `;

  // Add to container
  container.appendChild(notification);

  // Trigger animation
  setTimeout(() => {
    notification.classList.add("show");
  }, 100);

  // Auto remove after 6 seconds (longer for social media)
  setTimeout(() => {
    closeNotification(notification.querySelector(".notification-close"));
  }, 2000);
}

function getSocialPlatformInfo(platform) {
  const platformMap = {
    facebook: {
      icon: "fab fa-facebook-f",
      color: "#1877F2",
    },
    instagram: {
      icon: "fab fa-instagram",
      color: "#E4405F",
    },
    youtube: {
      icon: "fab fa-youtube",
      color: "#FF0000",
    },
    tiktok: {
      icon: "fab fa-tiktok",
      color: "#000000",
    },
    zalo: {
      icon: "fas fa-comments",
      color: "#0068FF",
    },
  };

  return (
    platformMap[platform] || {
      icon: "fas fa-share-alt",
      color: "#4CAF50",
    }
  );
}

function closeNotification(closeBtn) {
  const notification = closeBtn.closest(".development-notification");

  notification.classList.add("hide");

  setTimeout(() => {
    if (notification.parentNode) {
      notification.parentNode.removeChild(notification);
    }
  }, 300);
}

// Close notification when clicking outside
document.addEventListener("click", function (e) {
  if (e.target.classList.contains("development-notification")) {
    closeNotification(e.target.querySelector(".notification-close"));
  }
});

// Close notification with Escape key
document.addEventListener("keydown", function (e) {
  if (e.key === "Escape") {
    const notifications = document.querySelectorAll(
      ".development-notification"
    );
    notifications.forEach((notification) => {
      const closeBtn = notification.querySelector(".notification-close");
      if (closeBtn) {
        closeNotification(closeBtn);
      }
    });
  }
});
