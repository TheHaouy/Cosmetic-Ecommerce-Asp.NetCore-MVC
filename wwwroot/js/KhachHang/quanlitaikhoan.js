let currentFilter = "";

// Auto-load orders when page loads
document.addEventListener("DOMContentLoaded", function () {
  // Only call these functions if they exist (for backward compatibility)
  if (typeof loadOrders === "function") {
    loadOrders();
  }
  if (typeof updateOrderStatistics === "function") {
    updateOrderStatistics();
  }
});

function formatDate(dateString) {
  const date = new Date(dateString);
  return date.toLocaleDateString("vi-VN", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function formatCurrency(amount) {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
  }).format(amount);
}

// Submenu toggle functionality
function toggleSubMenu(menu) {
  const infoSubmenu = document.getElementById("infoSubMenu");
  const ordersSubmenu = document.getElementById("ordersSubMenu");

  if (menu === "info") {
    infoSubmenu.classList.toggle("submenu-slide");
    infoSubmenu.classList.toggle("submenu-hidden");
    ordersSubmenu.classList.remove("submenu-slide");
    ordersSubmenu.classList.add("submenu-hidden");
  } else if (menu === "orders") {
    ordersSubmenu.classList.toggle("submenu-slide");
    ordersSubmenu.classList.toggle("submenu-hidden");
    infoSubmenu.classList.remove("submenu-slide");
    infoSubmenu.classList.add("submenu-hidden");
  }
}

function logout() {
  if (confirm("Bạn có chắc chắn muốn đăng xuất?")) {
    window.location.href = "/";
  }
}

// ---------- Theme management ----------
const themeIcon = document.getElementById("themeIcon");

function updateTheme(theme) {
  document.documentElement.setAttribute("data-bs-theme", theme);
  if (themeIcon) {
    themeIcon.className =
      theme === "dark" ? "bi bi-moon-fill" : "bi bi-sun-fill";
  }
  localStorage.setItem("theme", theme);
}

document.addEventListener("DOMContentLoaded", () => {
  const themeToggle = document.getElementById("themeToggle");
  if (themeToggle) {
    themeToggle.addEventListener("click", () => {
      const currentTheme =
        document.documentElement.getAttribute("data-bs-theme") || "light";
      const newTheme = currentTheme === "dark" ? "light" : "dark";
      updateTheme(newTheme);
    });
  }
  const savedTheme = localStorage.getItem("theme") || "light";
  updateTheme(savedTheme);
});

// Advanced background effects
function createDynamicParticles() {
  const background = document.querySelector(".animated-background");
  if (!background) return;

  // Create additional floating particles
  for (let i = 0; i < 15; i++) {
    const particle = document.createElement("div");
    particle.className = "particle";
    particle.style.left = Math.random() * 100 + "%";
    particle.style.animationDelay = Math.random() * -10 + "s";
    particle.style.animationDuration = Math.random() * 5 + 5 + "s";
    background.appendChild(particle);
  }

  // Create additional sparkles
  for (let i = 0; i < 20; i++) {
    const sparkle = document.createElement("div");
    sparkle.className = "sparkle";
    sparkle.style.top = Math.random() * 100 + "%";
    sparkle.style.left = Math.random() * 100 + "%";
    sparkle.style.animationDelay = Math.random() * -3 + "s";
    sparkle.style.animationDuration = Math.random() * 2 + 2 + "s";
    background.appendChild(sparkle);
  }

  // Create additional geometric shapes
  const shapes = ["circle", "triangle", "square", "diamond"];
  for (let i = 0; i < 12; i++) {
    const shape = document.createElement("div");
    const randomShape = shapes[Math.floor(Math.random() * shapes.length)];
    shape.className = `geo-shape ${randomShape}`;
    shape.style.top = Math.random() * 100 + "%";
    shape.style.left = Math.random() * 100 + "%";
    shape.style.animationDelay = Math.random() * -12 + "s";
    shape.style.animationDuration = Math.random() * 5 + 10 + "s";
    background.appendChild(shape);
  }
}

// Mouse interaction with background
function addMouseInteraction() {
  let mouseX = 0;
  let mouseY = 0;

  document.addEventListener("mousemove", (e) => {
    mouseX = e.clientX / window.innerWidth;
    mouseY = e.clientY / window.innerHeight;

    const floatingIcons = document.querySelectorAll(".floating-icons");
    floatingIcons.forEach((icon, index) => {
      const speed = ((index % 3) + 1) * 0.5;
      const offsetX = (mouseX - 0.5) * speed * 20;
      const offsetY = (mouseY - 0.5) * speed * 20;
      icon.style.transform = `translate(${offsetX}px, ${offsetY}px)`;
    });

    const particles = document.querySelectorAll(".particle");
    particles.forEach((particle, index) => {
      const speed = ((index % 2) + 1) * 0.3;
      const offsetX = (mouseX - 0.5) * speed * 15;
      particle.style.transform = `translateX(${offsetX}px)`;
    });
  });
}

// Random color pulse effect
function addColorPulse() {
  const shapes = document.querySelectorAll(".geo-shape, .sparkle");
  setInterval(() => {
    shapes.forEach((shape) => {
      if (Math.random() > 0.8) {
        shape.style.animationPlayState = "paused";
        shape.style.transform = shape.style.transform + " scale(1.5)";
        shape.style.opacity = "0.3";
        setTimeout(() => {
          shape.style.animationPlayState = "running";
          shape.style.transform = shape.style.transform.replace(
            " scale(1.5)",
            ""
          );
          shape.style.opacity = "";
        }, 500);
      }
    });
  }, 2000);
}

// Floating cosmetic icons with special effects
function enhanceFloatingIcons() {
  const cosmeticIcons = document.querySelectorAll(".floating-icons");
  cosmeticIcons.forEach((icon, index) => {
    icon.addEventListener("mouseenter", () => {
      icon.style.transform = "scale(1.5) rotate(360deg)";
      icon.style.opacity = "0.5";
      icon.style.color = "var(--nature-green-accent)";
      icon.style.filter = "drop-shadow(0 0 20px var(--nature-green-accent))";
    });

    icon.addEventListener("mouseleave", () => {
      icon.style.transform = "";
      icon.style.opacity = "";
      icon.style.color = "";
      icon.style.filter = "";
    });

    if (Math.random() > 0.7) {
      setInterval(() => {
        icon.style.opacity = Math.random() * 0.3 + 0.1;
        setTimeout(() => {
          icon.style.opacity = "";
        }, 200);
      }, Math.random() * 3000 + 2000);
    }
  });
}

// Initialize all background effects
document.addEventListener("DOMContentLoaded", () => {
  setTimeout(() => {
    createDynamicParticles();
    addMouseInteraction();
    addColorPulse();
    enhanceFloatingIcons();
    ensureAvatarInput();
  }, 1000);
});

// Avatar functions
function ensureAvatarInput() {
  let avatarInput = document.getElementById("avatarInput");
  const avatarElement = document.getElementById("profileAvatar");

  if (!avatarInput && avatarElement) {
    avatarInput = document.createElement("input");
    avatarInput.type = "file";
    avatarInput.id = "avatarInput";
    avatarInput.accept = "image/*";
    avatarInput.style.display = "none";
    avatarInput.onchange = handleAvatarChange;
    avatarElement.appendChild(avatarInput);
  }
  return avatarInput;
}

function triggerAvatarUpload() {
  const avatarInput = document.getElementById("avatarInput");
  if (avatarInput) {
    avatarInput.click();
  } else {
    console.error("Avatar input not found");
  }
}

function updateAvatarDisplay(imageDataUrl) {
  const avatarImg = document.getElementById("avatarImg");
  const avatarIcon = document.getElementById("avatarIcon");

  if (avatarImg && avatarIcon) {
    avatarImg.src = imageDataUrl;
    avatarImg.style.display = "block";
    avatarIcon.style.display = "none";
  }
}

// Validation functions
function validateEmail(input) {
  if (!input || !input.value) return false;

  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  const value = input.value.trim();

  if (emailRegex.test(value)) {
    input.classList.remove("is-invalid");
    input.classList.add("is-valid");
    return true;
  } else {
    input.classList.remove("is-valid");
    input.classList.add("is-invalid");
    return false;
  }
}

function validatePhoneNumber(input) {
  if (!input) return false;

  let value = input.value.replace(/[^0-9]/g, "");
  if (value.length > 10) {
    value = value.slice(0, 10);
  }
  input.value = value;

  if (value.length === 0) {
    input.classList.remove("is-valid", "is-invalid");
    return true;
  } else if (value.length === 10 && /^0[0-9]{9}$/.test(value)) {
    input.classList.remove("is-invalid");
    input.classList.add("is-valid");
    return true;
  } else {
    input.classList.remove("is-valid");
    input.classList.add("is-invalid");
    return false;
  }
}

function handlePhoneKeydown(event) {
  const key = event.key;

  if (key === "Enter") {
    event.preventDefault();
    return false;
  }

  if (
    [
      "Backspace",
      "Delete",
      "Tab",
      "Escape",
      "ArrowLeft",
      "ArrowRight",
      "Home",
      "End",
    ].includes(key)
  ) {
    return true;
  }

  if (!/^[0-9]$/.test(key)) {
    event.preventDefault();
    return false;
  }

  if (event.target.value && event.target.value.length >= 10) {
    event.preventDefault();
    return false;
  }

  return true;
}

function handlePhonePaste(event) {
  event.preventDefault();
  const paste = (event.clipboardData || window.clipboardData).getData("text");
  const cleanedPaste = paste.replace(/[^0-9]/g, "").slice(0, 10);
  event.target.value = cleanedPaste;
  validatePhoneNumber(event.target);
  return false;
}

function preventEnterSubmit(event) {
  if (event.key === "Enter") {
    event.preventDefault();
    return false;
  }
}

function resetProfileForm() {
  const form = document.getElementById("profileForm");
  if (form) {
    form.reset();
    showToast("Đã đặt lại form!", "info");
  }
}

// Password functions
function checkPasswordStrength(password, barId, textId) {
  const bar = document.getElementById(barId);
  const text = document.getElementById(textId);

  if (!bar || !text) return;

  if (password.length === 0) {
    bar.style.width = "0%";
    bar.className = "strength-bar";
    text.textContent = "Nhập mật khẩu để kiểm tra độ mạnh";
    return;
  }

  let score = 0;
  if (password.length >= 8) score++;
  if (/[a-z]/.test(password)) score++;
  if (/[A-Z]/.test(password)) score++;
  if (/[0-9]/.test(password)) score++;
  if (/[^A-Za-z0-9]/.test(password)) score++;

  if (score <= 2) {
    bar.style.width = "33%";
    bar.className = "strength-bar strength-weak";
    text.textContent = "Mật khẩu yếu";
  } else if (score <= 3) {
    bar.style.width = "66%";
    bar.className = "strength-bar strength-medium";
    text.textContent = "Mật khẩu trung bình";
  } else {
    bar.style.width = "100%";
    bar.className = "strength-bar strength-strong";
    text.textContent = "Mật khẩu mạnh";
  }
}

function setupPasswordToggle(inputId, buttonId) {
  const input = document.getElementById(inputId);
  const button = document.getElementById(buttonId);

  if (input && button) {
    button.addEventListener("click", () => {
      const type =
        input.getAttribute("type") === "password" ? "text" : "password";
      input.setAttribute("type", type);
      const icon = button.querySelector("i");
      if (icon) {
        icon.className = type === "password" ? "bi bi-eye" : "bi bi-eye-slash";
      }
    });
  }
}

// Toast notification function
function showToast(message, type = "success") {
  const toastContainer = document.createElement("div");
  toastContainer.style.cssText = `
    position: fixed;
    top: 20px;
    right: 20px;
    z-index: 9999;
    min-width: 300px;
  `;

  toastContainer.innerHTML = `
    <div class="toast show align-items-center text-white bg-${
      type === "success" ? "success" : type === "error" ? "danger" : "info"
    } border-0" role="alert">
      <div class="d-flex">
        <div class="toast-body">
          <i class="bi bi-${
            type === "success"
              ? "check-circle"
              : type === "error"
              ? "exclamation-triangle"
              : "info-circle"
          } me-2"></i>
          ${message}
        </div>
        <button type="button" class="btn-close btn-close-white me-2 m-auto" onclick="this.closest('div[style*=\"position: fixed\"]').remove()"></button>
      </div>
    </div>
  `;

  document.body.appendChild(toastContainer);

  setTimeout(() => {
    if (toastContainer.parentNode) {
      toastContainer.remove();
    }
  }, 4000);
}

// Password verification and strength checking
document.addEventListener("DOMContentLoaded", function () {
  const currentPasswordInput = document.getElementById("currentPassword");
  const statusElement = document.getElementById("currentPasswordStatus");
  const validIcon = document.getElementById("currentPasswordValid");
  const invalidIcon = document.getElementById("currentPasswordInvalid");
  const errorMessage = document.getElementById("currentPasswordError");
  let checkTimeout;

  if (currentPasswordInput) {
    currentPasswordInput.addEventListener("input", function () {
      const password = this.value.trim();
      clearTimeout(checkTimeout);

      if (statusElement) statusElement.style.display = "none";
      if (validIcon) validIcon.style.display = "none";
      if (invalidIcon) invalidIcon.style.display = "none";
      if (errorMessage) errorMessage.style.display = "none";
      this.classList.remove("is-valid", "is-invalid");

      if (password.length === 0) return;

      checkTimeout = setTimeout(() => {
        verifyCurrentPassword(password, {
          onSuccess: () => {
            if (statusElement) statusElement.style.display = "flex";
            if (validIcon) validIcon.style.display = "block";
            if (invalidIcon) invalidIcon.style.display = "none";
            if (errorMessage) errorMessage.style.display = "none";
            currentPasswordInput.classList.remove("is-invalid");
            currentPasswordInput.classList.add("is-valid");
          },
          onFail: () => {
            if (statusElement) statusElement.style.display = "flex";
            if (validIcon) validIcon.style.display = "none";
            if (invalidIcon) invalidIcon.style.display = "block";
            if (errorMessage) errorMessage.style.display = "block";
            currentPasswordInput.classList.remove("is-valid");
            currentPasswordInput.classList.add("is-invalid");
          },
        });
      }, 800);
    });
  }

  function verifyCurrentPassword(password, callbacks = {}) {
    return fetch("/KhachHang/ThongTin/VerifyCurrentPassword", {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: `currentPassword=${encodeURIComponent(password)}`,
    })
      .then((res) => res.json())
      .then((result) => {
        if (result && result.success) {
          if (callbacks.onSuccess) callbacks.onSuccess();
          return true;
        } else {
          if (callbacks.onFail) callbacks.onFail();
          return false;
        }
      })
      .catch(() => {
        if (callbacks.onFail) callbacks.onFail();
        return false;
      });
  }

  // Setup password toggles
  setTimeout(() => {
    setupPasswordToggle("currentPassword", "toggleCurrentPassword");
    setupPasswordToggle("newPasswordChange", "toggleNewPasswordChange");
    setupPasswordToggle(
      "confirmNewPasswordChange",
      "toggleConfirmPasswordChange"
    );
  }, 100);

  // New password strength check
  const newPasswordChange = document.getElementById("newPasswordChange");
  if (newPasswordChange) {
    newPasswordChange.addEventListener("input", function () {
      checkPasswordStrength(
        this.value,
        "changeStrengthBar",
        "changeStrengthText"
      );
    });
  }

  // Setup form validation
  const profileForm = document.getElementById("profileForm");
  if (profileForm) {
    profileForm.addEventListener("submit", function (event) {
      // Server-side validation handles the submission
    });
  }
});

// Password form management
function collapseChangePasswordSection() {
  const changePasswordSection = document.getElementById(
    "changePasswordSection"
  );
  if (changePasswordSection) {
    changePasswordSection.classList.remove("show");
  }
}

function resetChangePasswordForm() {
  document.getElementById("currentPassword").value = "";
  document.getElementById("newPasswordChange").value = "";
  document.getElementById("confirmNewPasswordChange").value = "";

  const strengthBar = document.getElementById("changeStrengthBar");
  const strengthText = document.getElementById("changeStrengthText");
  if (strengthBar) strengthBar.style.width = "0";
  if (strengthText)
    strengthText.textContent = "Nhập mật khẩu để kiểm tra độ mạnh";

  ["currentPassword", "newPasswordChange", "confirmNewPasswordChange"].forEach(
    function (id) {
      const input = document.getElementById(id);
      if (input) {
        input.classList.remove("is-valid", "is-invalid");
      }
    }
  );
}

// ===========================
// ADDRESS MANAGEMENT FUNCTIONS
// ===========================

function openAddressModal(addressId = null) {
  const modal = new bootstrap.Modal(document.getElementById("addressModal"));
  const modalTitle = document.getElementById("addressModalLabel");
  const form = document.getElementById("addressForm");

  if (addressId) {
    modalTitle.innerHTML =
      '<i class="bi bi-geo-alt me-2"></i>Chỉnh Sửa Địa Chỉ';
    loadAddressData(addressId);
  } else {
    modalTitle.innerHTML = '<i class="bi bi-geo-alt me-2"></i>Thêm Địa Chỉ Mới';
    form.reset();
    document.getElementById("addressId").value = "";
  }
  modal.show();
}

function openEditAddressFromList(id) {
  try {
    const addresses = window.lastRenderedAddresses || [];
    const address = addresses.find((a) => String(a.id) === String(id));
    if (address) {
      if (typeof showAddressForm === "function") showAddressForm();
      if (typeof fillFormWithAddressData === "function") {
        fillFormWithAddressData({
          id: address.id,
          recipientName: address.recipientName || "",
          phone: address.phone || "",
          province: address.province || "",
          district: address.district || "",
          ward: address.ward || "",
          detailAddress: address.detailAddress || "",
          isDefault: !!address.isDefault,
          addressType: address.addressType || "home",
        });
      }
      const formTitle = document.querySelector(
        "#addAddressSection .card-title"
      );
      if (formTitle)
        formTitle.innerHTML =
          '<i class="bi bi-pencil me-2"></i>Chỉnh Sửa Địa Chỉ';
      return;
    }

    // Fallback: call API
    fetch(`/KhachHang/DiaChi/GetAddress/${id}`)
      .then((res) => res.json())
      .then((resp) => {
        if (resp.success && resp.data) {
          const addr = resp.data;
          if (typeof showAddressForm === "function") showAddressForm();
          if (typeof fillFormWithAddressData === "function") {
            fillFormWithAddressData(addr);
          }
        } else {
          alert("Không thể tải thông tin địa chỉ!");
        }
      })
      .catch((err) => {
        console.error("Error fetching address detail:", err);
        alert("Có lỗi xảy ra khi tải thông tin địa chỉ!");
      });
  } catch (err) {
    console.error("openEditAddressFromList error:", err);
    if (typeof editAddress === "function") {
      editAddress(id);
    } else {
      alert("Không thể mở form chỉnh sửa.");
    }
  }
}

// ===========================
// ADDRESS AUTOCOMPLETE FUNCTIONS
// ===========================

function initializeAddressAutocomplete() {
  const provinceInput = document.getElementById("province");
  const districtInput = document.getElementById("district");
  const wardInput = document.getElementById("ward");

  if (!provinceInput || !districtInput || !wardInput) return;

  setupAutocomplete(
    provinceInput,
    vietnamData.provinces.map((p) => p.name),
    function (selectedProvince) {
      districtInput.value = "";
      wardInput.value = "";
      districtInput.disabled = false;
      wardInput.disabled = true;

      const provinceData = vietnamData.provinces.find(
        (p) => p.name === selectedProvince
      );
      if (provinceData) {
        setupAutocomplete(
          districtInput,
          provinceData.districts,
          function (selectedDistrict) {
            wardInput.value = "";
            wardInput.disabled = false;
            const wards = vietnamData.wards[selectedDistrict] || [
              "Phường 1",
              "Phường 2",
              "Phường 3",
            ];
            setupAutocomplete(wardInput, wards);
          }
        );
      }
    }
  );
}

function setupAutocomplete(input, dataList, onSelect = null) {
  const dropdown = input.nextElementSibling;
  let selectedIndex = -1;

  input.addEventListener("input", function () {
    const value = this.value.toLowerCase();
    const filtered = dataList.filter((item) =>
      item.toLowerCase().includes(value)
    );
    showDropdown(dropdown, filtered, input, onSelect);
    selectedIndex = -1;
  });

  input.addEventListener("keydown", function (e) {
    const items = dropdown.querySelectorAll(".autocomplete-item");
    if (e.key === "ArrowDown") {
      e.preventDefault();
      selectedIndex = Math.min(selectedIndex + 1, items.length - 1);
      updateSelection(items, selectedIndex);
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      selectedIndex = Math.max(selectedIndex - 1, -1);
      updateSelection(items, selectedIndex);
    } else if (e.key === "Enter") {
      e.preventDefault();
      if (selectedIndex >= 0 && items[selectedIndex]) {
        selectItem(items[selectedIndex], input, dropdown, onSelect);
      }
    } else if (e.key === "Escape") {
      hideDropdown(dropdown);
    }
  });

  input.addEventListener("blur", function () {
    setTimeout(() => hideDropdown(dropdown), 200);
  });
}

function showDropdown(dropdown, items, input, onSelect) {
  if (items.length === 0) {
    hideDropdown(dropdown);
    return;
  }

  let html = "";
  items.forEach((item) => {
    html += `<div class="autocomplete-item" data-value="${item}">${item}</div>`;
  });

  dropdown.innerHTML = html;
  dropdown.classList.add("show");

  dropdown.querySelectorAll(".autocomplete-item").forEach((item) => {
    item.addEventListener("click", function () {
      selectItem(this, input, dropdown, onSelect);
    });
  });
}

function hideDropdown(dropdown) {
  dropdown.classList.remove("show");
}

function updateSelection(items, selectedIndex) {
  items.forEach((item, index) => {
    item.classList.toggle("selected", index === selectedIndex);
  });
}

function selectItem(item, input, dropdown, onSelect) {
  const value = item.getAttribute("data-value");
  input.value = value;
  hideDropdown(dropdown);
  if (onSelect) {
    onSelect(value);
  }
}

// Initialize autocomplete when modal is shown
document.addEventListener("DOMContentLoaded", function () {
  const addressModal = document.getElementById("addressModal");
  if (addressModal) {
    addressModal.addEventListener("shown.bs.modal", function () {
      initializeAddressAutocomplete();
    });
  }
});
